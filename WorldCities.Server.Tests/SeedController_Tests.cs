using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading.Tasks;
using WorldCities.Server.Controllers;
using WorldCities.Server.Data;
using WorldCities.Server.Data.Models;
using Xunit;

namespace WorldCities.Server.Tests;

public class SeedController_Tests
{
    /// <summary>
    /// Test the CreateDefaultUsers() method.
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task CreateDefaultUsers()
    {
        // Arrange
        // Create the option instances required by the ApplicationDbContext.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("WorldCities").Options;

        // Create an IWebHost environment mock instance.
        var mockEnv = Mock.Of<IWebHostEnvironment>();

        // Create an IConfiguration mock instance.
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "DefaultPasswords:RegisteredUser")]).Returns("M0ckP$$word");
        mockConfiguration.SetupGet(x => x[It.Is<string>(s => s == "DefaultPasswords:Administrator")]).Returns("M0ckP$$word");

        // Create an ApplicationDbContext instance using the in-memory DB.
        using var context = new ApplicationDbContext(options);

        // Create a RoleManager instance.
        var roleManager = IdentityHelper.GetRoleManager(new RoleStore<IdentityRole>(context));

        // Create a UserManager instance.
        var userManager = IdentityHelper.GetUserManager(new UserStore<ApplicationUser>(context));

        // Create a SeedController instance.
        var controller = new SeedController(context, mockEnv, mockConfiguration.Object, roleManager, userManager);

        // Define the variables for the users we want to test.
        ApplicationUser user_Admin = null!;
        ApplicationUser user_User = null!;
        ApplicationUser user_NotExisting = null!;

        // Act
        // Execute the SeedController's CreateDefaultUsers() method to create the
        // default users (and roles).
        await controller.CreateDefaultUsers();

        // Retrieve the users.
        user_Admin = await userManager.FindByEmailAsync("admin@email.com");
        user_User = await userManager.FindByEmailAsync("user@email.com");
        user_NotExisting = await userManager.FindByEmailAsync("notexisting@email.com");

        // Assert
        Assert.NotNull(user_Admin);
        Assert.NotNull(user_User);  
        Assert.Null(user_NotExisting);
    }
}
