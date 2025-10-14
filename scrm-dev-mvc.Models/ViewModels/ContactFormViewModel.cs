using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class ContactFormViewModel
    {
        public ContactDto Contact { get; set; }
        public List<LeadStatus> LeadStatuses { get; set; } = new();
        public List<Lifecycle> Lifecycle { get; set; } = new();

        public List<User> Users { get; set; } = new();
    }

}
