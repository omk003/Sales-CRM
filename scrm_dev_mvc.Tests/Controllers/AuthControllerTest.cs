using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using scrm_dev_mvc.Controllers;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.Enums;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.services.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace scrm_dev_mvc.Tests
{
    public class AuthControllerTests
    {
        // Mocks for all injected services
        private readonly Mock<IGmailService> _mockGmailService;
        private readonly Mock<IOrganizationService> _mockOrgService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IPasswordHasher> _mockPasswordHasher;
        private readonly Mock<IInvitationService> _mockInvitationService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;

        // Mocks for ASP.NET Core infrastructure
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly DefaultHttpContext _httpContext;

        // The controller instance we are testing
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // 1. Initialize all the mocks for services
            _mockGmailService = new Mock<IGmailService>();
            _mockOrgService = new Mock<IOrganizationService>();
            _mockUserService = new Mock<IUserService>();
            _mockPasswordHasher = new Mock<IPasswordHasher>();
            _mockInvitationService = new Mock<IInvitationService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            // 2. Mock HttpContext and its dependencies
            _httpContext = new DefaultHttpContext();

            // Mock Authentication Service
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IAuthenticationService)))
                .Returns(_mockAuthService.Object);

            _httpContext.RequestServices = _mockServiceProvider.Object;

            // Mock TempData
            var tempData = new TempDataDictionary(_httpContext, Mock.Of<ITempDataProvider>());

            // 3. Create the controller, passing in the mocked services
            _controller = new AuthController(
                _mockGmailService.Object,
                _mockConfiguration.Object,
                _mockOrgService.Object,
                _mockUserService.Object,
                _mockPasswordHasher.Object,
                _mockInvitationService.Object,
                _mockCurrentUserService.Object
            )
            {
                // 4. Assign the mocked infrastructure to the controller
                ControllerContext = new ControllerContext()
                {
                    HttpContext = _httpContext,
                },
                TempData = tempData,
                Url = Mock.Of<IUrlHelper>() // Mock a URL helper
            };

            // 5. Setup a default mock for configuration (if needed by many tests)
            _mockConfiguration.Setup(c => c["Data:AdminEmailId"])
                              .Returns(Guid.NewGuid().ToString());
        }

        // --- TEST 1 ---
        [Fact]
        public void Login_Get_WithInvitationCode_SetsTempDataAndReturnsView()
        {
            // Arrange
            var invitationCode = "test-code-123";

            // Act
            var result = _controller.Login(invitationCode, null);

            // Assert
            // Check that it returned a View
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model); // No model is passed to this view

            // Check that the invitation code was correctly stored in TempData
            Assert.Equal(invitationCode, _controller.TempData["InvitationCode"]);
        }

        // --- TEST 2 ---
        [Fact]
        public async System.Threading.Tasks.Task Register_Post_UserAlreadyExists_ReturnsViewWithModelError()
        {
            // Arrange
            var registerModel = new RegisterViewModel
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var existingUser = new User { Email = "test@example.com", PasswordHash = "somehash" };

            // Setup the mock UserService to "find" the existing user
            _mockUserService.Setup(s => s.IsEmailExistsAsync(registerModel.Email))
                            .ReturnsAsync(existingUser);

            // Act
            var result = await _controller.Register(registerModel, null);

            // Assert
            // 1. Check that we are returned to the View (not redirected)
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(registerModel, viewResult.Model); // The invalid model is sent back

            // 2. Check that the ModelState is now invalid
            Assert.False(_controller.ModelState.IsValid);

            // 3. Check that the correct error message was added
            Assert.True(_controller.ModelState.ContainsKey("")); // Key for general errors
            var modelError = _controller.ModelState[""].Errors.First();
            Assert.Equal("Account already exists.", modelError.ErrorMessage);

            // 4. Check that no new user was created
            _mockUserService.Verify(s => s.CreateUserAsync(It.IsAny<User>()), Times.Never);
        }

        // --- TEST 3 ---
        [Fact]
        public async System.Threading.Tasks.Task Login_Post_ValidCredentials_SignsInUserAndRedirectsToWorkspace()
        {
            // Arrange
            var loginModel = new LoginViewModel { Email = "user@example.com", Password = "GoodPassword123!" };
            var userId = Guid.NewGuid();
            var userRole = new Role { Name = "SalesUser" };
            var mockUser = new User
            {
                Id = userId,
                Email = "user@example.com",
                FirstName = "Test",
                PasswordHash = "hashed_password",
                RoleId = (int)UserRoleEnum.SalesUser, // Use the enum value
                Role = userRole
            };
            var mockOrg = new Organization { Name = "TestOrg" };

            // 1. Mock user existence check
            _mockUserService.Setup(s => s.IsEmailExistsAsync(loginModel.Email))
                            .ReturnsAsync(mockUser);

            // 2. Mock password verification
            _mockPasswordHasher.Setup(h => h.VerifyPassword(mockUser.PasswordHash, loginModel.Password))
                               .Returns(true);

            // 3. Mock organization check
            _mockOrgService.Setup(o => o.IsInOrganizationById(mockUser.Id))
                           .ReturnsAsync(mockOrg);

            // 4. Mock the Sign-in process
            _mockAuthService
                .Setup(a => a.SignInAsync(
                    _httpContext,
                    It.IsAny<string>(), // AuthenticationScheme
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(System.Threading.Tasks.Task.CompletedTask);

            // Act
            var result = await _controller.Login(loginModel, null, null);

            // Assert
            // 1. Check that the user was redirected
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            // 2. Check the redirect location
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Workspace", redirectResult.ControllerName);

            // 3. Verify that the password was checked
            _mockPasswordHasher.Verify(h => h.VerifyPassword(mockUser.PasswordHash, loginModel.Password), Times.Once);

            // 4. Verify that the sign-in method was called
            _mockAuthService.Verify(a => a.SignInAsync(
                _httpContext,
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.Is<ClaimsPrincipal>(cp => cp.FindFirst(ClaimTypes.NameIdentifier).Value == userId.ToString()),
                It.IsAny<AuthenticationProperties>()),
                Times.Once);
        }
        [Fact]
        public async System.Threading.Tasks.Task Login_Post_BadPassword_ReturnsViewWithModelError()
        {
            // Arrange
            var loginModel = new LoginViewModel { Email = "user@example.com", Password = "BadPassword!" };
            var mockUser = new User { Id = Guid.NewGuid(), Email = "user@example.com", PasswordHash = "hashed_password" };

            // 1. Mock user existence
            _mockUserService.Setup(s => s.IsEmailExistsAsync(loginModel.Email)).ReturnsAsync(mockUser);

            // 2. Mock password verification to FAIL
            _mockPasswordHasher.Setup(h => h.VerifyPassword(mockUser.PasswordHash, loginModel.Password))
                               .Returns(false);

            // Act
            var result = await _controller.Login(loginModel, null, null);

            // Assert
            // 1. Check for View result
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(loginModel, viewResult.Model);

            // 2. Check for model error
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("Invalid password.", _controller.ModelState[""].Errors.First().ErrorMessage);

            // 3. Ensure sign-in was NOT called
            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task Login_Post_GoogleOnlyAccount_ReturnsViewWithModelError()
        {
            // Arrange
            var loginModel = new LoginViewModel { Email = "google-user@example.com", Password = "any-password" };
            // Note: PasswordHash is null for a Google-only account
            var mockUser = new User { Id = Guid.NewGuid(), Email = "google-user@example.com", PasswordHash = null };

            // 1. Mock user existence
            _mockUserService.Setup(s => s.IsEmailExistsAsync(loginModel.Email)).ReturnsAsync(mockUser);

            // Act
            var result = await _controller.Login(loginModel, null, null);

            // Assert
            // 1. Check for View result
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(loginModel, viewResult.Model);

            // 2. Check for model error
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("This account is linked with Google login. Please use Google Sign-In.", _controller.ModelState[""].Errors.First().ErrorMessage);

            // 3. Ensure password verification and sign-in were NOT called
            _mockPasswordHasher.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task Register_Post_NewUser_CreatesUserAndSendsOtpAndRedirects()
        {
            // Arrange
            var registerModel = new RegisterViewModel { Email = "new-user@example.com", Password = "NewPassword123!", ConfirmPassword = "NewPassword123!" };
            var hashedPassword = "hashed_new_password";
            User createdUser = null;

            // 1. Mock user does NOT exist
            _mockUserService.Setup(s => s.IsEmailExistsAsync(registerModel.Email)).ReturnsAsync((User)null);

            // 2. Mock password hasher
            _mockPasswordHasher.Setup(h => h.HashPassword(registerModel.Password)).Returns(hashedPassword);

            // 3. Capture the user when CreateUserAsync is called
            _mockUserService.Setup(s => s.CreateUserAsync(It.IsAny<User>()))
                            .Callback<User>(user => createdUser = user)
                            .Returns(System.Threading.Tasks.Task.CompletedTask); // This line is fine (System.Threading.Tasks.Task.CompletedTask is also fine)

            // 4. Mock email service
            // This is the line to fix. Use .Returns(Task.CompletedTask) for methods returning Task.
            _mockGmailService.Setup(s => s.SendEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                             .ReturnsAsync(new SendEmailResult { IsSuccess = true });

            // Act
            var result = await _controller.Register(registerModel, null);

            // Assert
            // 1. Check for Redirect
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VerifyOtp", redirectResult.ActionName);

            // 2. Verify CreateUserAsync was called
            _mockUserService.Verify(s => s.CreateUserAsync(It.Is<User>(
                u => u.Email == registerModel.Email &&
                     u.PasswordHash == hashedPassword &&
                     u.RoleId == (int)UserRoleEnum.SalesUser
            )), Times.Once);

            // 3. Verify UpdateUserAsync was called to save OTP
            // We can check that the 'createdUser' object (captured by the callback) was updated
            _mockUserService.Verify(s => s.UpdateUserAsync(It.Is<User>(
                u => u.Email == registerModel.Email &&
                     !string.IsNullOrEmpty(u.OtpCode) &&
                     u.OtpExpiry > DateTime.UtcNow
            )), Times.Once);

            // 4. Verify OTP email was sent
            _mockGmailService.Verify(s => s.SendEmailAsync(It.IsAny<Guid>(), registerModel.Email, "Your SCRM OTP Code", It.IsAny<string>(), ""), Times.Once);

            // 5. Verify TempData
            Assert.Equal(registerModel.Email, _controller.TempData["Email"]);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyOtp_Post_ValidOtp_SignsInUserAndRedirectsToHome()
        {
            // Arrange
            var email = "user-to-verify@example.com";
            var otp = "123456";
            var userId = Guid.NewGuid();
            var mockUser = new User
            {
                Id = userId,
                Email = email,
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(5), // Valid expiry
                RoleId = (int)UserRoleEnum.SalesUser,
                Role = new Role { Name = "SalesUser" }
            };

            _mockUserService.Setup(s => s.IsEmailExistsAsync(email)).ReturnsAsync(mockUser);
            _mockOrgService.Setup(o => o.IsInOrganizationById(userId)).ReturnsAsync((Organization)null);
            _mockAuthService.Setup(a => a.SignInAsync(_httpContext, It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>())).Returns(System.Threading.Tasks.Task.CompletedTask);

            // Act
            var result = await _controller.VerifyOtp(email, otp, null); // No invitation code

            // Assert
            // 1. Check for Redirect
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // 2. Verify user OTP fields were cleared
            _mockUserService.Verify(s => s.UpdateUserAsync(It.Is<User>(
                u => u.Id == userId &&
                     u.OtpCode == null &&
                     u.OtpExpiry == null
            )), Times.Once);

            // 3. Verify sign-in was called
            _mockAuthService.Verify(a => a.SignInAsync(_httpContext, CookieAuthenticationDefaults.AuthenticationScheme, It.Is<ClaimsPrincipal>(cp => cp.FindFirst(ClaimTypes.NameIdentifier).Value == userId.ToString()), It.IsAny<AuthenticationProperties>()), Times.Once);

            // 4. Verify invitation was NOT processed
            _mockInvitationService.Verify(i => i.AcceptInvitationAsync(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyOtp_Post_InvalidOtp_ReturnsViewWithModelError()
        {
            // Arrange
            var email = "user-to-verify@example.com";
            var correctOtp = "123456";
            var wrongOtp = "999999";
            var mockUser = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = correctOtp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(5) // Valid expiry
            };

            _mockUserService.Setup(s => s.IsEmailExistsAsync(email)).ReturnsAsync(mockUser);

            // Act
            var result = await _controller.VerifyOtp(email, wrongOtp, "test-code"); // Pass wrong OTP

            // Assert
            // 1. Check for View result
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("Invalid or expired OTP.", _controller.ModelState[""].Errors.First().ErrorMessage);

            // 2. Check ViewBag
            Assert.Equal(email, viewResult.ViewData["Email"]);
            Assert.Equal("test-code", viewResult.ViewData["InvitationCode"]);

            // 3. Verify user was NOT updated and NOT signed in
            _mockUserService.Verify(s => s.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        }

        [Fact]
        public async System.Threading.Tasks.Task VerifyOtp_Post_ExpiredOtp_ReturnsViewWithModelError()
        {
            // Arrange
            var email = "user-to-verify@example.com";
            var otp = "123456";
            var mockUser = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                OtpCode = otp,
                OtpExpiry = DateTime.UtcNow.AddMinutes(-5) // EXPIRED
            };

            _mockUserService.Setup(s => s.IsEmailExistsAsync(email)).ReturnsAsync(mockUser);

            // Act
            var result = await _controller.VerifyOtp(email, otp, null); // Pass correct but expired OTP

            // Assert
            // 1. Check for View result
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Equal("Invalid or expired OTP.", _controller.ModelState[""].Errors.First().ErrorMessage);

            // 2. Verify user was NOT updated and NOT signed in
            _mockUserService.Verify(s => s.UpdateUserAsync(It.IsAny<User>()), Times.Never);
            _mockAuthService.Verify(a => a.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
        }

        //[Fact]
        //public async System.Threading.Tasks.Task GoogleCallback_ValidCode_NewUser_CreatesUserAndSignsIn()
        //{
        //    // Arrange
        //    var code = "google-auth-code";
        //    var invitationCode = "invite-from-google";
        //    var googleUser = new GoogleUserDto("new-google-user@gmail.com", "Google", "User");
        //    var newUserId = Guid.NewGuid();

        //    // 1. Mock Google Service
        //    _mockGmailService.Setup(s => s.ExchangeCodeForTokensAsync("", code, It.IsAny<string>()))
        //                     .ReturnsAsync(googleUser);

        //    // 2. Mock User Service (user does NOT exist)
        //    _mockUserService.Setup(s => s.IsEmailExistsAsync(googleUser.Email)).ReturnsAsync((User)null);

        //    // 3. Mock Create User
        //    _mockUserService.Setup(s => s.CreateUserAsync(It.Is<User>(u => u.Email == googleUser.Email)))
        //                    .Callback<User>(u => u.Id = newUserId) // Set the ID on the user object when it's "created"
        //                    .Returns(System.Threading.Tasks.Task.CompletedTask);

        //    // 4. Mock Invitation Service
        //    _mockInvitationService.Setup(i => i.AcceptInvitationAsync(invitationCode, newUserId)).ReturnsAsync(true);

        //    // 5. Mock Org Service
        //    _mockOrgService.Setup(o => o.IsInOrganizationById(newUserId)).ReturnsAsync(new Organization { Name = "NewOrg" });

        //    // 6. Mock Sign-in
        //    _mockAuthService.Setup(a => a.SignInAsync(_httpContext, It.IsAny<string>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>())).Returns(System.Threading.Tasks.Task.CompletedTask);

        //    // Act
        //    var result = await _controller.GoogleCallback(code, invitationCode);

        //    // Assert
        //    // 1. Check Redirect
        //    var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        //    Assert.Equal("Index", redirectResult.ActionName);
        //    Assert.Equal("Home", redirectResult.ControllerName);

        //    // 2. Verify user was created
        //    _mockUserService.Verify(s => s.CreateUserAsync(It.Is<User>(
        //        u => u.Email == googleUser.Email &&
        //             u.FirstName == googleUser.FirstName &&
        //             u.RoleId == (int)UserRoleEnum.SalesUser
        //    )), Times.Once);

        //    // 3. Verify invitation was processed
        //    _mockInvitationService.Verify(i => i.AcceptInvitationAsync(invitationCode, newUserId), Times.Once);

        //    // 4. Verify sign-in was called
        //    _mockAuthService.Verify(a => a.SignInAsync(_httpContext, CookieAuthenticationDefaults.AuthenticationScheme, It.Is<ClaimsPrincipal>(cp => cp.FindFirst(ClaimTypes.NameIdentifier).Value == newUserId.ToString()), It.IsAny<AuthenticationProperties>()), Times.Once);
        //}

        [Fact]
        public async System.Threading.Tasks.Task Logout_ValidCall_SignsOutUserAndRedirects()
        {
            // Arrange
            // 1. Mock the Sign-out process
            _mockAuthService
                .Setup(a => a.SignOutAsync(
                    _httpContext,
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    null))
                .Returns(System.Threading.Tasks.Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            // 1. Check for Redirect
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("LandingPage", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // 2. Verify SignOutAsync was called
            _mockAuthService.Verify(a => a.SignOutAsync(_httpContext, CookieAuthenticationDefaults.AuthenticationScheme, null), Times.Once);
        }
        // You can add more tests here for:
        // - Login with a bad password
        // - Login with a Google-only account
        // - Successful registration and OTP generation
        // - Successful OTP verification
        // - Invalid or expired OTP
        // - GoogleCallback logic
        // - Logout
    }
}