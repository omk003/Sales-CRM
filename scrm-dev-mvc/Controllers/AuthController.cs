using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using scrm_dev_mvc.DataAccess.Data;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.services;
using scrm_dev_mvc.Services;
using System.Security.Claims;

namespace scrm_dev_mvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly IGmailService _gmailService;
        private readonly IOrganizationService _organizationService;
        private readonly IUserService _userService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IInvitationService _invitationService;
        private readonly IConfiguration _configuration;
        public AuthController(IGmailService gmailService, IConfiguration configuration, IOrganizationService organizationService,IUserService userService, IPasswordHasher passwordHasher, IInvitationService invitationService)
        {
            
            _gmailService = gmailService;
            _organizationService = organizationService;
            _userService = userService;
            _passwordHasher = passwordHasher;
            _invitationService = invitationService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? invitationcode, string returnUrl = null)
        {
            // CAPTURE THE INVITATION CODE FROM THE URL
            if (!string.IsNullOrEmpty(invitationcode))
            {
                // Store it in TempData to survive the redirect to the OTP page
                TempData["InvitationCode"] = invitationcode;
            }
            ViewBag.InvitationCode = TempData["InvitationCode"]?.ToString();
            TempData.Keep("InvitationCode");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model,  string? invitationCode,string returnUrl = null)
        {

            // Login
            var user = await _userService.IsEmailExistsAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Account Doesnt Exist");
                return View(model);
            }


            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError("", "This account is linked with Google login. Please use Google Sign-In.");
                return View(model);
            }

            // verify password hash
            if(_passwordHasher.VerifyPassword(user.PasswordHash, model.Password) == false)
            {
                ModelState.AddModelError("", "Invalid password.");
                return View(model);
            }

            // 4. PROCESS THE INVITATION PASSWORD HASH IS VERIFIED
            if (!string.IsNullOrEmpty(invitationCode))
            {
                 await ProcessInvitationAsync(invitationCode, user.Id);
            }

                // Now create claims and sign in
            var org = await _organizationService.IsInOrganizationById(user.Id);

            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
                    new Claim("OrganizationName", org?.Name ?? string.Empty)
                };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    });

            return RedirectToAction("Index", "Home");
            
        }

        [HttpGet]
        public IActionResult Register(string? invitationcode, string returnUrl = null)
        {
            // CAPTURE THE INVITATION CODE FROM THE URL
            if (!string.IsNullOrEmpty(invitationcode))
            {
                // Store it in TempData to survive the redirect to the OTP page
                TempData["InvitationCode"] = invitationcode;
            }
            ViewBag.InvitationCode = TempData["InvitationCode"]?.ToString();
            TempData.Keep("InvitationCode");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            if(model.ConfirmPassword != model.Password)
            {
                ModelState.AddModelError("", "Enter same password as confirm password");
                return View(model);
            }
            var user = await _userService.IsEmailExistsAsync(model.Email);
            
            if (user != null)
            {
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    ModelState.AddModelError("", "This account is linked with Google login. Please use Google Sign-In.");
                    return View(model);
                }
                // Account already exists

                ModelState.AddModelError("", "Account already exists.");
                    return View(model);
                
            }
            else
            {
                // Create new user (unverified yet)
                user = new User
                {
                    Email = model.Email,
                    PasswordHash = _passwordHasher.HashPassword(model.Password),
                    CreatedAt = DateTime.UtcNow,
                    RoleId = 4
                };
                await _userService.CreateUserAsync(user);
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
            await _userService.UpdateUserAsync(user);
            string? adminId = _configuration["Data:AdminEmailId"];

            // Send OTP Email
            await _gmailService.SendEmailAsync(
                Guid.Parse(adminId ?? ""),
                user.Email,
                "Your SCRM OTP Code",
                $"Your OTP code is: {otp}. It expires in 5 minutes.",
                ""
            );

            TempData["Email"] = user.Email;
            return RedirectToAction("VerifyOtp");

        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["Email"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            // 3. PASS THE INVITATION CODE TO THE VERIFY OTP VIEW
            ViewBag.Email = email;
            ViewBag.InvitationCode = TempData["InvitationCode"]?.ToString(); // Pass it to the view

            // Re-keep TempData in case the user needs to resend OTP
            TempData.Keep("InvitationCode");
            TempData.Keep("Email");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string email, string otp, string? invitationCode)
        {
            var user = await _userService.IsEmailExistsAsync(email);
            if (user == null)
                return RedirectToAction("Login");

            if (user.OtpCode == otp && user.OtpExpiry > DateTime.UtcNow)
            {
                user.OtpCode = null;
                user.OtpExpiry = null;
                await _userService.UpdateUserAsync(user);

                // 4. PROCESS THE INVITATION *AFTER* OTP IS VERIFIED
                if (!string.IsNullOrEmpty(invitationCode))
                {
                    await ProcessInvitationAsync(invitationCode, user.Id);
                }

                // Now create claims and sign in
                var org = await _organizationService.IsInOrganizationById(user.Id);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
                    new Claim("OrganizationName", org?.Name ?? string.Empty)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    });

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid or expired OTP.");
            ViewBag.Email = email;
            ViewBag.InvitationCode = invitationCode; // Pass it back to the view on failure
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            var user = await _userService.IsEmailExistsAsync(email);
            if (user == null)
                return RedirectToAction("Login");

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(5);
            await _userService.UpdateUserAsync(user);
            string? adminId = _configuration["Data:AdminEmailId"];
            await _gmailService.SendEmailAsync(
                Guid.Parse(adminId ?? ""),
                user.Email,
                "Your New OTP Code",
                $"Your new OTP code is {otp}. It expires in 5 minutes.",
                ""
            );

            TempData["Email"] = user.Email;
            return RedirectToAction("VerifyOtp");
        }


        [HttpPost]
        public async Task<IActionResult> LoginWithGoogle(string? invitationCode, string returnUrl = null)
        {
            // Redirect to Google OAuth
            var redirectUri = Url.Action(nameof(GoogleCallback), "Auth", null, Request.Scheme, "maudlinly-nonreactive-arturo.ngrok-free.dev");
            var authUrl = await _gmailService.GenerateAuthUrl(invitationCode ?? "", redirectUri);
            return Redirect(authUrl);
        }
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        private async System.Threading.Tasks.Task ProcessInvitationAsync(string invitationCode, Guid userId)
        {
            var organization = await _organizationService.IsInOrganizationById(userId);
            if (organization != null)
            {
                TempData["Warning"] = "You are already in an organization. The invitation was disregarded.";
                return;
            }

            // Your InvitationService should handle checking if the code is valid/expired
            var success = await _invitationService.AcceptInvitationAsync(invitationCode, userId);

            if (success)
            {
                TempData["Message"] = "Welcome! You have successfully joined the organization.";
            }
            else
            {
                TempData["Error"] = "The invitation code is invalid, expired, or could not be processed.";
            }
            return;
        }


        [AllowAnonymous]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
        {
            bool rememberMe = true;
            string? invitationCode = state;

            if (string.IsNullOrEmpty(code))
            {
                return BadRequest("Error: Authorization code was not provided by Google.");
            }

            // The redirect URI used during auth request
            var redirectUri = Url.Action(nameof(GoogleCallback), "Auth", null, Request.Scheme, "maudlinly-nonreactive-arturo.ngrok-free.dev");

            try
            {
                // Exchange the code for tokens and get user info (including email) from Google
                var googleUser = await _gmailService.ExchangeCodeForTokensAsync("", code, redirectUri);

                // Check if the user already exists in your database
                var user = await _userService.IsEmailExistsAsync(googleUser.Email);
                if (user == null)
                {
                    // Create a new user if not exists
                    user = new User
                    {
                        Email = googleUser.Email,
                        FirstName = googleUser.FirstName,
                        LastName = googleUser.LastName,
                        RoleId = 4 // default role
                    };
                    await _userService.CreateUserAsync(user);
                }

                // 5. PROCESS INVITATION FOR GOOGLE SIGN-IN
                if (!string.IsNullOrEmpty(invitationCode))
                {
                    await ProcessInvitationAsync(invitationCode, user.Id);
                }

                // Get organization if any
                var org = await _organizationService.IsInOrganizationById(user.Id);

                // Create claims
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role?.Name ?? string.Empty),
            new Claim("OrganizationName", org?.Name ?? string.Empty)
        };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : (DateTimeOffset?)null
                };

                // Sign in
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                TempData["Message"] = ex.Message;
                return RedirectToAction("Login");
            }
        }

        [Authorize] // Only logged-in users
        [HttpGet]
        public async Task<IActionResult> GmailSync()
        {
            // 1️⃣ Get the currently logged-in user's email from claims
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            // 2️⃣ Generate redirect URI for Gmail OAuth callback
            var redirectUri = Url.Action(nameof(GmailSyncCallback), "Auth", null, Request.Scheme, "maudlinly-nonreactive-arturo.ngrok-free.dev");

            // 3️⃣ Generate the Google OAuth URL using the logged-in user's email in 'state'
            var authUrl = await _gmailService.GenerateAuthUrl(userEmail, redirectUri);

            // 4️⃣ Redirect user to Google's consent screen
            return Redirect(authUrl);
        }

        //[Authorize]
        [HttpGet]
        [AllowAnonymous] // Google callback can be called by Google
        public async Task<IActionResult> GmailSyncCallback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code))
            {
                TempData["Message"] = "Authorization code not provided.";
                return RedirectToAction("Index", "User"); // Or wherever you want
            }

            try
            {
                // Exchange code for tokens and attach them to the user (state = email)
                var user = await _gmailService.ExchangeCodeForTokensAsync(state, code,
                    Url.Action(nameof(GmailSyncCallback), "Auth", null, Request.Scheme));
                if (user == null)
                {
                    TempData["Message"] = $"Error linking Gmail: Log in with same Gmail";
                    return RedirectToAction("Index", "User");
                }
                TempData["Message"] = "Gmail account successfully linked!";
                return RedirectToAction("Index", "User");
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"Error linking Gmail: {ex.Message}";
                return RedirectToAction("Index", "User");
            }
        }

    }
}
