using Microsoft.AspNetCore.Mvc.Rendering;
using scrm_dev_mvc.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class WorkflowCreateViewModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [Display(Name = "When this happens... (Trigger)")]
        public WorkflowTrigger Trigger { get; set; }

        [Display(Name = "Change Contact Lead Status")]
        public bool Action_ChangeLeadStatus { get; set; }

        [Display(Name = "Change Contact LifeCycle Stage")]
        public bool Action_ChangeLifeCycleStage { get; set; }

        [Display(Name = "Create a new Task")]
        public bool Action_CreateTask { get; set; }

        [Display(Name = "Set Lead Status to")]
        public LeadStatusEnum ChangeLeadStatus_NewStatusId { get; set; }

        [Display(Name = "Set LifeCycle Stage to")]
        public int ChangeLifeCycleStage_NewStageId { get; set; }

        [Display(Name = "Task Title")]
        public string Task_Title { get; set; } = "Follow-up";
        [Display(Name = "Days Until Due")]
        public int Task_DaysDue { get; set; } = 3;
        [Display(Name = "Task Type (e.g., Call, Email)")]
        public string Task_TaskType { get; set; } = "Call";
        [Display(Name = "Priority")]
        public int Task_PriorityId { get; set; }
        [Display(Name = "Status")]
        public int Task_StatusId { get; set; }

        public string? ChangeLeadStatus_ConditionKey { get; set; }
        public string? ChangeLeadStatus_ConditionValue { get; set; }
        public string? ChangeLifeCycleStage_ConditionKey { get; set; }
        public string? ChangeLifeCycleStage_ConditionValue { get; set; }
        public string? Task_ConditionKey { get; set; }
        public string? Task_ConditionValue { get; set; }

        public string? Task_ConditionTaskType { get; set; }  // e.g. "email", "call"
        public int? Task_ConditionLeadStatus { get; set; }
        

        public IEnumerable<SelectListItem>? AvailableTriggers { get; set; }
        public IEnumerable<SelectListItem>? AvailableLeadStatuses { get; set; }
        public IEnumerable<SelectListItem>? AvailableLifeCycleStages { get; set; }
        public IEnumerable<SelectListItem>? AvailablePriorities { get; set; }
        public IEnumerable<SelectListItem>? AvailableTaskStatuses { get; set; }
    }
}