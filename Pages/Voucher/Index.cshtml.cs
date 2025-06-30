using AccountManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.Voucher
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        public IndexModel(IConfiguration config) => _config = config;

        public List<VoucherSummary> Vouchers { get; set; }

        public async Task OnGetAsync()
        {
            Vouchers = new List<VoucherSummary>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_GetAllVouchers", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            conn.Open();
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Vouchers.Add(new VoucherSummary
                {
                    VoucherId = (int)reader["VoucherId"],
                    VoucherDate = (DateTime)reader["VoucherDate"],
                    ReferenceNo = reader["ReferenceNo"].ToString(),
                    VoucherType = reader["VoucherType"].ToString(),
                    TotalDebit = (decimal)reader["TotalDebit"],
                    TotalCredit = (decimal)reader["TotalCredit"]
                });
            }
        }
    }

}
