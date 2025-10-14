using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class WorkflowTemplate
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public string? WorkflowName { get; set; }

    public string? WorkflowJson { get; set; }

    public virtual ICollection<EmailTemplate> EmailTemplates { get; set; } = new List<EmailTemplate>();

    public virtual ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();

    public virtual User User { get; set; } = null!;
}
