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
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        public List<ChartOfAccount> Accounts { get; set; } = new();
        public bool CanViewList { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await CheckPermissions();

            if (!CanViewList)
            {
                return Forbid();
            }

            await LoadAccounts();
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
                cmd.Parameters.AddWithValue("@ModuleName", "ChartOfAccounts");

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    CanViewList = Convert.ToBoolean(reader["CanViewList"]);
                    if (CanViewList) break;
                }
            }
        }

        private async Task LoadAccounts()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT * FROM vw_ChartOfAccountTree", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var account = new ChartOfAccount
                {
                    AccountId = Convert.ToInt32(reader["AccountId"]),
                    AccountName = reader["AccountName"].ToString(),
                    ParentAccountId = reader["ParentAccountId"] == DBNull.Value ? null : Convert.ToInt32(reader["ParentAccountId"]),
                    Remarks = reader["HierarchyPath"].ToString()
                };

                try
                {
                    account.IsActive = Convert.ToBoolean(reader["IsActive"]);
                }
                catch (IndexOutOfRangeException)
                {
                    account.IsActive = false;
                }

                Accounts.Add(account);
            }
        }
    }
}