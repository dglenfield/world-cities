using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Security;
using WorldCities.Server.Data;
using WorldCities.Server.Data.Models;

namespace WorldCities.Server.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class SeedController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public SeedController(ApplicationDbContext context, IWebHostEnvironment env, 
        IConfiguration configuration, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _env = env;
        _configuration = configuration;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult> CreateDefaultUsers()
    {
        // Setup the default role names.
        string role_RegisteredUser = "RegisteredUser";
        string role_Administrator = "Administrator";

        // Create the default roles (if they don't exist yet).
        if (await _roleManager.FindByNameAsync(role_RegisteredUser) == null)
        {
            await _roleManager.CreateAsync(new IdentityRole(role_RegisteredUser));
        }
        if (await _roleManager.FindByNameAsync(role_Administrator) == null)
        {
            await _roleManager.CreateAsync(new IdentityRole(role_Administrator));
        }

        // Create a list to track the newly added users.
        var addedUserList = new List<ApplicationUser>();

        // Check if the admin user already exists.
        var email_Admin = "admin@email.com";
        if (await _userManager.FindByNameAsync(email_Admin) == null) 
        {
            // Create a new admin ApplicationUser account.
            var user_Admin = new ApplicationUser()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = email_Admin,
                Email = email_Admin
            };

            // Insert the admin user into the DB.
            await _userManager.CreateAsync(user_Admin, _configuration["DefaultPasswords:Administrator"]);

            // Assign the "RegisteredUser" and "Administrator" roles.
            await _userManager.AddToRoleAsync(user_Admin, role_RegisteredUser);
            await _userManager.AddToRoleAsync(user_Admin, role_Administrator);

            // Confirm the email and remove lockout.
            user_Admin.EmailConfirmed = true;
            user_Admin.LockoutEnabled = false;

            // Add the admin user to the added users list.
            addedUserList.Add(user_Admin);
        }

        // Check if the standard user already exists.
        var email_User = "user@email.com";
        if (await _userManager.FindByNameAsync(email_User) == null) 
        {
            // Create a new standard ApplicationUser account.
            var user_User = new ApplicationUser()
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = email_User,
                Email = email_User
            };

            // Insert the standard user into the DB.
            await _userManager.CreateAsync(user_User, _configuration["DefaultPasswords:RegisteredUser"]);

            // Assign the "RegisteredUser" role.
            await _userManager.AddToRoleAsync(user_User, role_RegisteredUser);

            // Confirm the email and remove lockout.
            user_User.EmailConfirmed = true;
            user_User.LockoutEnabled = false;

            // Add the standard user to the added users list.
            addedUserList.Add(user_User);
        }

        // If we added at least one user, persist the changes into the DB.
        if (addedUserList.Count > 0) 
        {
            await _context.SaveChangesAsync();
        }

        return new JsonResult(new
        {
            Count = addedUserList.Count,
            Users = addedUserList
        });
    }

    [HttpGet]
    public async Task<ActionResult> Import()
    {
        // Prevents non-development environments from running this method.
        if (!_env.IsDevelopment()) 
        {
            throw new SecurityException("Not allowed");
        }

        var path = Path.Combine(_env.ContentRootPath, "Data/Source/worldcities.xlsx");

        using var stream = System.IO.File.OpenRead(path);
        using var excelPackage = new ExcelPackage(stream);

        // Get the first worksheet.
        var worksheet = excelPackage.Workbook.Worksheets[0];

        // Define how many rows we want to process.
        int nEndRow = worksheet.Dimension.End.Row;

        // Initialize the record counters.
        int numberOfCountriesAdded = 0;
        int numberOfCitiesAdded = 0;

        // Create a lookup dictionary containing all the countries already existing
        // into the database (it will be empty on first run).
        var countriesByName = _context.Countries.AsNoTracking()
            .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        // Iterates through all rows, skipping the first one.
        for (int nRow = 2; nRow < nEndRow; nRow++) 
        {
            var row = worksheet.Cells[nRow, 1, nRow, worksheet.Dimension.End.Column];

            var countryName = row[nRow, 5].GetValue<string>();
            var iso2 = row[nRow, 6].GetValue<string>();
            var iso3 = row[nRow, 7].GetValue<string>();

            // Skip this country if it already exists in the database.
            if (countriesByName.ContainsKey(countryName)) 
            {
                continue;
            }

            // Create the Country entity and fill it with xlsx data.
            Country country = new() { Name = countryName, ISO2 = iso2, ISO3 = iso3 };

            // Add the new country to the DB context.
            await _context.Countries.AddAsync(country);

            // Store the country in our lookup to retrieve its Id later on.
            countriesByName.Add(countryName, country);

            // Increment the counter.
            numberOfCountriesAdded++;
        }

        // Save all the countries into the database.
        if (numberOfCountriesAdded > 0) 
        {
            await _context.SaveChangesAsync();
        }

        // Create a lookup dictionary containing all the cities already existing
        // into the database (it will be empty on first run).
        var cities = _context.Cities.AsNoTracking()
            .ToDictionary(x => (Name: x.Name, Lat: x.Lat, Lon: x.Lon, CountryId: x.CountryId));

        // Iterates through all rows, skipping the first one.
        for (int nRow = 2; nRow <= nEndRow; nRow++) 
        {
            var row = worksheet.Cells[nRow, 1, nRow, worksheet.Dimension.End.Column];

            var name = row[nRow, 1].GetValue<string>();
            var lat = row[nRow, 3].GetValue<decimal>();
            var lon = row[nRow, 4].GetValue<decimal>();
            var countryName = row[nRow, 5].GetValue<string>();

            // Retrieve country Id by countryName.
            var countryId = countriesByName[countryName].Id;

            // Skip this city if it already exists in the database.
            if (cities.ContainsKey((Name: name, Lat: lat, Lon: lon, CountryId: countryId))) 
            {
                continue;
            }

            // Create the City entity and fill it with xlsx data.
            City city = new() { Name = name, Lat = lat, Lon = lon, CountryId = countryId };

            // Add the new city to the DB context.
            _context.Cities.Add(city);

            // Increment the counter.
            numberOfCitiesAdded++;
        }

        // Save all the cities into the database.
        if (numberOfCitiesAdded > 0) 
        {
            await _context.SaveChangesAsync();
        }

        return new JsonResult(new { Cities = numberOfCitiesAdded, Countries = numberOfCountriesAdded });
    }
}
