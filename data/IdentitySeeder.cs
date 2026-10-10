using Microsoft.AspNetCore.Identity;

namespace mini_store.Data
{
    public static class IdentitySeeder
    {
        // =========================================================
        // ROLES
        // =========================================================

        public static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager)
        {
            string[] roles =
            {
                "Admin",
                "Staff"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        // =========================================================
        // FIRST ADMIN (only when no admin exists yet)
        // Credentials come from configuration, never from code.
        // =========================================================

        public static async Task SeedAdminAsync(
            UserManager<IdentityUser> userManager,
            IConfiguration configuration,
            ILogger logger)
        {
            var email = configuration["Seed:AdminEmail"];
            var password = configuration["Seed:AdminPassword"];

            // Nothing configured: skip silently
            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            // An admin already exists: do nothing
            var admins = await userManager.GetUsersInRoleAsync("Admin");

            if (admins.Any())
            {
                return;
            }

            var user = await userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var created = await userManager.CreateAsync(user, password);

                if (!created.Succeeded)
                {
                    logger.LogError(
                        "Could not create the first admin: {Errors}",
                        string.Join(", ", created.Errors.Select(e => e.Description)));

                    return;
                }
            }

            var added = await userManager.AddToRoleAsync(user, "Admin");

            if (added.Succeeded)
            {
                logger.LogInformation("First admin account {Email} is ready.", email);
            }
            else
            {
                logger.LogError(
                    "Could not assign the Admin role: {Errors}",
                    string.Join(", ", added.Errors.Select(e => e.Description)));
            }
        }
    }
}