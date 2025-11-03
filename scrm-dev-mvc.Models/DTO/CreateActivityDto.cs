using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace scrm_dev_mvc.Models.DTO
{
        public class CreateActivityDto
        {
            /// <summary>
            /// The ID of the user creating the activity.
            /// </summary>
            public Guid OwnerId { get; set; }

            /// <summary>
            /// The name of the activity type, e.g., "Call", "Email", "Task".
            /// </summary>
            public string ActivityTypeName { get; set; } = null!;

            /// <summary>
            /// Notes for the activity.
            /// </summary>
            public string? Notes { get; set; }

            /// <summary>
            /// The date/time the activity occurred. Defaults to DateTime.UtcNow if not set.
            /// </summary>
            public DateTime? ActivityDate { get; set; }

            /// <summary>
            /// The due date, primarily for tasks.
            /// </summary>
            public DateTime? DueDate { get; set; }

            /// <summary>
            /// The status, e.g., "Pending", "Completed".
            /// Defaults to "Pending" for future tasks, "Completed" otherwise.
            /// </summary>
            public string? Status { get; set; }

            // --- Associations ---

            /// <summary>
            /// Optional: The ID of the associated Contact.
            /// </summary>
            public int? ContactId { get; set; }

            /// <summary>
            /// Optional: The ID of the associated Deal.
            /// </summary>
            public int? DealId { get; set; }

            // --- Source Record Linking ---

            /// <summary>
            /// Optional: The ID of the source record (e.g., an EmailMessage's ID).
            /// </summary>
            public int? SubjectId { get; set; }

            /// <summary>
            /// Optional: The type of the source record (e.g., "email_message").
            /// </summary>
            public string? SubjectType { get; set; }
        }
    
}
