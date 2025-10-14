using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class ContactDto
    {
        public int Id { get; set; }
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name can only contain letters and spaces.")]
        public string? FirstName { get; set; }
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "First name can only contain letters and spaces.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string? LastName { get; set; } 

        public int? LeadStatusId { get; set; }

        public int? LifeCycleStageId { get; set; }
        [RegularExpression(@"^(\+91)?[0-9]{10}$", ErrorMessage = "Enter a valid 10-digit or +91 phone number.")]
        public string? Number { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = null!;
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Job Title can only contain letters and spaces.")]
        [StringLength(100, ErrorMessage = "Job title cannot exceed 100 characters.")]
        public string? JobTitle { get; set; }


        public Guid? OwnerId { get; set; }

    }
}
