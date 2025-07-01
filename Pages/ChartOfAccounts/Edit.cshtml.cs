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
    public class EditModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly UserManager<IdentityUser> _userManager;

        public EditModel(IConfiguration config, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _userManager = userManager;
        }

        [BindProperty]
        public ChartOfAccount Account { get; set; }

        public List<ChartOfAccount> ParentAccounts { get; set; } = new();
        public bool CanUpdate { get; private set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            await CheckPermissions();

            if (!CanUpdate)
            {
                return Forbid();
            }

            if (!await LoadAccount(id))
            {
                return NotFound();
            }

            await LoadParentAccounts(id);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await CheckPermissions();

            if (!CanUpdate)
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                await LoadParentAccounts(Account.AccountId);
                return Page();
            }

            await UpdateAccount();
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
                    CanUpdate = Convert.ToBoolean(reader["CanUpdate"]);
                    if (CanUpdate) break;
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

        private async Task LoadParentAccounts(int currentId)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("SELECT AccountId, AccountName FROM ChartOfAccounts WHERE AccountId != @id", conn);
            cmd.Parameters.AddWithValue("@id", currentId);

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

        private async Task UpdateAccount()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using var cmd = new SqlCommand("sp_ManageChartOfAccounts", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@Action", "UPDATE");
            cmd.Parameters.AddWithValue("@AccountId", Account.AccountId);
            cmd.Parameters.AddWithValue("@AccountName", Account.AccountName);
            cmd.Parameters.AddWithValue("@ParentAccountId", (object?)Account.ParentAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", Account.IsActive);
            cmd.Parameters.AddWithValue("@Remarks", Account.Remarks ?? "");

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}