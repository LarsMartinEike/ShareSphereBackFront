using Microsoft.AspNetCore.Identity;
using ShareSphere.Api.Models;

namespace ShareSphere.Api.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAdminUser(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 1) Rollen erstellen, falls sie noch nicht existieren
            string[] roleNames = { "admin", "user"};
            
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // 2) Admin-Benutzer erstellen, falls er noch nicht existiert
            const string adminUserName = "admin";
            const string adminEmail = "admin@sharesphere.com";
            const string adminPassword = "Admin123!"; // ⚠️ Ändern Sie dies in Production!

            var adminUser = await userManager.FindByNameAsync(adminUserName);
            
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    DisplayName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "admin");
                    Console.WriteLine($"✓ Admin user '{adminUserName}' created successfully");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"ℹ Admin user '{adminUserName}' already exists");
            }
        }
    }
}