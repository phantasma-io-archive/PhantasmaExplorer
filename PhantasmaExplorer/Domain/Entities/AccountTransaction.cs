namespace Phantasma.Explorer.Domain.Entities
{
    public class AccountTransaction
    {
        public string AccountId { get; set; }
        public string TransactionId { get; set; }
       
        public Account Account { get; set; }
        public Transaction Transaction { get; set; }
    }
}
