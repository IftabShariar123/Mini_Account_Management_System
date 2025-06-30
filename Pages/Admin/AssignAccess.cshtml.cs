using AccountManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AccountManagement.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class AssignAccessModel : PageModel
    {
        private readonly IConfiguration _config;
        public AssignAccessModel(IConfiguration config) => _config = config;

        [BindProperty]
        public string SelectedRole { get; set; }

        [BindProperty]
        public List<ModulePermission> SelectedModules { get; set; } = new();

        public List<SelectListItem> Roles { get; set; }

        public List<string> AllModules => AppModules.Modules;
                

        public async Task OnGetAsync(string? role = null)
        {
            Roles = new List<SelectListItem>
    {
        new("Admin", "Admin"),
        new("Accountant", "Accountant"),
        new("Viewer", "Viewer")
    };

            SelectedRole = role;
            SelectedModules = new();

            foreach (var moduleName in AllModules)
            {
                var permission = new ModulePermission { Name = moduleName };

                if (!string.IsNullOrEmpty(role))
                {
                    using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                    await conn.OpenAsync();

                    using var cmd = new SqlCommand("sp_HasAccess", conn)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.Parameters.AddWithValue("@RoleName", role);
                    cmd.Parameters.AddWithValue("@ModuleName", moduleName);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        permission.CanCreate = reader.GetBoolean(reader.GetOrdinal("CanCreate"));
                        permission.CanUpdate = reader.GetBoolean(reader.GetOrdinal("CanUpdate"));
                        permission.CanDelete = reader.GetBoolean(reader.GetOrdinal("CanDelete"));
                        permission.CanViewList = reader.GetBoolean(reader.GetOrdinal("CanViewList"));
                        permission.CanViewDetails = reader.GetBoolean(reader.GetOrdinal("CanViewDetails"));
                    }
                }

                SelectedModules.Add(permission);
            }
        }


        public async Task<IActionResult> OnPostAsync()
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            foreach (var module in SelectedModules)
            {
                var cmd = new SqlCommand("sp_AssignModuleAccess", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@RoleName", SelectedRole);
                cmd.Parameters.AddWithValue("@ModuleName", module.Name);
                cmd.Parameters.AddWithValue("@CanCreate", module.CanCreate);
                cmd.Parameters.AddWithValue("@CanUpdate", module.CanUpdate);
                cmd.Parameters.AddWithValue("@CanDelete", module.CanDelete);
                cmd.Parameters.AddWithValue("@CanViewList", module.CanViewList);
                cmd.Parameters.AddWithValue("@CanViewDetails", module.CanViewDetails);

                await cmd.ExecuteNonQueryAsync();
            }

            TempData["Success"] = "Access rights assigned successfully.";
            return RedirectToPage();
        }
    }

    public class ModulePermission
    {
        public string Name { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
        public bool CanViewList { get; set; }
        public bool CanViewDetails { get; set; }
    }
}