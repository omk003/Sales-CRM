using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.Enums
{
    public enum WorkflowActionType
    {
        [Display(Name = "Create a new Task")]
        CreateTask = 1,

        [Display(Name = "Send an Email")]
        SendEmail = 2,

        // --- REPLACED 'UpdateContactField' ---
        [Display(Name = "Change Contact Lead Status")]
        ChangeLeadStatus = 3,

        [Display(Name = "Change Contact LifeCycle Stage")]
        ChangeLifeCycleStage = 4
    }
}