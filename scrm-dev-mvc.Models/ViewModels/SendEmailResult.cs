namespace scrm_dev_mvc.Models.ViewModels
{
    public class SendEmailResult
    {
        public bool IsSuccess { get; set; }
        public bool AuthenticationRequired { get; set; }
        public string? AuthenticationUrl { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
