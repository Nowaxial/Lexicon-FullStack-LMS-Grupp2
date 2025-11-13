using Bogus;
using LMS.Infractructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LMS.API.Services;


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
        //if (!env.IsDevelopment()) return;

        dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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
                    new Course { Name = "ASP.NET Core", Description = "Webbutveckling med ASP.NET Core", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-35)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(15)) },
                    new Course { Name = "Fullstack .NET", Description = "Komplett .NET utveckling med Blazor", Starts = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), Ends = DateOnly.FromDateTime(DateTime.Today.AddDays(90)) }
                };

                dbContext.Courses.AddRange(courses);
                await AddRolesAsync([TeacherRole, StudentRole]);
                await AddDemoUsersAsync();
                await AddUsersAsync(36);
                await dbContext.SaveChangesAsync();

                // Add course relationships for ALL users in database
                var allUsers = await dbContext.Users.ToListAsync(cancellationToken);
                await AddCourseUserRelationship(allUsers);
            }

            // Always show current distribution
            await ShowCurrentTeacherDistribution();

            // --- Module seeding with realistic data (per course) ---
            await EnsureModulesSeededWithRealisticDataAsync(dbContext, cancellationToken);

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

    private async Task ShowCurrentTeacherDistribution()
    {
        var courseUsers = await dbContext.CourseUsers
            .Where(cu => cu.IsTeacher)
            .Include(cu => cu.User)
            .ToListAsync();

        var teacherCourseCount = courseUsers
            .GroupBy(cu => cu.UserId)
            .Select(g => new { TeacherId = g.Key, CourseCount = g.Count(), Teacher = g.First().User })
            .ToList();

        Console.WriteLine("=== LÄRARE KURSFÖRDELNING ===");
        foreach (var tc in teacherCourseCount)
        {
            Console.WriteLine($"{tc.Teacher.FirstName} {tc.Teacher.LastName} {tc.Teacher.Email}: {tc.CourseCount} kurser");
        }

        var totalTeachers = await dbContext.Users
            .Where(u => dbContext.UserRoles.Any(ur => ur.UserId == u.Id &&
                dbContext.Roles.Any(r => r.Id == ur.RoleId && r.Name == TeacherRole)))
            .CountAsync();

        var totalCourses = await dbContext.Courses.CountAsync();
        Console.WriteLine($"Totalt {totalTeachers} lärare, {totalCourses} kurser");
    }

    private static async Task EnsureActivitiesSeededAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var modules = await context.Modules
            .Where(m => !context.Activities.Any(a => a.ModuleId == m.Id))
            .Include(m => m.Course)
            .Select(m => new { m.Id, m.Starts, m.Ends, m.Name, CourseName = m.Course.Name })
            .ToListAsync(ct);

        if (!modules.Any()) return;

        var newActivities = new List<ProjActivity>();

        foreach (var module in modules)
        {
            var activities = GetActivitiesForModule(module.Name, module.CourseName);
            var moduleStart = module.Starts.ToDateTime(TimeOnly.MinValue);
            var moduleEnd = module.Ends.ToDateTime(TimeOnly.MaxValue);
            var totalHours = (moduleEnd - moduleStart).TotalHours;

            for (int i = 0; i < activities.Length; i++)
            {
                var activity = activities[i];
                var startOffset = (totalHours / activities.Length) * i;
                var activityStart = moduleStart.AddHours(startOffset);
                var activityEnd = activityStart.AddHours(activity.Duration);

                if (activityEnd > moduleEnd)
                    activityEnd = moduleEnd;

                newActivities.Add(new ProjActivity
                {
                    ModuleId = module.Id,
                    Title = activity.Title,
                    Description = activity.Description,
                    Type = activity.Type,
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

    private static (string Title, string Description, string Type, int Duration)[] GetActivitiesForModule(string moduleName, string courseName)
    {
        return (courseName, moduleName) switch
        {
            ("C# Fundamentals", "Grundläggande syntax") => [
                ("Variabler och datatyper", "Lär dig om olika datatyper i C#", "Föreläsning", 2),
                ("Kontrollstrukturer", "If-satser, loopar och switch", "Övning", 3),
                ("Syntax-quiz", "Testa dina kunskaper om C# syntax", "Kunskapskontroll", 1)
            ],
            ("C# Fundamentals", "Objektorienterad programmering") => [
                ("Klasser och objekt", "Grunderna i OOP", "Föreläsning", 2),
                ("Arv och polymorfism", "Avancerade OOP-koncept", "Workshop", 4),
                ("OOP-projekt", "Skapa en enkel applikation", "Projektarbete", 6)
            ],
            ("JavaScript Basics", "Grundläggande JavaScript") => [
                ("JavaScript syntax", "Variabler, funktioner och objekt", "Föreläsning", 2),
                ("DOM-grunderna", "Manipulera HTML med JavaScript", "Övning", 3),
                ("Första JavaScript-appen", "Bygg en enkel kalkylator", "Projektarbete", 4)
            ],
            ("React Development", "React grunder") => [
                ("Introduktion till React", "Vad är React och JSX", "Föreläsning", 2),
                ("Första React-komponenten", "Skapa din första komponent", "Övning", 3),
                ("React-app från scratch", "Bygg en todo-app", "Projektarbete", 5)
            ],
            ("Python Basics", "Python syntax") => [
                ("Python grundsyntax", "Variabler, listor och funktioner", "Föreläsning", 2),
                ("Python-övningar", "Praktiska kodningsövningar", "Övning", 3),
                ("Python-quiz", "Testa dina Python-kunskaper", "Kunskapskontroll", 1)
            ],
            ("ASP.NET Core", "MVC grunderna") => [
                ("MVC-arkitekturen", "Model-View-Controller mönstret", "Föreläsning", 2),
                ("Skapa Controllers", "Bygg dina första controllers", "Övning", 3),
                ("MVC-webbapp", "Komplett webbapplikation", "Projektarbete", 6)
            ],
            ("Fullstack .NET", "C# grunder") => [
                ("C# för fullstack", "C# i fullstack-kontext", "Föreläsning", 2),
                ("Backend-logik", "Skapa business logic", "Övning", 4),
                ("API-design", "Designa RESTful APIs", "Workshop", 3)
            ],
            _ => [
                ("Introduktion", $"Introduktion till {moduleName}", "Föreläsning", 2),
                ("Praktisk övning", $"Övningar inom {moduleName}", "Övning", 3),
                ("Projektarbete", $"Projekt inom {moduleName}", "Projektarbete", 4)
            ]
        };
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

    private static async Task EnsureModulesSeededWithRealisticDataAsync(ApplicationDbContext context, CancellationToken ct)
    {
        var courses = await context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.Starts, c.Ends, c.Name })
            .ToListAsync(ct);

        var newModules = new List<Module>();

        foreach (var c in courses)
        {
            bool hasModules = await context.Modules.AnyAsync(m => m.CourseId == c.Id, ct);
            if (hasModules) continue;

            var moduleNames = GetModuleNamesForCourse(c.Name);
            int totalDays = Math.Max((c.Ends.DayNumber - c.Starts.DayNumber + 1), 7);
            int baseLen = Math.Max(3, totalDays / moduleNames.Length);
            int remainder = totalDays - baseLen * moduleNames.Length;

            var currentStart = c.Starts;

            for (int i = 0; i < moduleNames.Length; i++)
            {
                int len = baseLen + (remainder-- > 0 ? 1 : 0);
                var currentEnd = currentStart.AddDays(len - 1);

                if (currentEnd.DayNumber > c.Ends.DayNumber)
                    currentEnd = c.Ends;

                newModules.Add(new Module
                {
                    CourseId = c.Id,
                    Name = moduleNames[i],
                    Description = $"Modul {i + 1} för {c.Name}",
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

    private static string[] GetModuleNamesForCourse(string courseName)
    {
        return courseName switch
        {
            "C# Fundamentals" => ["Grundläggande syntax", "Objektorienterad programmering", "Collections och LINQ", "Felhantering och debugging"],
            "JavaScript Basics" => ["Grundläggande JavaScript", "DOM-manipulation", "Asynkron programmering", "ES6+ funktioner"],
            "React Development" => ["React grunder", "Components och Props", "State och Hooks", "Routing och Context"],
            "Python Basics" => ["Python syntax", "Datastrukturer", "Funktioner och moduler", "Filhantering och API:er"],
            "ASP.NET Core" => ["MVC grunderna", "Entity Framework", "API utveckling", "Säkerhet och autentisering"],
            "Fullstack .NET" => ["C# grunder", "ASP.NET Core API", "Blazor frontend", "Databaser och deployment"],
            _ => ["Modul 1", "Modul 2", "Modul 3", "Modul 4"]
        };
    }

    private async Task AddDemoUsersAsync()
    {
        var demoUsers = new[]
        {
            new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "teacher",
                NormalizedUserName = "TEACHER",
                Email = "teacher@test.com",
                NormalizedEmail = "TEACHER@TEST.COM",
                FirstName = "Julia",
                LastName = "Svensson",
                EmailConfirmed = true,
                RefreshToken = Guid.NewGuid().ToString(),
                RefreshTokenExpireTime = DateTime.UtcNow.AddDays(30)
            },
            new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "student",
                NormalizedUserName = "STUDENT",
                Email = "student@test.com",
                NormalizedEmail = "STUDENT@TEST.COM",
                FirstName = "Anders",
                LastName = "Andersson",
                EmailConfirmed = true,
                RefreshToken = Guid.NewGuid().ToString(),
                RefreshTokenExpireTime = DateTime.UtcNow.AddDays(30)
            }
        };

        await AddUserToDb(demoUsers);

        await userManager.AddToRoleAsync(demoUsers[0], TeacherRole);
        await userManager.AddToRoleAsync(demoUsers[1], StudentRole);
    }

    private async Task AddUsersAsync(int nrOfUsers)
    {
        var domains = new[] { "gmail.com", "hotmail.com", "outlook.com", "yahoo.com" };

        var faker = new Faker<ApplicationUser>("sv")
            .Rules((f, e) =>
            {
                e.Id = Guid.NewGuid().ToString();
                e.FirstName = f.Name.FirstName();
                e.LastName = f.Name.LastName();

                var domain = f.PickRandom(domains);

                var cleanFirstName = e.FirstName.ToLower()
                    .Replace("å", "a").Replace("ä", "a").Replace("ö", "o")
                    .Replace(" ", "").Replace("-", "");

                var cleanLastName = e.LastName.ToLower()
                    .Replace("å", "a").Replace("ä", "a").Replace("ö", "o")
                    .Replace(" ", "").Replace("-", "");

                e.Email = $"{cleanFirstName}.{cleanLastName}@{domain}";
                e.NormalizedEmail = e.Email.ToUpper();
                e.UserName = e.Email.Split('@')[0];
                e.NormalizedUserName = e.UserName.ToUpper();

                e.PhoneNumber = f.Phone.PhoneNumber();
                e.EmailConfirmed = true;
                e.LockoutEnabled = true;
                e.RefreshToken = f.Random.Guid().ToString();
                e.RefreshTokenExpireTime = DateTime.UtcNow.AddDays(f.Random.Int(1, 30));
            });

        var users = faker.Generate(nrOfUsers);
        await AddUserToDb(users);

        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var role = i < 6 ? "Teacher" : "Student"; // Only 6 teachers instead of 12

            var result = await userManager.AddToRoleAsync(user, role);

            if (!result.Succeeded)
            {
                throw new Exception(
                    $"Could not assign role {role} to {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                );
            }
        }
    }

    private async Task AddCourseUserRelationship(IEnumerable<ApplicationUser> users, int usersNoCourse = 0)
    {
        var courses = await dbContext.Courses.ToListAsync();
        if (!courses.Any()) return;

        var courseUsers = new List<CourseUser>();
        var usersList = users.ToList();

        var teachers = new List<ApplicationUser>();
        var students = new List<ApplicationUser>();
        var demoTeacher = usersList.FirstOrDefault(u => u.Email == "teacher@test.com");
        var demoStudent = usersList.FirstOrDefault(u => u.Email == "student@test.com");

        foreach (var user in usersList)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains(TeacherRole))
                teachers.Add(user);
            else
                students.Add(user);
        }

        // Remove demo users from regular lists
        if (demoTeacher != null) teachers.Remove(demoTeacher);
        if (demoStudent != null) students.Remove(demoStudent);

        var shuffledStudents = students.OrderBy(x => Guid.NewGuid()).ToList();
        int studentIndex = 0;

        for (int courseIndex = 0; courseIndex < courses.Count; courseIndex++)
        {
            var course = courses[courseIndex];

            // Demo teacher gets courses 0, 1, and 2 (3 courses total)
            if (courseIndex <= 2 && demoTeacher != null)
            {
                courseUsers.Add(new CourseUser
                {
                    UserId = demoTeacher.Id,
                    CourseId = course.Id,
                    IsTeacher = true
                });
            }

            // Demo student only in first course
            if (courseIndex == 0 && demoStudent != null)
            {
                courseUsers.Add(new CourseUser
                {
                    UserId = demoStudent.Id,
                    CourseId = course.Id,
                    IsTeacher = false
                });
            }

            // Add 2 regular teachers per course (each teacher gets 2 courses)
            for (int teacherSlot = 0; teacherSlot < 2; teacherSlot++)
            {
                var teacherIndex = (courseIndex + teacherSlot * 3) % teachers.Count;
                var teacher = teachers[teacherIndex];

                courseUsers.Add(new CourseUser
                {
                    UserId = teacher.Id,
                    CourseId = course.Id,
                    IsTeacher = true
                });
            }

            // Add 5 students per course (6 for first course with demo student)
            int studentsToAdd = courseIndex == 0 ? 5 : 5;
            for (int i = 0; i < studentsToAdd && studentIndex < shuffledStudents.Count; i++, studentIndex++)
            {
                var student = shuffledStudents[studentIndex];
                courseUsers.Add(new CourseUser
                {
                    UserId = student.Id,
                    CourseId = course.Id,
                    IsTeacher = false
                });
            }
        }

        if (courseUsers.Any())
        {
            dbContext.CourseUsers.AddRange(courseUsers);
            await dbContext.SaveChangesAsync();
        }
    }



    private async Task AddUserToDb(IEnumerable<ApplicationUser> users)
    {
        var passWord = configuration["password"];
        ArgumentNullException.ThrowIfNull(passWord, nameof(passWord));

        foreach (var user in users)
        {
            var result = await userManager.CreateAsync(user, passWord);
            if (!result.Succeeded)
            {
                var errors = string.Join("\n", result.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create user {user.Email}: {errors}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
