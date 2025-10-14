using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class EmailTemplate
{
    public int Id { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public int WorkflowId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual WorkflowTemplate Workflow { get; set; } = null!;
}
