using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.ViewModels;

namespace scrm_dev_mvc.services.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that interacts with the Gmail API
    /// for authentication and sending emails.
    /// </summary>
    public interface IGmailService
    {
        /// <summary>
        /// Step 1 of the OAuth 2.0 flow. Generates the Google consent screen URL.
        /// </summary>
        /// <param name="userId">The unique identifier for the user in your application's database.</param>
        /// <param name="redirectUri">The callback URL that Google will redirect to after consent.</param>
        /// <returns>The absolute URL for the Google authorization page.</returns>
        Task<string> GenerateAuthUrl(string userEmail, string redirectUri);

        /// <summary>
        /// Step 2 of the OAuth 2.0 flow. Handles the callback from Google.
        /// It exchanges the received authorization code for access and refresh tokens
        /// and saves them to the user's record in the database.
        /// </summary>
        /// <param name="userId">The user's ID, which should be passed back from the 'state' parameter.</param>
        /// <param name="code">The authorization code provided by Google in the callback.</param>
        /// <param name="redirectUri">The same callback URI that was used to generate the auth URL.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<User> ExchangeCodeForTokensAsync(string userId, string code, string redirectUri);

        /// <summary>
        /// Sends an email on behalf of an authenticated user. It uses the stored refresh
        /// token to automatically get a valid access token before sending.
        /// </summary>
        /// <param name="userId">The Guid of the user in your database who is sending the email.</param>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="body">The plain text body of the email.</param>
        /// <returns>A task that represents the asynchronous email sending operation.</returns>
        Task<SendEmailResult> SendEmailAsync(Guid userId, string toEmail, string subject, string body, string redirectUri);
        
    }
}
