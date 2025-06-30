using AccountManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AccountManagement.Pages.ChartOfAccounts
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;

        public IndexModel(IConfiguration config)
        {
            _config = config;
        }

        public List<ChartOfAccount> Accounts { get; set; } = new();

        public async Task OnGetAsync()
        {
            using SqlConnection conn = new(_config.GetConnectionString("DefaultConnection"));
            using SqlCommand cmd = new("SELECT * FROM vw_ChartOfAccountTree", conn);
            await conn.OpenAsync();

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Accounts.Add(new ChartOfAccount
                {
                    AccountId = Convert.ToInt32(reader["AccountId"]),
                    AccountName = reader["AccountName"].ToString(),
                    ParentAccountId = reader["ParentAccountId"] == DBNull.Value ? null : Convert.ToInt32(reader["ParentAccountId"]),
                    Remarks = reader["HierarchyPath"].ToString()
                });
            }
        }
    }
}