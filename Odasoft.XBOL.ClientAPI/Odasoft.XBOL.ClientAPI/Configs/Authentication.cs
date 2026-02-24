namespace Odasoft.XBOL.ClientAPI.Configs
{
    public class Authentication
    {
        public List<AllowedUser> AllowedUsers { get; set; } = new();
    }

    public class AllowedUser
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
