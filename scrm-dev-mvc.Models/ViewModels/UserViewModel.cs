using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class UserViewModel
    {
        public Guid Id { get; set; }

        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First Name can only contain letters")]
        public string FirstName { get; set; }

        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters")]
        [RegularExpression(@"^[a-zA-Z]+$", ErrorMessage = "First Name can only contain letters")]
        public string LastName { get; set; }

        public string? Email { get; set; }

        public string? OrganizationName { get; set; }

        public string? Role {  get; set; }

        public bool IsSyncedWithGoogle { get; set; }
    }
}
