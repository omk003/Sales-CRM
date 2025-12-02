using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class TaskUpdateViewModel
    {
        public int TaskId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Task status is required.")]
        public int StatusId { get; set; } 

        public DateTime? DueDate { get; set; }

        public IEnumerable<SelectListItem> TaskStatuses { get; set; } = new List<SelectListItem>();

        public int? ContactId { get; set; }
        public string? ContactName { get; set; }
        public int? CurrentContactLeadStatusId { get; set; } 
        public IEnumerable<SelectListItem> LeadStatuses { get; set; } = new List<SelectListItem>();

        public int? DealId { get; set; }
        public string? DealName { get; set; }
        public int? CurrentDealStageId { get; set; } 

        public string? TaskType { get; set; } 
        public string? ContactEmail { get; set; }
        public string? ContactPhoneNumber { get; set; }
        public IEnumerable<SelectListItem> DealStages { get; set; } = new List<SelectListItem>();
    }
}