using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.Models.ViewModels;
using scrm_dev_mvc.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services
{

    public class EmailService : IEmailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGmailService _gmailService;
        private readonly IActivityService _activityService;

        public EmailService(
            IUnitOfWork unitOfWork,
            IGmailService gmailService,
            IActivityService activityService)
        {
            _unitOfWork = unitOfWork;
            _gmailService = gmailService;
            _activityService = activityService;
        }

        public async Task<SendEmailLogicResult> SendEmailAsync(
            Guid senderUserId,
            string contactEmail,
            string subject,
            string body,
            string redirectUri)
        {
            // --- 1. Find Contact (Logic is now in the service) ---
            var contact = await _unitOfWork.Contacts
                                .FirstOrDefaultAsync(c => c.Email == contactEmail);

            if (contact == null)
            {
                return new SendEmailLogicResult
                {
                    IsSuccess = false,
                    IsNotFound = true, // <-- Set the new property
                    ErrorMessage = $"Contact with email {contactEmail} not found."
                };
            }

            // --- 2. Call GmailService to send ---
            SendEmailResult gmailResult = await _gmailService.SendEmailAsync(
                senderUserId,
                contactEmail,
                subject,
                body,
                redirectUri
            );

            if (!gmailResult.IsSuccess)
            {
                return SendEmailLogicResult.FromFailure(gmailResult);
            }

            // --- 3. Create Thread, Message, and Activity (on success) ---
            try
            {
                // We use contact.Id (which we found) and senderUserId
                var thread = await GetOrCreateThreadAsync(senderUserId, contact.Id, subject);

                var message = new EmailMessage
                {
                    ThreadId = thread.Id,
                    Body = body,
                    SentAt = DateTime.UtcNow,
                    Direction = "outbound",
                    IsDeleted = false
                };
                await _unitOfWork.EmailMessages.AddAsync(message);
                await _unitOfWork.SaveChangesAsync(); // Save to get message.Id

                var activityDto = new CreateActivityDto
                {
                    OwnerId = senderUserId,
                    ActivityTypeName = "Email",
                    Notes = $"Sent email: {subject}",
                    ActivityDate = message.SentAt,
                    Status = "Completed",
                    ContactId = contact.Id, // <-- Use the ID we found
                    SubjectId = message.Id,
                    SubjectType = "email_message"
                };
                await _activityService.CreateActivityAsync(activityDto);

                return new SendEmailLogicResult
                {
                    IsSuccess = true,
                    SentMessage = message
                };
            }
            catch (Exception ex)
            {
                return new SendEmailLogicResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Email was sent, but logging failed: {ex.Message}"
                };
            }
        }

        // Helper method (same as before)
        private async Task<EmailThread> GetOrCreateThreadAsync(Guid userId, int contactId, string subject)
        {
            var thread = await _unitOfWork.EmailThreads
                .FirstOrDefaultAsync(t => t.UserId == userId && t.ContactId == contactId);

            var now = DateTime.UtcNow;

            if (thread == null)
            {
                thread = new EmailThread
                {
                    UserId = userId,
                    ContactId = contactId,
                    Subject = subject,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _unitOfWork.EmailThreads.AddAsync(thread);
            }
            else
            {
                thread.Subject = subject;
                thread.UpdatedAt = now;
                thread.IsArchived = false;
                _unitOfWork.EmailThreads.Update(thread);
            }

            await _unitOfWork.SaveChangesAsync();
            return thread;
        }
    }

    }
