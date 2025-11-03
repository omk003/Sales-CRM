using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using global::scrm_dev_mvc.Data.Repository.IRepository;
using global::scrm_dev_mvc.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{
   
        // Inherit from BackgroundService for a reliable, long-running task
        public class GmailPollingHostedService : BackgroundService
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly ILogger<GmailPollingHostedService> _logger;
            private readonly GoogleAuthorizationCodeFlow _googleFlow;
            private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(1);

            public GmailPollingHostedService(
                IServiceProvider serviceProvider,
                ILogger<GmailPollingHostedService> logger,
                IConfiguration configuration) // Inject IConfiguration to build the flow
            {
                _serviceProvider = serviceProvider;
                _logger = logger;

                // --- Initialize the Google Flow (copied from your GmailService) ---
                _googleFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = configuration["Google:ClientId"],
                        ClientSecret = configuration["Google:ClientSecret"]
                    },
                    Scopes = new[] { "https://mail.google.com/" } // Only need the mail scope for IMAP
                });
            }

            protected override async System.Threading.Tasks.Task ExecuteAsync(CancellationToken stoppingToken)
            {
                _logger.LogInformation("Gmail Polling Service is starting.");

                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // --- 1. Create a new service scope for this cycle ---
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        // --- 2. Efficiently query ONLY users who need polling ---
                        // --- NEW CORRECT LINE ---
                        var usersToPoll = await unitOfWork.Users.GetAllAsync(
                            predicate: u => u.GmailCred != null && !string.IsNullOrEmpty(u.GmailCred.GmailRefreshToken),
                            asNoTracking: false, // Keep tracking so changes are saved
                            includes: u => u.GmailCred // Pass the include as a lambda expression
                        );

                        foreach (var user in usersToPoll)
                            {
                                if (stoppingToken.IsCancellationRequested) break;

                                try
                                {
                                    await PollUserInboxAsync(user, unitOfWork, stoppingToken);
                                }
                                catch (Exception userEx)
                                {
                                    // Log error for a specific user but continue the loop
                                    _logger.LogError(userEx, $"Failed to poll inbox for user {user.Email}.");
                                }
                            }

                            // --- 3. Save all changes (e.g., new UIDs) ONCE per cycle ---
                            await unitOfWork.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error for the entire polling cycle
                        _logger.LogError(ex, "An error occurred in the Gmail polling cycle.");
                    }

                    await System.Threading.Tasks.Task.Delay(_pollInterval, stoppingToken);
                }

                _logger.LogInformation("Gmail Polling Service is stopping.");
            }

            private async System.Threading.Tasks.Task PollUserInboxAsync(User user, IUnitOfWork unitOfWork, CancellationToken token)
            {
                _logger.LogDebug($"Polling for user {user.Email}...");

                // --- 3. Token Refresh Logic ---
                string newAccessToken;
                try
                {
                    var credential = new UserCredential(_googleFlow, user.Email, new TokenResponse
                    {
                        RefreshToken = user.GmailCred.GmailRefreshToken
                    });

                    if (await credential.RefreshTokenAsync(token))
                    {
                        newAccessToken = credential.Token.AccessToken;
                        // Save the new refresh token if Google issued one
                        if (credential.Token.RefreshToken != null)
                        {
                            user.GmailCred.GmailRefreshToken = credential.Token.RefreshToken;
                            unitOfWork.Users.Update(user); // UoW will track this change
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to refresh token for user {user.Email}.");
                        return; // Skip this user
                    }
                }
                catch (TokenResponseException authEx)
                {
                    // This happens if the user revoked access
                    _logger.LogWarning(authEx, $"Auth token revoked for user {user.Email}.");
                    user.GmailCred.GmailRefreshToken = null; // Clear the bad token
                    user.GmailCred.GmailAccessToken = null;
                    unitOfWork.Users.Update(user); // UoW will track this change
                    return; // Skip this user
                }

                // --- Your IMAP Logic (Now using the fresh Access Token) ---
                using var client = new ImapClient();
                await client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, token);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(user.Email, newAccessToken), token);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly, token);

                IList<UniqueId> uids;
                if (user.LastProcessedUid == null || user.LastProcessedUid == 0)
                {
                    // First run: Get emails from the last 24 hours to avoid overload
                    var query = SearchQuery.DeliveredAfter(DateTime.UtcNow.AddDays(-1));
                    uids = await inbox.SearchAsync(query, token);
                }
                else
                {
                    var minUid = new UniqueId((uint)user.LastProcessedUid.Value + 1);
                    var range = new UniqueIdRange(minUid, UniqueId.MaxValue);
                    var query = SearchQuery.Uids(range);
                    uids = await inbox.SearchAsync(query, token);
                }

                if (uids.Any())
                {
                    _logger.LogInformation($"Found {uids.Count} new email(s) for {user.Email}.");
                    foreach (var uid in uids.OrderBy(u => u.Id)) // Process in order
                    {
                        var message = await inbox.GetMessageAsync(uid, token);
                        _logger.LogDebug($"[{user.Email}] New email: {message.Subject}");

                        //
                        // --- TODO: Add your logic here ---
                        // Example: await _emailService.IngestInboundEmail(user, message);
                        //
                    }

                    // Update the user's last processed UID.
                    // This change will be saved by the ExecuteAsync method.
                    user.LastProcessedUid = uids.Max(u => u.Id);
                    user.LastCheckedTime = DateTime.UtcNow;
                    unitOfWork.Users.Update(user);
                }
                else
                {
                    _logger.LogDebug($"No new mail for {user.Email}.");
                }

                await client.DisconnectAsync(true, token);
            }
        }
    }
