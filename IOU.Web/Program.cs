using IOU.Web.Data;
using IOU.Web.Models;
using IOU.Web.Services.Interfaces;
using IOU.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IOU.Web.Config;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); // Add this for Identity pages

// Add HttpClient for Mpesa
builder.Services.AddHttpClient("mpesa", c =>
{
    c.BaseAddress = new Uri("https://sandbox.safaricom.co.ke");
});

// Add DbContext
builder.Services.AddDbContext<IOUWebContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity (only once)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<IOUWebContext>()
    .AddDefaultTokenProviders();

// Configure cookie policy
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register custom services
builder.Services.AddScoped<IDebtCalculationService, DebtCalculationService>();
builder.Services.AddScoped<IDebtService, DebtService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IScheduledPaymentService, ScheduledPaymentService>();
builder.Services.AddScoped<IRazorViewRenderer, RazorViewRenderer>();
builder.Services.AddScoped<IEmailService, MailJetEmailService>(); // Register IEmailService
builder.Services.AddScoped<IMpesaService, MpesaService>();
builder.Services.AddScoped<ICreditReportService, CreditReportService>();
builder.Services.AddSingleton<NgrokService>();
builder.Services.AddHostedService<NgrokMonitorService>();
builder.Services.Configure<MpesaConfiguration>(
    builder.Configuration.GetSection("MpesaConfiguration"));
// In Program.cs, make sure this is correct:
builder.Services.AddHttpClient("Mpesa", c =>
{
    var baseUrl = builder.Configuration["MpesaConfiguration:BaseUrl"];
    if (!string.IsNullOrEmpty(baseUrl))
    {
        c.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }
    c.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
});
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebApp", policy =>
    {
        policy.WithOrigins("https://your-frontend-url.com")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});



// Add Razor runtime compilation
builder.Services.AddMvc().AddRazorRuntimeCompilation();



var app = builder.Build();
app.UseCors("AllowWebApp");
// Seed Roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var roles = new[] { "Admin", "Student", "Lender" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication(); // Add this line
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.MapRazorPages(); // Add this for Identity pages

app.Run();