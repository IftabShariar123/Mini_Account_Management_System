using AccountManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.Voucher
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public List<VoucherSummary> Vouchers { get; set; } = new();
        public bool CanViewList { get; private set; }
        public bool CanCreate { get; private set; }
        public bool CanViewDetails { get; private set; }
        public bool CanEdit { get; private set; }
        public bool CanDelete { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await CheckPermissions();

            if (!CanViewList)
            {
                return Forbid();
            }

            await LoadVouchers();
            return Page();
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
                    CanViewList = CanViewList || Convert.ToBoolean(reader["CanViewList"]);
                    CanCreate = CanCreate || Convert.ToBoolean(reader["CanCreate"]);
                    CanViewDetails = CanViewDetails || Convert.ToBoolean(reader["CanViewDetails"]);
                    CanEdit = CanEdit || Convert.ToBoolean(reader["CanUpdate"]);
                    CanDelete = CanDelete || Convert.ToBoolean(reader["CanDelete"]);
                }
            }
        }

        private async Task LoadVouchers()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_GetAllVouchers", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Vouchers.Add(new VoucherSummary
                {
                    VoucherId = (int)reader["VoucherId"],
                    VoucherDate = (DateTime)reader["VoucherDate"],
                    ReferenceNo = reader["ReferenceNo"]?.ToString(),
                    VoucherType = reader["VoucherType"].ToString(),
                    TotalDebit = (decimal)reader["TotalDebit"],
                    TotalCredit = (decimal)reader["TotalCredit"],
                });
            }
        }
    }
}
