// In a new file: Models/ViewModels/TaskCreateViewModel.cs

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class TaskCreateViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a priority.")]
        public int PriorityId { get; set; }

        [Required(ErrorMessage = "Please select a status.")]
        public int StatusId { get; set; }

        public DateTime? DueDate { get; set; }

        // These IDs are hidden fields, set by the page that opens the modal
        public int? ContactId { get; set; }
        public int? CompanyId { get; set; }
        public int? DealId { get; set; }

        // Dropdown lists
        public IEnumerable<SelectListItem> Priorities { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Statuses { get; set; } = new List<SelectListItem>();
    }
}