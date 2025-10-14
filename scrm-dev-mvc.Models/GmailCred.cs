using System;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models;

public partial class GmailCred
{
    public string Email { get; set; } = null!;

    public string? GmailAccessToken { get; set; }

    public string? GmailRefreshToken { get; set; }

    public virtual User EmailNavigation { get; set; } = null!;
}
