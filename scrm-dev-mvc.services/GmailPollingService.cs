using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using System;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Services
{
    public class GmailPollingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeSpan _pollInterval = TimeSpan.FromMinutes(1);

        public GmailPollingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public void StartPolling(CancellationToken token)
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var users = _unitOfWork.Users.GetAll(); // Fetch all users with Gmail tokens
                    foreach (var user in users)
                    {
                        if (string.IsNullOrEmpty(user.GmailCred.GmailAccessToken)) continue;

                        try
                        {
                            await PollUserInbox(user, token);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error polling {user.Email}: {ex.Message}");
                        }
                    }

                    await System.Threading.Tasks.Task.Delay(_pollInterval, token);
                }
            });
        }

        private async System.Threading.Tasks.Task PollUserInbox(User user, CancellationToken token)
        {
            using var client = new MailKit.Net.Imap.ImapClient();
            await client.ConnectAsync("imap.gmail.com", 993, MailKit.Security.SecureSocketOptions.SslOnConnect, token);

            // Authenticate using XOAUTH2
            await client.AuthenticateAsync(new MailKit.Security.SaslMechanismOAuth2(user.Email, user.GmailCred.GmailAccessToken), token);

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly, token);

            IList<MailKit.UniqueId> uids;

            // ... code to connect and open inbox ...


            if (user.LastProcessedUid == null || user.LastProcessedUid == 0)
            {
                // First run: fetch emails from today using a SearchQuery
                var query = MailKit.Search.SearchQuery.DeliveredAfter(DateTime.UtcNow.Date);
                uids = await inbox.SearchAsync(query, token);
            }
            else
            {
                // Subsequent runs: Build a SearchQuery for the UID range.
                // This is the most reliable way.
                // ... inside the else block ...
                var minUid = new MailKit.UniqueId((uint)user.LastProcessedUid.Value + 1);
                var range = new MailKit.UniqueIdRange(minUid, MailKit.UniqueId.MaxValue);

                // This is the single, correct line to create the query
                var query = MailKit.Search.SearchQuery.Uids(range);

                uids = await inbox.SearchAsync(query, token);
            }

            // ... the rest of the logic remains the same ...
            // Only proceed if there are new emails to process
            if (uids.Any())
            {
                foreach (var uid in uids)
                {
                    var message = await inbox.GetMessageAsync(uid, token);
                    Console.WriteLine($"[{user.Email}] New email: {message.Subject} from {string.Join(", ", message.From)} at {message.Date}");

                    // Add your specific logic here to handle the new email
                    // (e.g., create a contact, an activity, etc.)
                }

                // --- IMPROVED LOGIC ---
                // Find the highest UID from this batch and update the user object ONCE.
                var maxUid = uids.Max(u => u.Id);
                user.LastProcessedUid = maxUid;
                user.LastCheckedTime = DateTime.UtcNow;

                _unitOfWork.Users.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }

            await client.DisconnectAsync(true, token);
        }
    }
}
