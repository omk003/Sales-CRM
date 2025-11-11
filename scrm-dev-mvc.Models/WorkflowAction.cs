using scrm_dev_mvc.Models.Enums; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrm_dev_mvc.Models
{
    public class WorkflowAction
    {
        [Key]
        public int Id { get; set; }

        // The "DO" this
        [Required]
        public WorkflowActionType ActionType { get; set; }

        // Stores details for the action, e.g., task title, email template ID
        // We use JSON for flexibility.
        // Example: {"Title": "Follow-up", "DaysDue": 3}
        // Example: {"EmailTemplateId": 5, "SendTo": "Contact"}
        public string ParametersJson { get; set; }

        // --- Relationships ---

        // The parent Workflow this action belongs to
        [Required]
        public int WorkflowId { get; set; }
        [ForeignKey("WorkflowId")]
        public Workflow Workflow { get; set; }
    }
}