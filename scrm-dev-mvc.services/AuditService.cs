using scrm_dev_mvc.Data.Repository.IRepository;
using scrm_dev_mvc.Models;
using scrm_dev_mvc.Models.DTO;
using scrm_dev_mvc.services;

namespace scrm_dev_mvc.Services
{
    public class AuditService : IAuditService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async System.Threading.Tasks.Task LogChangeAsync(AuditLogDto dto)
        {
            var auditLog = new Audit
            {
                OwnerId = dto.OwnerId,
                RecordId = dto.RecordId,
                TableName = dto.TableName,
                FieldName = dto.FieldName,
                OldValue = dto.OldValue ?? "[NULL]",
                NewValue = dto.NewValue ?? "[NULL]",
                Timestamp = DateTime.UtcNow
            };

            await _unitOfWork.Audits.AddAsync(auditLog);
            // We DO NOT call SaveChangesAsync here.
            // The calling service (e.g., TaskService) will do that.
        }
    }
}