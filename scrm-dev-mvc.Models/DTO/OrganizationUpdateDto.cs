namespace scrm_dev_mvc.Models.DTO
{
    public class OrganizationUpdateDto
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
    }
}