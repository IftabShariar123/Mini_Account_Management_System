using AccountManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AssignAccessModel(IConfiguration config, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _config = config;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        [BindProperty]
        public string NewRole { get; set; }

        [BindProperty]
        public string SelectedRole { get; set; }

        [BindProperty]
        public List<ModulePermission> SelectedModules { get; set; } = new();

        public List<SelectListItem> Roles { get; set; }
        public List<string> AllModules => AppModules.Modules;

        public List<IdentityUser> Users { get; set; }
        public Dictionary<string, IList<string>> UserRoles { get; set; } = new();
        public List<string> AllRoles { get; set; }

        public async Task OnGetAsync(string? role = null)
        {
            Roles = _roleManager.Roles.Select(r => new SelectListItem(r.Name, r.Name)).ToList();
            AllRoles = Roles.Select(r => r.Value).ToList();
            SelectedRole = role;

            Users = _userManager.Users.ToList();
            foreach (var user in Users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                UserRoles[user.Id] = roles;
            }

            SelectedModules = new();
            if (!string.IsNullOrEmpty(role))
            {
                foreach (var moduleName in AllModules)
                {
                    var permission = new ModulePermission { Name = moduleName };
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
                        permission.CanCreate = Convert.ToBoolean(reader["CanCreate"]);
                        permission.CanUpdate = Convert.ToBoolean(reader["CanUpdate"]);
                        permission.CanDelete = Convert.ToBoolean(reader["CanDelete"]);
                        permission.CanViewList = Convert.ToBoolean(reader["CanViewList"]);
                        permission.CanViewDetails = Convert.ToBoolean(reader["CanViewDetails"]);
                    }

                    SelectedModules.Add(permission);
                }
            }
        }


        public async Task<IActionResult> OnPostAddRoleAsync()
        {
            if (!string.IsNullOrWhiteSpace(NewRole) && !await _roleManager.RoleExistsAsync(NewRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(NewRole));
                TempData["Success"] = $"Role '{NewRole}' created.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAssignRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = $"User assigned to role '{role}'.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveRoleAsync(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["Success"] = $"Role '{role}' removed from user.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Roles = _roleManager.Roles.Select(r => new SelectListItem(r.Name, r.Name)).ToList();

            if (string.IsNullOrEmpty(SelectedRole))
            {
                ModelState.AddModelError("", "Please select a role.");
                return Page();
            }

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

            TempData["Success"] = "Module permissions saved.";
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