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
    public class DetailsModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;

        public DetailsModel(IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public VoucherDetailsVM Voucher { get; set; } = new();
        public bool CanViewDetails { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            await CheckPermissions();

            if (!CanViewDetails)
            {
                return Forbid();
            }

            await LoadVoucherDetails(id);

            if (Voucher.VoucherId == 0) 
            {
                return NotFound();
            }

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
                    CanViewDetails = Convert.ToBoolean(reader["CanViewDetails"]);
                    if (CanViewDetails) break;
                }
            }
        }

        private async Task LoadVoucherDetails(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_GetVoucherDetails", conn)
            {
                CommandType = CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@VoucherId", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                if (Voucher.VoucherId == 0) 
                {
                    Voucher.VoucherId = (int)reader["VoucherId"];
                    Voucher.VoucherDate = (DateTime)reader["VoucherDate"];
                    Voucher.ReferenceNo = reader["ReferenceNo"]?.ToString();
                    Voucher.VoucherType = reader["VoucherType"].ToString();
                }

                Voucher.LineItems.Add(new VoucherLineDetail
                {
                    AccountName = reader["AccountName"].ToString(),
                    DebitAmount = (decimal)reader["DebitAmount"],
                    CreditAmount = (decimal)reader["CreditAmount"],

                });
            }
        }
    }
}