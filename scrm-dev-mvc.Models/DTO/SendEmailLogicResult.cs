using scrm_dev_mvc.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.DTO
{
    public class SendEmailLogicResult
    {
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Set to true if the contact (or other required entity) was not found.
        /// </summary>
        public bool IsNotFound { get; set; } // <-- ADDED THIS PROPERTY

        public bool AuthenticationRequired { get; set; }
        public string? AuthenticationUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public EmailMessage? SentMessage { get; set; }

        public static SendEmailLogicResult FromFailure(SendEmailResult gmailResult)
        {
            return new SendEmailLogicResult
            {
                IsSuccess = false,
                AuthenticationRequired = gmailResult.AuthenticationRequired,
                AuthenticationUrl = gmailResult.AuthenticationUrl,
                ErrorMessage = gmailResult.ErrorMessage
            };
        }
    }
}
