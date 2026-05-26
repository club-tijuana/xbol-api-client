namespace Odasoft.XBOL.Commons.Security
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? Token { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
