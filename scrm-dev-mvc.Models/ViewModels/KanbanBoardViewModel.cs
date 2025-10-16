using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class KanbanBoardViewModel
    {
        public List<string> DealStages { get; set; }
        public Dictionary<string, List<Deal>> DealsByStage { get; set; }
    }
}
