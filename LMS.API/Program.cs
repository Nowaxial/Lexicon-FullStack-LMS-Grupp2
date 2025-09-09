using LMS.API.Extensions;
using LMS.API.Services;
using LMS.Infractructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;

namespace LMS.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --- Services ---
        builder.Services.ConfigureSql(builder.Configuration);      // DbContext
        builder.Services.ConfigureControllers();                   // MVC / JSON options

        builder.Services.AddRepositories();                        // UoW + repos (Course, Module, etc.)
        builder.Services.AddServiceLayer();                        // AuthService, CourseService, UserService (+ Lazy<>)
        builder.Services.AddStorage(builder.Configuration);


        // Identity first, then Authentication (JWT)
        builder.Services.ConfigureIdentity();                      // Identity + UserManager/RoleManager
        builder.Services.ConfigureAuthentication(builder.Configuration); // JWT bearer

        builder.Services.AddAuthorization();                       // (policies optional; attributes will work)

        builder.Services.AddHostedService<DataSeedHostingService>();
        builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperProfile>());
        builder.Services.ConfigureCors();                          // "AllowAll"
        builder.Services.ConfigureOpenApi();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "LMS API", Version = "v1" });
     
            c.ResolveConflictingActions(api => api.First());
            c.CustomSchemaIds(t => t.FullName);

            var asm = typeof(Program).Assembly;
            var xml = Path.Combine(AppContext.BaseDirectory, $"{asm.GetName().Name}.xml");
            if (File.Exists(xml))
                c.IncludeXmlComments(xml, includeControllerXmlComments: true);
        });


        // Swagger + JWT support (per your extension)

        var app = builder.Build();

        // --- Middleware ---
        app.ConfigureExceptionHandler();                           // global error handler

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(opt =>
            {
                opt.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");

            });
        }

        app.UseHttpsRedirection();

        app.UseCors("AllowAll");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}