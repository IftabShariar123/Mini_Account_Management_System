using AccountManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.ChartOfAccounts
{

    public class DeleteModel : PageModel
    {
        private readonly IConfiguration _config;

        public DeleteModel(IConfiguration config)
        {
            _config = config;
        }

        [BindProperty]
        public ChartOfAccount Account { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            using SqlConnection conn = new(_config.GetConnectionString("DefaultConnection"));
            using SqlCommand cmd = new("SELECT * FROM ChartOfAccounts WHERE AccountId = @id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            await conn.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
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
                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            using SqlConnection conn = new(_config.GetConnectionString("DefaultConnection"));
            using SqlCommand cmd = new("sp_ManageChartOfAccounts", conn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Action", "DELETE");
            cmd.Parameters.AddWithValue("@AccountId", Account.AccountId);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return RedirectToPage("Index");
        }
    }
}
