using scrm_dev_mvc.Models;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models.ViewModels
{
    /// <summary>
    /// Holds all the data needed for the Deal Details/Preview page.
    /// </summary>
    public class DealPreviewViewModel
    {
        public Deal Deal { get; set; }
        public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();
        public IEnumerable<Contact> Contacts { get; set; } = new List<Contact>();
    }
}
