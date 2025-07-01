using AccountManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.ChartOfAccounts
{
    [Authorize]
    public class DeleteModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;

        public DeleteModel(IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        [BindProperty]
        public ChartOfAccount Account { get; set; }

        public bool CanDelete { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            await CheckPermissions();

            if (!CanDelete)
            {
                return Forbid();
            }

            return await LoadAccount(id) ? Page() : NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await CheckPermissions();

            if (!CanDelete)
            {
                return Forbid();
            }

            await DeleteAccount();
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
                cmd.Parameters.AddWithValue("@ModuleName", "ChartOfAccounts");

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    CanDelete = Convert.ToBoolean(reader["CanDelete"]);
                    if (CanDelete) break;
                }
            }
        }

        private async Task<bool> LoadAccount(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT * FROM ChartOfAccounts WHERE AccountId = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Account = new ChartOfAccount
                {
                    AccountId = (int)reader["AccountId"],
                    AccountName = reader["AccountName"].ToString(),
                    ParentAccountId = reader["ParentAccountId"] == DBNull.Value ? null : (int?)reader["ParentAccountId"],
                    IsActive = (bool)reader["IsActive"],
                    Remarks = reader["Remarks"].ToString()
                };
                return true;
            }
            return false;
        }

        private async Task DeleteAccount()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "DELETE");
            cmd.Parameters.AddWithValue("@AccountId", Account.AccountId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}