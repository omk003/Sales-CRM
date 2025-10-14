using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class DealLineItem
{
    public int Id { get; set; }

    public int DealId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public virtual Deal Deal { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
