namespace Odasoft.XBOL.Models
{
    public class ClientCreditAccount : BaseModel
    {
        public long ClientId { get; set; }
        public Client Client { get; set; } = null!;

        public decimal CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }

        public IList<ClientCreditTransaction> ClientCreditTransactions { get; set; } = [];
    }
}
