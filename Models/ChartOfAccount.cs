using System.ComponentModel.DataAnnotations;

namespace AccountManagement.Models
{
    public class ChartOfAccount
    {
        [Key]
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public int? ParentAccountId { get; set; }
        public bool IsActive { get; set; }
        public string? Remarks { get; set; }
    }
}
