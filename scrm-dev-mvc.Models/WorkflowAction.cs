using scrm_dev_mvc.Models.Enums; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrm_dev_mvc.Models
{
    public class WorkflowAction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public WorkflowActionType ActionType { get; set; }


        public string ConditionJson { get; set; }


        // Example: {"Title": "Follow-up", "DaysDue": 3}
        // Example: {"EmailTemplateId": 5, "SendTo": "Contact"}
        public string ParametersJson { get; set; }


        [Required]
        public int WorkflowId { get; set; }
        [ForeignKey("WorkflowId")]
        public Workflow Workflow { get; set; }
    }
}