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
        public ChartOfAccount Account { get; set; }

        public List<ChartOfAccount> ParentAccounts { get; set; } = new();
        public bool CanCreate { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await CheckPermissions();

            if (!CanCreate)
            {
                return Forbid();
            }

            await LoadParentAccounts();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await CheckPermissions();

            if (!CanCreate)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await LoadParentAccounts();
                return Page();
            }

            await CreateAccount();
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
                    CanCreate = Convert.ToBoolean(reader["CanCreate"]);
                    if (CanCreate) break;
                }
            }
        }

        private async Task LoadParentAccounts()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT AccountId, AccountName FROM ChartOfAccounts", conn);

            await conn.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                ParentAccounts.Add(new ChartOfAccount
                {
                    AccountId = (int)reader["AccountId"],
                    AccountName = reader["AccountName"].ToString()
                });
            }
        }

        private async Task CreateAccount()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "CREATE");
            cmd.Parameters.AddWithValue("@AccountName", Account.AccountName);
            cmd.Parameters.AddWithValue("@ParentAccountId", (object?)Account.ParentAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", Account.IsActive);
            cmd.Parameters.AddWithValue("@Remarks", Account.Remarks ?? "");

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}