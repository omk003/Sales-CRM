using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrm_dev_mvc.Models
{
    public class Invitation
    {
        [Key]
        public Guid Id { get; set; }

        public Guid SenderId { get; set; }

        [Required]
        public string InvitationCode { get; set; } 

        [Required]
        public string Email { get; set; } 

        [Required]
        public int OrganizationId { get; set; }

        [Required]
        public int RoleId { get; set; } 

        public DateTime SentDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; }
        public bool IsAccepted { get; set; } = false;

        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }
    }
}