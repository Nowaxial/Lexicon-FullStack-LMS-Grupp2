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
        //if (await dbContext.Users.AnyAsync(cancellationToken)) return;

        userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        ArgumentNullException.ThrowIfNull(roleManager, nameof(roleManager));
        ArgumentNullException.ThrowIfNull(userManager, nameof(userManager));

        try
        {
            // --- Course seeding (only if missing) ---
            if (!await dbContext.Courses.AnyAsync(cancellationToken))
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
            }

            // --- Module seeding (per course) ---
            await EnsureModulesSeededAsync(dbContext, cancellationToken);

            // --- Activity seeding (per module) ---
            await EnsureActivitiesSeededAsync(dbContext, cancellationToken);

            logger.LogInformation("Seed complete");
        }
        catch (Exception ex)
        {
            logger.LogError($"Data seed fail with error: {ex.Message}");
            throw;
        }
    }

    private static async Task EnsureActivitiesSeededAsync(ApplicationDbContext context, CancellationToken ct)
    {
        // Hämta moduler som saknar aktiviteter
        var modules = await context.Modules
            .Where(m => !context.Activities.Any(a => a.ModuleId == m.Id))
            .Select(m => new { m.Id, m.Starts, m.Ends, m.Name })
            .ToListAsync(ct);

        if (!modules.Any()) return;

        var newActivities = new List<ProjActivity>();
        var activityTypes = new[] { "E-learning", "Föreläsning", "Övning", "Inlämningsuppgift", "Övrigt" };
        var activityTitles = new[]
        {
            "Introduktion", "Grunderna", "Praktisk övning", "Fördjupade studier",
            "Workshop", "Labbtillfälle", "Projektarbete", "Repetition",
            "Kunskapskontroll", "Hemuppgift", "Grupparbete", "Redovisning"
        };

        var random = new Random();

        foreach (var module in modules)
        {
            var moduleStart = module.Starts.ToDateTime(TimeOnly.MinValue);
            var moduleEnd = module.Ends.ToDateTime(TimeOnly.MaxValue);

            // Create 3-5 activities per modul
            int activityCount = random.Next(3, 6);

            for (int i = 0; i < activityCount; i++)
            {
                // Random start and duration within module
                var totalHours = (moduleEnd - moduleStart).TotalHours;
                var startOffset = random.NextDouble() * totalHours * 0.8;
                var activityStart = moduleStart.AddHours(startOffset);

                // 1-5 hours duration for each activity
                var duration = random.Next(1, 5);
                var activityEnd = activityStart.AddHours(duration);

                // Don't go past module end
                if (activityEnd > moduleEnd)
                    activityEnd = moduleEnd;

                newActivities.Add(new ProjActivity
                {
                    ModuleId = module.Id,
                    Title = $"{activityTitles[random.Next(activityTitles.Length)]} {i + 1}",
                    Description = $"Aktivitet för {module.Name} - Session {i + 1}",
                    Type = activityTypes[random.Next(activityTypes.Length)],
                    Starts = activityStart,
                    Ends = activityEnd
                });
            }
        }

        if (newActivities.Count > 0)
        {
            context.Activities.AddRange(newActivities);
            await context.SaveChangesAsync(ct);
        }
    }


    private static async Task EnsureModulesSeededAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var faker = new Faker("sv");

        var courses = await context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.Starts, c.Ends, c.Name })
            .ToListAsync(ct);

        var newModules = new List<Module>();

        foreach (var c in courses)
        {
            // Skip if this course already has modules
            bool hasModules = await context.Modules.AnyAsync(m => m.CourseId == c.Id, ct);
            if (hasModules) continue;

            int segments = new Faker().Random.Int(3, 5);
            int totalDays = Math.Max((c.Ends.DayNumber - c.Starts.DayNumber + 1), 7);

            int baseLen = Math.Max(3, totalDays / segments);
            int remainder = totalDays - baseLen * segments;

            var currentStart = c.Starts;

            for (int i = 1; i <= segments; i++)
            {
                int len = baseLen + (remainder-- > 0 ? 1 : 0);
                var currentEnd = currentStart.AddDays(len - 1);

                // Clamp to course end without relying on '>' operator
                if (currentEnd.DayNumber > c.Ends.DayNumber)
                    currentEnd = c.Ends;

                newModules.Add(new Module
                {
                    CourseId = c.Id,
                    Name = $"Modul {i}: {CapFirst(faker.Hacker.Verb())} {faker.Hacker.Noun()}",
                    Description = faker.Lorem.Sentences(2),
                    Starts = currentStart,
                    Ends = currentEnd
                });

                if (currentEnd.DayNumber >= c.Ends.DayNumber) break;
                currentStart = currentEnd.AddDays(1);
            }
        }

        if (newModules.Count > 0)
        {
            context.Modules.AddRange(newModules);
            await context.SaveChangesAsync(ct);
        }
    }

    private static string CapFirst(string s)
        => string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
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

        await AddCourseUserRelationship(users,5);
    }

    private async Task AddCourseUserRelationship(IEnumerable<ApplicationUser> users, int usersNoCourse = 0)
    {
        //Only 1 course per user. 
        var courses = await dbContext.Courses.ToListAsync();
        var rand = new Faker();

        if (courses.Any())
        {
            var courseUsers = new List<CourseUser>();

            // Pick 5 random users to NOT assign to any course
            var excludedUsers = users.OrderBy(u => Guid.NewGuid()).Take(usersNoCourse).ToHashSet();

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
