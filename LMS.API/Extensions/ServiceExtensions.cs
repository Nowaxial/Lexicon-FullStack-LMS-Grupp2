using LMS.Infractructure.Data;
using LMS.Infractructure.Repositories;
using LMS.Infractructure.Storage;
using LMS.Presentation;
using LMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Service.Contracts.Storage;

namespace LMS.API.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", p =>
                p.WithOrigins(
                    "https://localhost:7224"  // Local Blazor
                                                    // Azure Blazor
                  
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        });
    }


    public static void ConfigureOpenApi(this IServiceCollection services) =>
       services.AddEndpointsApiExplorer()
               .AddSwaggerGen(setup =>
               {
                   setup.EnableAnnotations();

                   setup.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                   {
                       In = ParameterLocation.Header,
                       Description = "Place to add JWT with Bearer",
                       Name = "Authorization",
                       Type = SecuritySchemeType.Http,
                       Scheme = "Bearer"
                   });

                   setup.AddSecurityRequirement(new OpenApiSecurityRequirement
                   {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Bearer",
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            new List<string>()
                        }
                   });
               });

    public static void ConfigureControllers(this IServiceCollection services)
    {
        services.AddControllers(opt =>
        {
            // Reject requests asking for anything other than JSON
            opt.ReturnHttpNotAcceptable = true;

            // Force all controllers/actions to produce JSON
            opt.Filters.Add(new ProducesAttribute("application/json"));

            // Optional: clear other formatters (forces JSON only)
            opt.OutputFormatters.Clear();
        })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        })
        .AddApplicationPart(typeof(AssemblyReference).Assembly);
    }

    public static void ConfigureSql(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("ApplicationDbContext") ?? throw new InvalidOperationException("Connection string 'ApplicationDbContext' not found.")));
    }

    public static void AddRepositories(this IServiceCollection services)
    {

        services.AddScoped<IProjActivityRepository, ProjActivityRepository>();
        services.AddScoped(provider =>
            new Lazy<IProjActivityRepository>(() => provider.GetRequiredService<IProjActivityRepository>()));


        // Concrete repo
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseUserRepository, CourseUserRepository>();
        services.AddScoped<IModuleRepository, ModuleRepository>();
        services.AddScoped<IProjDocumentRepository, ProjDocumentRepository>();

        // Lazy<ICourseRepository> for UnitOfWork ctor
        services.AddScoped(provider =>
            new Lazy<ICourseRepository>(() => provider.GetRequiredService<ICourseRepository>()));
        services.AddScoped(provider =>
            new Lazy<ICourseUserRepository>(() => provider.GetRequiredService<ICourseUserRepository>()));

        services.AddScoped(provider =>
           new Lazy<IModuleRepository>(() => provider.GetRequiredService<IModuleRepository>()));
        services.AddScoped(provider => 
           new Lazy<IProjDocumentRepository>(() => provider.GetRequiredService<IProjDocumentRepository>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();


    }

    public static void AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IServiceManager, ServiceManager>();

        // Auth
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped(provider => new Lazy<IAuthService>(() =>
            provider.GetRequiredService<IAuthService>()));

        // Courses
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped(provider => new Lazy<ICourseService>(() =>
            provider.GetRequiredService<ICourseService>()));

        services.AddScoped<IUserService, UserService>();
        services.AddScoped(provider => new Lazy<IUserService>(() =>
            provider.GetRequiredService<IUserService>()));


        services.AddScoped<IModuleService, ModuleService>();
        services.AddScoped(provider => new Lazy<IModuleService>(() => provider.GetRequiredService<IModuleService>()));

        services.AddScoped<IProjActivityService, ProjActivityService>();
        services.AddScoped(provider => new Lazy<IProjActivityService>(() => provider.GetRequiredService<IProjActivityService>()));

        services.AddScoped<IProjDocumentService, ProjDocumentService>();
        services.AddScoped(provider => new Lazy<IProjDocumentService>(() =>
            provider.GetRequiredService<IProjDocumentService>()));

        // Notification services
        services.AddScoped<EncryptionService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped(provider => new Lazy<INotificationService>(() =>
            provider.GetRequiredService<INotificationService>()));

    }
    public static void AddStorage(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<FileStorageOptions>(config.GetSection("FileStorage"));
        services.AddScoped<IFileStorage, LocalFileStorage>();
    }
}
