using AccountManagement.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Data
{
    public class ChartOfAccountService
    {
        private readonly IConfiguration _config;

        public ChartOfAccountService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<ChartOfAccount>> GetAllAsync()
        {
            var accounts = new List<ChartOfAccount>();
            using SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using SqlCommand cmd = new SqlCommand("SELECT * FROM vw_ChartOfAccountTree", conn);
            await conn.OpenAsync();
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(new ChartOfAccount
                {
                    AccountId = (int)reader["AccountId"],
                    AccountName = reader["AccountName"].ToString(),
                    ParentAccountId = reader["ParentAccountId"] as int?,
                    Remarks = reader["HierarchyPath"].ToString()
                });
            }
            return accounts;
        }

        public async Task CreateAsync(ChartOfAccount account)
        {
            using SqlConnection conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            using SqlCommand cmd = new SqlCommand("sp_ManageChartOfAccounts", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Action", "CREATE");
            cmd.Parameters.AddWithValue("@AccountName", account.AccountName);
            cmd.Parameters.AddWithValue("@ParentAccountId", (object?)account.ParentAccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@IsActive", account.IsActive);
            cmd.Parameters.AddWithValue("@Remarks", account.Remarks ?? "");
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

    }
}