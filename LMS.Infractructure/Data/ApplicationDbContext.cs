using Domain.Models.Entities;
using LMS.Infractructure.Data.Configurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Diagnostics;
using System.Reflection.Metadata;

namespace LMS.Infractructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<ProjDocument> ProjDocuments { get; set; } = null!;

        // DbSets
        public DbSet<Course> Courses { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<CourseUser> CourseUsers { get; set; }
        public DbSet<ProjActivity> Activities { get; set; }
        public DbSet<ProjDocument> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new ApplicationUserConfigurations());

            // -------------------------------
            // DateOnly converter
            // -------------------------------
            var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
                d => d.ToDateTime(TimeOnly.MinValue),
                d => DateOnly.FromDateTime(d));

            // -------------------------------
            // Course config
            // -------------------------------
            builder.Entity<Course>()
                .Property(c => c.Starts)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("date");

            builder.Entity<Course>()
                .Property(c => c.Ends)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("date");

            // -------------------------------
            // Module config
            // -------------------------------
            builder.Entity<Module>()
                .Property(m => m.Starts)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("date");

            builder.Entity<Module>()
                .Property(m => m.Ends)
                .HasConversion(dateOnlyConverter)
                .HasColumnType("date");

            builder.Entity<Module>()
                .HasOne(m => m.Course)
                .WithMany(c => c.Modules)
                .HasForeignKey(m => m.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------
            // Activity config
            // -------------------------------
            builder.Entity<ProjActivity>()
                .HasOne(a => a.Module)
                .WithMany(m => m.Activities)
                .HasForeignKey(a => a.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // -------------------------------
            // Document config
            // -------------------------------
            builder.Entity<ProjDocument>()
                .HasOne(d => d.Course)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjDocument>()
                .HasOne(d => d.Module)
                .WithMany(m => m.Documents)
                .HasForeignKey(d => d.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProjDocument>()
                .HasOne(d => d.Activity)
                .WithMany(a => a.Documents)
                .HasForeignKey(d => d.ActivityId)
                .OnDelete(DeleteBehavior.Restrict);

            // -------------------------------
            // CourseUser config
            // -------------------------------
            builder.Entity<CourseUser>()
                .HasKey(cu => new { cu.UserId, cu.CourseId });

            builder.Entity<CourseUser>()
                .HasOne(cu => cu.Course)
                .WithMany(c => c.CourseUsers)
                .HasForeignKey(cu => cu.CourseId);

            builder.Entity<CourseUser>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId);
        }
        
    }
}