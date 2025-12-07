using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Product
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public decimal? Price { get; set; }

    public Guid UserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<DealLineItem> DealLineItems { get; set; } = new List<DealLineItem>();

    public virtual User User { get; set; } = null!;
}
