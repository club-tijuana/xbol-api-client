using Odasoft.XBOL.Commons.Enums;

namespace Odasoft.XBOL.Models
{
    public class Client : BaseModel
    {
        public ClientType ClientType { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? BusinessName { get; set; }

        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public bool IsActive { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public Guid UpdatedBy { get; set; }

        public IList<Order> Orders { get; set; } = [];
        public IList<ClientCreditAccount> ClientCreditAccounts { get; set; } = [];
        public IList<Ticket> Tickets { get; set; } = [];
    }
}
