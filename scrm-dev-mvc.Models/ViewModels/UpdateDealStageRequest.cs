using System.ComponentModel.DataAnnotations;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class UpdateDealStageRequest
    {
        [Required]
        public int DealId { get; set; }

        [Required]
        public string NewStageName { get; set; }
    }
}
