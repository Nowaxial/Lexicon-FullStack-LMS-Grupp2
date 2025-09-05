using Bogus;
using LMS.Infractructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Services;

//Add in secret.json
//{
//   "password" :  "YourSecretPasswordHere"
//}
public class DataSeedHostingService : IHostedService
{
    private readonly IServiceProvider serviceProvider;
    private readonly IConfiguration configuration;
    private readonly ILogger<DataSeedHostingService> logger;
    private UserManager<ApplicationUser> userManager = null!;
    private RoleManager<IdentityRole> roleManager = null!;
    private const string TeacherRole = "Teacher";
    private const string StudentRole = "Student";
    private ApplicationDbContext dbContext = null!;
    public DataSeedHostingService(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<DataSeedHostingService> logger)
    {
        this.serviceProvider = serviceProvider;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        if (!env.IsDevelopment()) return;

        dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
         if (await dbContext.Users.AnyAsync(cancellationToken)) return;

        userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        ArgumentNullException.ThrowIfNull(roleManager, nameof(roleManager));
        ArgumentNullException.ThrowIfNull(userManager, nameof(userManager));

        try
        {
            var courses = new[]
{
                new Course { Name = "C# Fundamentals", Description = "Introduktion till C#", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-7)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(60)) },
                new Course { Name = "JavaScript Basics", Description = "Grundläggande JavaScript", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-14)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(45)) },
                new Course { Name = "React Development", Description = "Modern React utveckling", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-21)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) },
                new Course { Name = "Python Basics", Description = "Grundläggande Python", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-28)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(20)) },
                new Course { Name = "ASP.NET Core", Description = "Webbutveckling med ASP.NET Core", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-35)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(15)) }
            };

            dbContext.Courses.AddRange(courses);
            await AddRolesAsync([TeacherRole, StudentRole]);
            await AddDemoUsersAsync();
            await AddUsersAsync(20);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Seed complete");
        }
        catch (Exception ex)
        {
            logger.LogError($"Data seed fail with error: {ex.Message}");
            throw;
        }
    }

    private async Task AddRolesAsync(string[] rolenames)
    {
        foreach (string rolename in rolenames)
        {
            if (await roleManager.RoleExistsAsync(rolename)) continue;
            var role = new IdentityRole { Name = rolename };
            var res = await roleManager.CreateAsync(role);

            if (!res.Succeeded) throw new Exception(string.Join("\n", res.Errors));
        }
    }
    private async Task AddDemoUsersAsync()
    {
        var teacher = new ApplicationUser
        {
            UserName = "teacher@test.com",
            Email = "teacher@test.com"
        };
        
        var student = new ApplicationUser
        {
            UserName = "student@test.com",
            Email = "student@test.com"
        };

        await AddUserToDb([teacher, student]);

        var teacherRoleResult = await userManager.AddToRoleAsync(teacher, TeacherRole);
        if (!teacherRoleResult.Succeeded) throw new Exception(string.Join("\n", teacherRoleResult.Errors));

        var studentRoleResult = await userManager.AddToRoleAsync(student, StudentRole);
        if (!studentRoleResult.Succeeded) throw new Exception(string.Join("\n", studentRoleResult.Errors));

        await AddCourseUserRelationship(new[] { teacher, student });
    }

    private async Task AddUsersAsync(int nrOfUsers)
    {
        var faker = new Faker<ApplicationUser>("sv")
            .Rules((f, e) =>
            {
                e.Id = Guid.NewGuid().ToString();

                e.Email = f.Internet.Email();
                e.NormalizedEmail = e.Email.ToUpper();
                e.UserName = e.Email.Split('@')[0]; // use part before @
                e.NormalizedUserName = e.UserName.ToUpper();

                var hasher = new PasswordHasher<ApplicationUser>();
                e.PasswordHash = hasher.HashPassword(e, "Password123!");

                e.EmailConfirmed = f.Random.Bool();
                e.PhoneNumber = f.Phone.PhoneNumber();
                e.PhoneNumberConfirmed = f.Random.Bool();
                e.TwoFactorEnabled = f.Random.Bool();
                e.LockoutEnabled = true;
                e.AccessFailedCount = f.Random.Int(0, 5);

                e.RefreshToken = f.Random.Guid().ToString();
                e.RefreshTokenExpireTime = DateTime.UtcNow.AddDays(f.Random.Int(1, 30));
            });

        var users = faker.Generate(nrOfUsers);
        await AddUserToDb(users);

        // Assign random role ("Teacher" or "Student")
        var roles = new[] { "Teacher", "Student" };
        var rnd = new Random();

        foreach (var user in users)
        {
            var role = roles[rnd.Next(roles.Length)];
            var result = await userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Could not assign role {role} to {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                );
            }
        }

        await AddCourseUserRelationship(users);
    }

    private async Task AddCourseUserRelationship(IEnumerable<ApplicationUser> users)
    {
        //Only 1 course per user. 
        var courses = await dbContext.Courses.ToListAsync();
        var rand = new Faker();

        if (courses.Any())
        {
            var courseUsers = new List<CourseUser>();

            // Pick 5 random users to NOT assign to any course
            var excludedUsers = users.OrderBy(u => Guid.NewGuid()).Take(5).ToHashSet();

            foreach (var user in users)
            {
                // Skip the excluded users
                if (excludedUsers.Contains(user))
                    continue;

                var course = rand.PickRandom(courses);

                courseUsers.Add(new CourseUser
                {
                    UserId = user.Id,
                    CourseId = course.Id,
                    IsTeacher = false
                });
            }

            if (courseUsers.Any())
            {
                dbContext.CourseUsers.AddRange(courseUsers);
                await dbContext.SaveChangesAsync();
            }
        }
    }
    private async Task AddUserToDb(IEnumerable<ApplicationUser> users)
    {
        var passWord = configuration["password"];
        ArgumentNullException.ThrowIfNull(passWord, nameof(passWord));

        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user, passWord);
            if (!result.Succeeded) throw new Exception(string.Join("\n", result.Errors));
        }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
