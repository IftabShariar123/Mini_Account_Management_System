using AccountManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.Voucher
{
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _config;
        public CreateModel(IConfiguration config) => _config = config;

        [BindProperty]
        public VoucherEntryVM Voucher { get; set; }

        public List<SelectListItem> Accounts { get; set; }

        public async Task OnGetAsync()
        {
            Accounts = new List<SelectListItem>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            var cmd = new SqlCommand("SELECT AccountId, AccountName FROM ChartOfAccounts", conn);
            conn.Open();
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Accounts.Add(new SelectListItem
                {
                    Value = reader["AccountId"].ToString(),
                    Text = reader["AccountName"].ToString()
                });
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid || Voucher.LineItems.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid data or no line items.");
                await OnGetAsync(); 
                return Page();
            }

            var table = new DataTable();
            table.Columns.Add("AccountId", typeof(int));
            table.Columns.Add("DebitAmount", typeof(decimal));
            table.Columns.Add("CreditAmount", typeof(decimal));

           
            decimal totalDebit = 0;
            decimal totalCredit = 0;

            foreach (var item in Voucher.LineItems)
            {
                if (item.DebitAmount > 0 || item.CreditAmount > 0)
                {
                    totalDebit += item.DebitAmount;
                    totalCredit += item.CreditAmount;

                    table.Rows.Add(item.AccountId, item.DebitAmount, item.CreditAmount);
                }
            }

            if (totalDebit != totalCredit)
            {
                ModelState.AddModelError(string.Empty, "Total Debit and Credit must be equal.");
                await OnGetAsync(); 
                return Page();
            }


            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_SaveVoucher", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@VoucherDate", Voucher.VoucherDate);
            cmd.Parameters.AddWithValue("@ReferenceNo", Voucher.ReferenceNo ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@VoucherType", Voucher.VoucherType);

            var tvp = cmd.Parameters.AddWithValue("@Entries", table);
            tvp.SqlDbType = SqlDbType.Structured;
            tvp.TypeName = "VoucherEntryType";

            conn.Open();
            await cmd.ExecuteNonQueryAsync();

            TempData["Success"] = "Voucher saved successfully.";
            return RedirectToPage("Index");
        }

    }
}
