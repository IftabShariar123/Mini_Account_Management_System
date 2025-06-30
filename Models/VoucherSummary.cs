namespace AccountManagement.Models
{
    public class VoucherSummary
    {
        public int VoucherId { get; set; }
        public DateTime VoucherDate { get; set; }
        public string ReferenceNo { get; set; }
        public string VoucherType { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
    }
}
