using scrm_dev_mvc.Models.Enums; 
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrm_dev_mvc.Models
{
    public class Workflow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public WorkflowTrigger Event { get; set; }

        public bool IsActive { get; set; } = true;

        
        [Required]
        public int OrganizationId { get; set; }
        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }

        public ICollection<WorkflowAction> Actions { get; set; } = new List<WorkflowAction>();
    }
}