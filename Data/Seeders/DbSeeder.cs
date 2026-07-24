using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OPA_Pay.Helpers;
using OPA_Pay.Models;

namespace OPA_Pay.Data.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            string[] roles = { "Admin", "Agent", "Client" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            const string adminEmail = "admin@opa.com";
            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                var user = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Admin",
                    RoleType = "Admin"
                };

                var result = await userManager.CreateAsync(user, "Admin123!");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        public static async Task SeedDemoAgentsAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            if (await context.AgentProfiles.AnyAsync(a => a.IsApproved))
                return;

            var demoAgents = new[]
            {
                new { Email = "agent.beirut@opa.com", Name = "Beirut Central Office", City = "Beirut", Lat = 33.8938, Lng = 35.5018 },
                new { Email = "agent.tripoli@opa.com", Name = "Tripoli Branch", City = "Tripoli", Lat = 34.4367, Lng = 35.8497 },
                new { Email = "agent.saida@opa.com", Name = "Saida Express", City = "Saida", Lat = 33.5631, Lng = 35.3688 }
            };

            foreach (var demo in demoAgents)
            {
                var user = await userManager.FindByEmailAsync(demo.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = demo.Email,
                        Email = demo.Email,
                        FullName = demo.Name,
                        RoleType = "Agent"
                    };

                    var result = await userManager.CreateAsync(user, "Agent123!");
                    if (!result.Succeeded)
                        continue;

                    await userManager.AddToRoleAsync(user, "Agent");
                }

                if (!await context.AgentProfiles.AnyAsync(a => a.UserId == user.Id))
                {
                    await context.AgentProfiles.AddAsync(new Agent
                    {
                        UserId = user.Id,
                        OfficeName = demo.Name,
                        City = demo.City,
                        Latitude = demo.Lat,
                        Longitude = demo.Lng,
                        OpeningTime = new TimeSpan(8, 0, 0),
                        ClosingTime = new TimeSpan(20, 0, 0),
                        IsOpen = true,
                        IsApproved = true
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
