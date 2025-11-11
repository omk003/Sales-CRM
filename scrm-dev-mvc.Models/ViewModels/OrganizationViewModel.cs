using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class OrganizationViewModel
    {
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public List<UserInOrganizationViewModel> Users { get; set; }
        public string CurrentUserRole { get; set; } // e.g., "superadmin", "admin", "user"
    }

    public class UserInOrganizationViewModel
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // "superadmin", "admin", "user"
    }

   
}
