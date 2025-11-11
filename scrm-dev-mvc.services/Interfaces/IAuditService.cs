using scrm_dev_mvc.Models.DTO;

namespace scrm_dev_mvc.services.Interfaces
{
    public interface IAuditService
    {
        Task LogChangeAsync(AuditLogDto dto);
    }
}