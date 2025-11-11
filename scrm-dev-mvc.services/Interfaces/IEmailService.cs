using scrm_dev_mvc.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.services.Interfaces
{

    
        public interface IEmailService
        {
            Task<SendEmailLogicResult> SendEmailAsync(
                Guid senderUserId,
                string contactEmail, // <-- No more contactId
                string subject,
                string body,
                string redirectUri
            );
        }
    

}
