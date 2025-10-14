using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string Email { get; set; } = null!;

    public int RoleId { get; set; }

    public int? OrganizationId { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public long? LastProcessedUid { get; set; }

    public DateTime? LastCheckedTime { get; set; }

    public string? PasswordHash { get; set; }

    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }

    public bool IsSyncedWithGoogle { get; set; } = false;

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual ICollection<Audit> Audits { get; set; } = new List<Audit>();

    public virtual ICollection<Call> Calls { get; set; } = new List<Call>();

    public virtual ICollection<Company> Companies { get; set; } = new List<Company>();

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();

    public virtual ICollection<Deal> Deals { get; set; } = new List<Deal>();

    public virtual ICollection<EmailThread> EmailThreads { get; set; } = new List<EmailThread>();

    public virtual GmailCred? GmailCred { get; set; }

    public virtual Organization Organization { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<WorkflowTemplate> WorkflowTemplates { get; set; } = new List<WorkflowTemplate>();
}
