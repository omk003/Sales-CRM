using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class CompanyPreviewViewModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string Domain { get; set; } = null!;

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public bool? IsDeleted { get; set; }

        public Guid? UserId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public int OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; } = null!;

        public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

        public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

        public virtual User? User { get; set; }

        public virtual IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
    }
}
