using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class CompanyViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Company name is required.")]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "City can only contain letters and spaces.")]
        public string? City { get; set; }

        [StringLength(100)]
        [RegularExpression(@"^[a-zA-Z\s]+$", ErrorMessage = "Country can only contain letters and spaces.")]
        public string? Country { get; set; }

        public Guid? userId { get; set; }

        public string? userName { get; set; }

        public DateTime? CreatedDate { get; set; }

        [Required(ErrorMessage = "Domain is required.")]
        [RegularExpression(@"^([a-zA-Z0-9-]+\.)+[a-zA-Z]{2,}$", ErrorMessage = "Domain is invalid")]
        public string Domain { get; set; }
    }
}
