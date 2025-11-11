namespace Messenger.Web.DTOs
{
    public class LoginResponse
    {
        public bool IsSuccess { get; set; }
        public Guid UserId { get; set; }
        public string? Role { get; set; } = string.Empty;
        public string? Token { get; set; } = string.Empty;
    }
}
