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
            public DateTime ActivityDate { get; set; }

            /// <summary>
            /// The due date, primarily for tasks.
            /// </summary>
            public DateTime? DueDate { get; set; }

            
            public string Status { get; set; }

           
            public int? ContactId { get; set; }

            public int? DealId { get; set; }

            public int SubjectId { get; set; }

          
            public string SubjectType { get; set; }
        }
    
}
