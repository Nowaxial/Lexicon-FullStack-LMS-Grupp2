using Domain.Models.Entities;
using LMS.Blazor;
using LMS.Blazor.Client.Services;
using LMS.Blazor.Components;
using LMS.Blazor.Components.Account;
using LMS.Blazor.Data;
using LMS.Blazor.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddControllers();

// Authentication-related services
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

// AuthenticationStateProvider for persistent authentication
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();

// API service
builder.Services.AddScoped<IApiService, ClientApiService>();

// Authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

// Database setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Identity setup with roles
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Email sender (no-op in this case)
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// HTTP Client configuration
builder.Services.AddHttpClient("LmsAPIClient", cfg =>
{
    cfg.BaseAddress = new Uri(builder.Configuration["LmsAPIBaseAddress"] ?? throw new Exception("LmsAPIBaseAddress is missing."));
});

// Password hashing configuration
builder.Services.Configure<PasswordHasherOptions>(options => options.IterationCount = 10000);

// Token storage service
builder.Services.AddSingleton<ITokenStorage, TokenStorageService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts(); // Ensure HSTS is applied in production
}

app.UseHttpsRedirection();


app.UseStaticFiles();
app.MapStaticAssets();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LMS.Blazor.Client._Imports).Assembly);

app.MapControllers();

app.MapAdditionalIdentityEndpoints();

app.Run();
