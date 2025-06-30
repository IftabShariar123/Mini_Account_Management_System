namespace AccountManagement.Models
{
    public class VoucherEntryVM
    {
        public DateTime VoucherDate { get; set; }
        public string ReferenceNo { get; set; }
        public string VoucherType { get; set; }
        public List<VoucherLineItem> LineItems { get; set; } = new();
    }
    public class VoucherLineItem
    {
        public int AccountId { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
    }
}
