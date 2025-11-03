using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models;

public partial class Deal
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Deal Name is required.")]
    public string Name { get; set; }
    public Guid? OwnerId { get; set; }
    [Required(ErrorMessage = "Please select a deal stage.")] // <-- ADD THIS
    [Display(Name = "Deal Stage")]
    public int StageId { get; set; }

    public int? CompanyId { get; set; }

    public DateTime? CloseDate { get; set; }

    public decimal? Value { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public virtual Company? Company { get; set; }

    public virtual ICollection<DealLineItem> DealLineItems { get; set; } = new List<DealLineItem>();

    public virtual User? Owner { get; set; }

    public virtual Stage? Stage { get; set; }

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
