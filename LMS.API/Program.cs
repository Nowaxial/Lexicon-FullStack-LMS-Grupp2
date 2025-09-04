using LMS.API.Extensions;
using LMS.API.Services;
using LMS.Infractructure.Data;
using Microsoft.AspNetCore.Identity;

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

        //hej


        //Showing
    }
}