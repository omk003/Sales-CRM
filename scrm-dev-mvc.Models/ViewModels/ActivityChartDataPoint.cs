using System;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class ActivityChartDataPoint
    {
        public string Date { get; set; } // Formatted as "YYYY-MM-DD"
        public int Count { get; set; }
    }
}