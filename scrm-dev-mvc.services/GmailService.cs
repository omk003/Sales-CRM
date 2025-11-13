using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace scrm_dev_mvc.Services
{
    using Google.Apis.Auth.OAuth2;
    using Google.Apis.Auth.OAuth2.Flows;
    using Google.Apis.Auth.OAuth2.Responses;
    using Google.Apis.Oauth2.v2;
    using Google.Apis.Services;
    using MailKit.Net.Smtp;
    using MailKit.Security;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using MimeKit;
    using scrm_dev_mvc.Data.Repository.IRepository;
    using scrm_dev_mvc.Models;
    using scrm_dev_mvc.Models.ViewModels;
    using scrm_dev_mvc.services.Interfaces;
    using System.Threading.Tasks;
    public class GmailService : IGmailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly GoogleAuthorizationCodeFlow _flow;

        public GmailService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;

            _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _configuration["Google:ClientId"],
                    ClientSecret = _configuration["Google:ClientSecret"]
                },
                Scopes = new[] { "https://mail.google.com/","openid",
    "email",
    "profile" },
            });
        }

       
        public async Task<string> GenerateAuthUrl(string userEmail,string redirectUri)
        {
            
            var request = _flow.CreateAuthorizationCodeRequest(redirectUri);
            if(string.IsNullOrEmpty(userEmail))
            {
                request.State = Guid.NewGuid().ToString();
            }
            else
            {
                request.State = userEmail;
            }
            return request.Build().AbsoluteUri;
        }

        public async Task<User> ExchangeCodeForTokensAsync(string userEmail, string code, string redirectUri)
        {
            TokenResponse tokenResponse = await _flow.ExchangeCodeForTokenAsync(
                userId: null,
                code,
                redirectUri,
                CancellationToken.None
            );

            var credential = new UserCredential(_flow, "me", tokenResponse);

            var oauth2Service = new Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "sales-crm-dev"
            });

            // Get user's info from Google
            var userInfo = await oauth2Service.Userinfo.Get().ExecuteAsync();
            var googleEmail = userInfo.Email;
            var firstName = userInfo.GivenName;
            var lastName = userInfo.FamilyName;
            User user;
            if (!string.IsNullOrEmpty(userEmail))
            {
                user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == userEmail, "GmailCred");
                if (googleEmail != userEmail)
                {
                    return null;
                }
            }
            else
            {
                user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == googleEmail, "GmailCred");
            }
               
                
            if (user == null)
            {
                // If not, create a new user
                user = new User
                {
                    Email = googleEmail,
                    FirstName = firstName,
                    LastName = lastName,
                    RoleId = 4, // default role
                    GmailCred = new GmailCred(),
                    
                };
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }

            if (user.GmailCred == null)
                user.GmailCred = new GmailCred();

            user.GmailCred.GmailAccessToken = tokenResponse.AccessToken;

            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                user.GmailCred.GmailRefreshToken = tokenResponse.RefreshToken;

            if (!string.IsNullOrEmpty(firstName)) user.FirstName = firstName;
            if (!string.IsNullOrEmpty(lastName)) user.LastName = lastName;
            user.IsSyncedWithGoogle = true;
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveChangesAsync();

            user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Email == googleEmail, "Role");

            return user;
        }




        public async Task<SendEmailResult> SendEmailAsync(Guid userId, string toEmail, string subject, string body, string redirectUri)
        {
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId, "GmailCred");

            if (user == null || string.IsNullOrEmpty(user.GmailCred.GmailRefreshToken))
            {
               
                string authUrl = await GenerateAuthUrl(user?.Email, redirectUri);

                return new SendEmailResult
                {
                    IsSuccess = false,
                    AuthenticationRequired = true,
                    AuthenticationUrl = authUrl
                };
            }

            try
            {
                
                var credential = new UserCredential(_flow, user.Email, new TokenResponse
                {
                    RefreshToken = user.GmailCred.GmailRefreshToken
                });

                bool success = await credential.RefreshTokenAsync(CancellationToken.None);

                if (!success || credential.Token == null)
                {
                    string authUrl = await GenerateAuthUrl(user?.Email, redirectUri);
                    return new SendEmailResult { IsSuccess = false, AuthenticationRequired = true, AuthenticationUrl = authUrl };
                }
                if (credential.Token.RefreshToken != null)
                {
                    user.GmailCred.GmailRefreshToken = credential.Token.RefreshToken;
                    _unitOfWork.Users.Update(user);
                    await _unitOfWork.SaveChangesAsync();
                }

                var accessToken = credential.Token.AccessToken;

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(user.Email, user.Email));
                message.To.Add(MailboxAddress.Parse(toEmail));
                message.Subject = subject;
                message.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(new SaslMechanismOAuth2(user.Email, accessToken));
                await smtp.SendAsync(message);
                await smtp.DisconnectAsync(true);

                // If we reach here, the email was sent successfully.
                return new SendEmailResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                return new SendEmailResult { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }
    }

}
