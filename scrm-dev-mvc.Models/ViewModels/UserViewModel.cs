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
      
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string? Email { get; set; }

        public string? OrganizationName { get; set; }

        public string? Role {  get; set; }

        public bool IsSyncedWithGoogle { get; set; }
    }
}
