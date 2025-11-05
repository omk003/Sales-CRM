using scrm_dev_mvc.Models.ViewModels;
using System.Collections.Generic;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class WorkspaceViewModel
    {
        public List<TaskSummaryViewModel> PendingTasks { get; set; } = new List<TaskSummaryViewModel>();
        public List<TaskSummaryViewModel> DueTasks { get; set; } = new List<TaskSummaryViewModel>();

        public List<TaskSummaryViewModel> CompletedTasks { get; set; } = new List<TaskSummaryViewModel>();
        // Key: "tasks", "emails", "calls"
        public Dictionary<string, List<ActivityChartDataPoint>> MyActivityData { get; set; } = new Dictionary<string, List<ActivityChartDataPoint>>();

        public bool IsAdmin { get; set; }

        // Key: "tasks", "emails", "calls"
        public Dictionary<string, List<UserActivityStats>> TeamActivityData { get; set; } = new Dictionary<string, List<UserActivityStats>>();
    }
}