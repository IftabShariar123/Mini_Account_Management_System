namespace AccountManagement.Models
{
    public class VoucherDetailsVM
    {
        public int VoucherId { get; set; }
        public DateTime VoucherDate { get; set; }
        public string ReferenceNo { get; set; }
        public string VoucherType { get; set; }
        public List<VoucherLineDetail> LineItems { get; set; } = new();
    }

    public class VoucherLineDetail
    {
        public string AccountName { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
    }
}

