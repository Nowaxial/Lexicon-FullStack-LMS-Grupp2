
using Domain.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LMS.Blazor.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ApplicationUser>().ToTable("ApplicationUser");
    }

    public static implicit operator ApplicationDbContext(LMS.Infractructure.Data.ApplicationDbContext v)
    {
        throw new NotImplementedException();
    }
}
