using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class TaskTemplate
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int WorkflowId { get; set; }

    public DateTime? Duedate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual WorkflowTemplate Workflow { get; set; } = null!;
}
