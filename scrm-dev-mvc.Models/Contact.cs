using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace scrm_dev_mvc.Models;

public partial class Contact
{
    public int Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public int LeadStatusId { get; set; }

    public int LifeCycleStageId { get; set; }

    public string? Number { get; set; }

    public string Email { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public Guid OwnerId { get; set; }

    public int? CompanyId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int OrganizationId { get; set; }

    [ForeignKey("OrganizationId")]
    public Organization Organization { get; set; } = null!;

    public string? JobTitle { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<Call> Calls { get; set; } = new List<Call>();

    public virtual Company? Company { get; set; }

    public virtual ICollection<EmailThread> EmailThreads { get; set; } = new List<EmailThread>();

    public virtual LeadStatus? LeadStatus { get; set; }

    public virtual Lifecycle LifeCycleStage { get; set; } = null!;

    public virtual User? Owner { get; set; }

    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();
}
