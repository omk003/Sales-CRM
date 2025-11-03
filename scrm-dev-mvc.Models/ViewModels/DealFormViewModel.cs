using scrm_dev_mvc.Models;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models.ViewModels
{
    /// <summary>
    /// View model to hold a Deal and the necessary lists for dropdowns
    /// in the Create and Update forms.
    /// </summary>
    public class DealFormViewModel
    {
        public Deal Deal { get; set; } = new Deal();
        public IEnumerable<User> Users { get; set; } = new List<User>();
        public IEnumerable<Stage> Stages { get; set; } = new List<Stage>();
        public IEnumerable<Company> Companies { get; set; } = new List<Company>();
    }
}
