using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Trade.Identity.Service.Entities;
using Trade.Identity.Service.Settings;

namespace Trade.Identity.Service.HostedServices
{
    public class IdentitySeedHostedService : IHostedService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IdentitySettings _settings;

        public IdentitySeedHostedService(
            IServiceScopeFactory serviceScopeFactory,
            IOptions<IdentitySettings> identityOptions)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _settings = identityOptions.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();    
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await CreateRoleIfNotExistAsync(Roles.Admin, roleManager);
            await CreateRoleIfNotExistAsync(Roles.Trader, roleManager);

            var adminUser = await userManager.FindByEmailAsync(_settings.AdminUserEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = _settings.AdminUserEmail,
                    Email = _settings.AdminUserEmail
                };

                await userManager.CreateAsync(adminUser, _settings.AdminUserPassword);
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static async Task CreateRoleIfNotExistAsync(
            string role,
            RoleManager<ApplicationRole> roleManager)
        {
            var roleExist = await roleManager.RoleExistsAsync(role);

            if (!roleExist) 
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }
}
