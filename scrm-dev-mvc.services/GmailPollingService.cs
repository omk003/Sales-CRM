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
                    var users = _unitOfWork.Users.GetAll(); 
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

            await client.AuthenticateAsync(new MailKit.Security.SaslMechanismOAuth2(user.Email, user.GmailCred.GmailAccessToken), token);

            var inbox = client.Inbox;
            await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly, token);

            IList<MailKit.UniqueId> uids;



            if (user.LastProcessedUid == null || user.LastProcessedUid == 0)
            {
                var query = MailKit.Search.SearchQuery.DeliveredAfter(DateTime.UtcNow.Date);
                uids = await inbox.SearchAsync(query, token);
            }
            else
            {
               
                var minUid = new MailKit.UniqueId((uint)user.LastProcessedUid.Value + 1);
                var range = new MailKit.UniqueIdRange(minUid, MailKit.UniqueId.MaxValue);

                var query = MailKit.Search.SearchQuery.Uids(range);

                uids = await inbox.SearchAsync(query, token);
            }

            if (uids.Any())
            {
                foreach (var uid in uids)
                {
                    var message = await inbox.GetMessageAsync(uid, token);
                    Console.WriteLine($"[{user.Email}] New email: {message.Subject} from {string.Join(", ", message.From)} at {message.Date}");

                   
                }

 
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
