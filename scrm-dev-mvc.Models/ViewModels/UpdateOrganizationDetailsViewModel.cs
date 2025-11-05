using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class UpdateOrganizationDetailsViewModel
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        [RegularExpression(@"^(\+91)?[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit phone number.")]
        public string? PhoneNumber { get; set; }
    }
}
