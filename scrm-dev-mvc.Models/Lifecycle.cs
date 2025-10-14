using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Lifecycle
{
    public int Id { get; set; }

    public string LifeCycleStageName { get; set; } = null!;

    public virtual ICollection<Contact> Contacts { get; set; } = new List<Contact>();
}
