// In scrm_dev_mvc/Models/Task.cs

using scrm_dev_mvc.Models;
namespace scrm_dev_mvc.Models;
public partial class Task
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int PriorityId { get; set; }
    public int StatusId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Guid OwnerId { get; set; }

    // --- ADD THESE NEW PROPERTIES ---
    public int? ContactId { get; set; }
    public int? CompanyId { get; set; }
    public int? DealId { get; set; }


    public string? TaskType { get; set; }

    public int OrganizationId { get; set; }
    // --- Navigation Properties ---
    public virtual Priority Priority { get; set; } = null!;
    public virtual scrm_dev_mvc.Models.TaskStatus Status { get; set; } = null!;

    // --- NAVIGATION PROPERTIES ---
    public virtual Contact? Contact { get; set; }
    public virtual Company? Company { get; set; }
    public virtual Deal? Deal { get; set; }
}