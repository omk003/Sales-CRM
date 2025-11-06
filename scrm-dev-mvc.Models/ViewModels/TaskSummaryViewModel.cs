using System;

namespace scrm_dev_mvc.Models.ViewModels
{
    public class TaskSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime DueDate { get; set; }

        public string? TaskType { get; set; }
        public string RelatedTo { get; set; } // e.g., "Jane Doe (Contact)" or "Acme Inc (Deal)"
    }
}