using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class Organization
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
}
