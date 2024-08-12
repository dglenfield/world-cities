using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorldCities.Server.Data.Models;

namespace WorldCities.Server.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<City> Cities => Set<City>();
    public DbSet<Country> Countries => Set<Country>();

    public ApplicationDbContext() : base()
    {
    }

    public ApplicationDbContext(DbContextOptions options) : base(options) 
    { 
    }
}
