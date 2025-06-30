using AccountManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.Voucher
{
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _config;
        public DetailsModel(IConfiguration config) => _config = config;

        public VoucherDetailsVM Voucher { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Voucher = new VoucherDetailsVM();

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var cmd = new SqlCommand("sp_GetVoucherDetails", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@VoucherId", id);

            conn.Open();
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (Voucher.VoucherId == 0)
                {
                    Voucher.VoucherId = (int)reader["VoucherId"];
                    Voucher.VoucherDate = (DateTime)reader["VoucherDate"];
                    Voucher.ReferenceNo = reader["ReferenceNo"].ToString();
                    Voucher.VoucherType = reader["VoucherType"].ToString();
                }

                Voucher.LineItems.Add(new VoucherLineDetail
                {
                    AccountName = reader["AccountName"].ToString(),
                    DebitAmount = (decimal)reader["DebitAmount"],
                    CreditAmount = (decimal)reader["CreditAmount"]
                });
            }

            return Page();
        }
    }
}
