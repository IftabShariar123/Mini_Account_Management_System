using AccountManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.Voucher
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;

        public CreateModel(IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        [BindProperty]
        public VoucherEntryVM Voucher { get; set; }

        public List<SelectListItem> Accounts { get; set; }
        public bool CanCreate { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await CheckPermissions();

            if (!CanCreate)
            {
                return Forbid();
            }

            await LoadAccounts();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await CheckPermissions();

            if (!CanCreate)
            {
                return Forbid();
            }

            if (!ModelState.IsValid || Voucher.LineItems?.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid data or no line items.");
                await LoadAccounts();
                return Page();
            }

            if (!ValidateVoucher())
            {
                await LoadAccounts();
                return Page();
            }

            await SaveVoucher();

            TempData["Success"] = "Voucher saved successfully.";
            return RedirectToPage("Index");
        }

        private async Task CheckPermissions()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            foreach (var role in roles)
            {
                using var cmd = new SqlCommand("sp_HasAccess", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@RoleName", role);
                cmd.Parameters.AddWithValue("@ModuleName", "Voucher");

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    CanCreate = Convert.ToBoolean(reader["CanCreate"]);
                    if (CanCreate) break;
                }
            }
        }

        private async Task LoadAccounts()
        {
            Accounts = new List<SelectListItem>();
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT AccountId, AccountName FROM ChartOfAccounts WHERE IsActive = 1", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Accounts.Add(new SelectListItem
                {
                    Value = reader["AccountId"].ToString(),
                    Text = reader["AccountName"].ToString()
                });
            }
        }

        private bool ValidateVoucher()
        {
            decimal totalDebit = 0;
            decimal totalCredit = 0;

            foreach (var item in Voucher.LineItems)
            {
                if (item.DebitAmount > 0 && item.CreditAmount > 0)
                {
                    ModelState.AddModelError(string.Empty, "A line item cannot have both debit and credit amounts.");
                    return false;
                }

                totalDebit += item.DebitAmount;
                totalCredit += item.CreditAmount;
            }

            if (totalDebit != totalCredit)
            {
                ModelState.AddModelError(string.Empty, "Total Debit and Credit must be equal.");
                return false;
            }

            return true;
        }

        private async Task SaveVoucher()
        {
            var table = new DataTable();
            table.Columns.Add("AccountId", typeof(int));
            table.Columns.Add("DebitAmount", typeof(decimal));
            table.Columns.Add("CreditAmount", typeof(decimal));

            foreach (var item in Voucher.LineItems)
            {
                if (item.DebitAmount > 0 || item.CreditAmount > 0)
                {
                    table.Rows.Add(item.AccountId, item.DebitAmount, item.CreditAmount);
                }
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

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}