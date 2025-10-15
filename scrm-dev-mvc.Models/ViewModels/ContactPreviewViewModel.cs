using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class ContactPreviewViewModel
    {
        public string? ProfileImageUrl { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        public string JobTitle { get; set; }
        public int Id { get; set; }
        public DateTime? CreatedAt { get; set; }

        public DateTime LastActivityDate { get; set; }

        public string LifecycleStage { get; set; }

        public string LeadStatus { get; set; }

        public Company Company { get; set; }

        public List<Deal> Deals { get; set; }

        public List<Activity> Activities { get; set; }
    }
}
