using scrm_dev_mvc.Models.DTO;

namespace scrm_dev_mvc.services
{
    public interface IAuditService
    {
        Task LogChangeAsync(AuditLogDto dto);
    }
}