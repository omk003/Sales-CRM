using System;

namespace scrm_dev_mvc.Models.DTO
{
    public class AuditLogDto
    {
        public Guid OwnerId { get; set; }
        public int RecordId { get; set; }
        public string TableName { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}