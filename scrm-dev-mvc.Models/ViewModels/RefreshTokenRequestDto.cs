namespace scrm_dev_mvc.Models.ViewModels
{
    public class RefreshTokenRequestDto
    {
        public Guid UserId { get; set; }

        public required string  RefreshToken { get; set; }
    }
}
