using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.Enums
{
    public enum WorkflowTrigger
    {
        [Display(Name = "Contact: Is Created")]
        ContactCreated = 1,

        [Display(Name = "Contact Status: Changes to Open")]
        ContactLeadStatusChangesToOpen = 2,
        [Display(Name = "Contact Status: Changes to Connected")]
        ContactLeadStatusChangesToConnected = 3,
        [Display(Name = "Contact Status: Changes to Qualified")]
        ContactLeadStatusChangesToQualified = 4,
        [Display(Name = "Contact Status: Changes to Disqualified")]
        ContactLeadStatusChangesToDisqualified = 5,
        [Display(Name = "Contact Status: Changes to Bad Timing")]
        ContactLeadStatusChangesToBadTiming = 6,
        [Display(Name = "Contact Status: Changes to High Price")]
        ContactLeadStatusChangesToHighPrice = 7,

        [Display(Name = "Task: Is Completed")]
        TaskCompleted = 10,
        [Display(Name = "Task: Is Created")]
        TaskCreated = 11
    }
}