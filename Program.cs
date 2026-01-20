using System.Globalization;
using System.Reflection;
using Castle.Core.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Polly;
using SpeakingClub.Data;
using SpeakingClub.Data.Abstract;
using SpeakingClub.Data.Concrete;
using SpeakingClub.Data.Configuration;
using SpeakingClub.Identity;
using SpeakingClub.Models;
using SpeakingClub.Services;

var builder = WebApplication.CreateBuilder(args);

// Set environment to Development (temporarily for debugging)
builder.Environment.EnvironmentName = "Development";

#region Configuration
// Load configuration
var config = builder.Configuration;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();
Console.WriteLine("ENV: " + builder.Environment.EnvironmentName);
// Configure EmailSender settings
var emailSettings = config.GetSection("EmailSender");
var port = emailSettings.GetValue<int>("Port");
var host = emailSettings.GetValue<string>("SMTPMail") ?? "localhost";
var enablessl = true;
var username = emailSettings.GetValue<string>("Username") ?? "dummy@example.com";
var password = emailSettings.GetValue<string>("Password") ?? "dummy-password";
var provider = new FileExtensionContentTypeProvider();
// If .glb is not mapped, add it:
provider.Mappings[".glb"] = "model/gltf-binary";

// Only validate email settings if not in Development mode and settings are actually configured
if (!builder.Environment.IsDevelopment() && !string.IsNullOrEmpty(emailSettings.GetValue<string>("SMTPMail")))
{
    if (string.IsNullOrEmpty(host))
        throw new ArgumentNullException(nameof(host), "SMTP host cannot be null or empty.");
    if (port <= 0)
        throw new ArgumentOutOfRangeException(nameof(port), "SMTP port must be a positive number.");
    if (string.IsNullOrEmpty(username))
        throw new ArgumentNullException(nameof(username), "SMTP username cannot be null or empty.");
    if (string.IsNullOrEmpty(password))
        throw new ArgumentNullException(nameof(password), "SMTP password cannot be null or empty.");
}
#endregion

#region DbContext Registration
// Register SpeakingClubContext (your domain entities)
builder.Services.AddDbContext<SpeakingClubContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection"))
    // options.UseSqlite(config.GetConnectionString("DefaultConnection")) // Use SQLite when needed.
);

// Register ApplicationDbContext (for Identity)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection"))
);
#endregion

#region Identity Registration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = true;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.MaxFailedAccessAttempts = 4;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(4);
    options.Lockout.AllowedForNewUsers = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.ClaimsIdentity.RoleClaimType = System.Security.Claims.ClaimTypes.Role;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
#endregion
#region Identity Cookie Configuration

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Giriş sayfası
    options.LogoutPath = "/Account/Logout"; // Çıkış sayfası
    options.AccessDeniedPath = "/Account/AccessDenied"; // Yetkisiz erişim
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Increased default session timeout
    options.Cookie = new CookieBuilder
    {
        Name = "SpeakingClubAuth",
        HttpOnly = true,
        SameSite = SameSiteMode.Lax,
        SecurePolicy = CookieSecurePolicy.SameAsRequest, // Changed for Plesk compatibility
        IsEssential = true, // Required for GDPR compliance and Plesk
        Path = "/"
    };
    // Regenerate cookie to prevent 403 errors
    options.Events.OnValidatePrincipal = context =>
    {
        // Check if the cookie is about to expire (within 30% of its lifetime)
        var timeElapsed = DateTimeOffset.UtcNow - context.Properties.IssuedUtc;
        var timeRemaining = context.Properties.ExpiresUtc - DateTimeOffset.UtcNow;
        
        if (timeElapsed.HasValue && timeRemaining.HasValue)
        {
            var totalTime = timeElapsed.Value + timeRemaining.Value;
            if (timeRemaining.Value < TimeSpan.FromTicks(totalTime.Ticks / 3))
            {
                // Refresh the cookie
                context.ShouldRenew = true;
            }
        }
        
        return Task.CompletedTask;
    };
});

#endregion
#region Security

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "SpeakingClubCSRF";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // For Plesk compatibility
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

#endregion
#region Additional Services
// Register UnitOfWork extension (repositories accessible via IUnitOfWork)
builder.Services.AddUnitOfWork();
// Configure Email Sender
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(sp =>
    new SmtpEmailSender(host!, port, enablessl, username!, password!));

// Register resource services and file provider for localization/resources.
var resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
builder.Services.AddSingleton<AliveResourceService>(p => new AliveResourceService(resourcesPath));
builder.Services.AddSingleton<IManageResourceService>(sp => new ManageResourceService(resourcesPath));
builder.Services.AddSingleton<LanguageService>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddHttpClient<IDictionaryService, DictionaryService>();
builder.Services.AddHttpClient<IDeeplService, DeeplService>()
    .AddTransientHttpErrorPolicy(policyBuilder =>
        policyBuilder.WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(10)));
builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(resourcesPath));
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// Register background service for temp file cleanup
builder.Services.AddHostedService<TempFileCleanupService>();
#endregion

#region Localization and MVC Configuration
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        var assemblyInfo = typeof(SharedResource).GetTypeInfo().Assembly;
        var assemblyName = new AssemblyName(assemblyInfo.FullName ?? throw new InvalidOperationException("Assembly full name cannot be null."));
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        {
            var location = assemblyName.Name ?? throw new ArgumentNullException(nameof(assemblyName.Name), "Assembly name cannot be null or empty.");
            return factory.Create(nameof(SharedResource), location);
        };
    });

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("de-DE"),
        new CultureInfo("tr-TR")
    };

    options.DefaultRequestCulture = new RequestCulture("tr-TR", "tr-TR");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
});

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization()
    .AddRazorRuntimeCompilation();
#endregion

var app = builder.Build();

#region Database Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    var speakingClubContext = scope.ServiceProvider.GetRequiredService<SpeakingClubContext>();
    var applicationDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        if (app.Environment.IsDevelopment())
        {
            // Automatically apply migrations in development mode
            Console.WriteLine("Attempting database migration...");
            speakingClubContext.Database.Migrate();
            applicationDbContext.Database.Migrate();
            Console.WriteLine("Database migration completed successfully.");
        }
        else
        {
            // Only run migration once in production
            var hasAppliedMigrations = speakingClubContext.Database.GetAppliedMigrations().Any();
            if (!hasAppliedMigrations)
            {
                speakingClubContext.Database.Migrate();
                applicationDbContext.Database.Migrate();
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Database migration failed: {ex.Message}");
        Console.WriteLine("Continuing startup without database...");
    }

    // Identity seeding and role/user checking (safe to run in production)
    try
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var usersConfig = config.GetSection("Data:Users").GetChildren();
    if (usersConfig == null || !usersConfig.Any())
    {
        Console.WriteLine("No user configuration found.");
    }
    else
    {
        var rootUserSection = usersConfig.FirstOrDefault(user =>
            user.GetValue<string>("UserName")?.Equals("Root", StringComparison.OrdinalIgnoreCase) == true);
        var adminUserSection = usersConfig.FirstOrDefault(user =>
            user.GetValue<string>("UserName")?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true);
        bool seedRequired = false;

        if (rootUserSection != null)
        {
            var rootEmail = rootUserSection.GetValue<string>("email");
            if (string.IsNullOrEmpty(rootEmail))
                throw new ArgumentNullException(nameof(rootEmail), "Root email cannot be null or empty.");

            var rootUser = await userManager.FindByEmailAsync(rootEmail!);
            if (rootUser == null)
            {
                Console.WriteLine("Root user does not exist.");
                seedRequired = true;
            }
        }
        else
        {
            Console.WriteLine("Root user configuration not found.");
        }

        if (seedRequired)
        {
            Console.WriteLine("Seeding roles and users...");
            await SeedIdentity.Seed(userManager, roleManager, config);
        }
    }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: User seeding failed: {ex.Message}");
    }
}
#endregion

#region HTTP Pipeline Configuration

// Configure ForwardedHeaders for Cloudflare proxy
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    RequireHeaderSymmetry = false,
    ForwardLimit = 2
};
// Add Cloudflare IPs - trust their headers
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
// Trust all proxies for Cloudflare (adjust if needed)
forwardedHeadersOptions.AllowedHosts.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

if (!app.Environment.IsDevelopment())
{
    // Hata sayfası ve HSTS
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware: ACME challenge ve HTTPS yönlendirme
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    // 1. ACME challenge → yönlendirme yok
    if (path != null && path.StartsWith("/.well-known/acme-challenge"))
    {
        await next();
        return;
    }

    // 2. Diğer tüm istekler → HTTPS’ye yönlendir
    if (!context.Request.IsHttps)
    {
        var httpsUrl = $"https://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        context.Response.Redirect(httpsUrl, permanent: true);
        return;
    }

    await next();
});

// 3. ACME challenge dosyalarını serve et
var acmeChallengePath = Path.Combine(Directory.GetCurrentDirectory(), ".well-known", "acme-challenge");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(acmeChallengePath),
    RequestPath = "/.well-known/acme-challenge",
    ServeUnknownFileTypes = true,
    ContentTypeProvider = new FileExtensionContentTypeProvider()
});

// 4. Normal statik dosyalar
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = true
});

// Localization
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

// Routing ve auth
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// MVC route'ları
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// app.MapControllerRoute(
//     name: "blogdetail",
//     pattern: "blog/{url}",
//     defaults: new { controller = "Home", action = "BlogDetail" });

app.MapControllerRoute(
    name: "about",
    pattern: "about",
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "privacy",
    pattern: "privacy",
    defaults: new { controller = "Home", action = "Privacy" });

app.MapControllerRoute(
    name: "words",
    pattern: "words",
    defaults: new { controller = "Home", action = "Words" });

// SEO Routes
app.MapControllerRoute(
    name: "sitemap",
    pattern: "sitemap.xml",
    defaults: new { controller = "Sitemap", action = "Index" });

#endregion

#region SignalR Configuration
app.MapHub<SpeakingClub.Hubs.QuizMonitorHub>("/quizMonitorHub");
#endregion

Console.WriteLine("");
Console.WriteLine("🚀 Application starting up...");
Console.WriteLine("🌐 Application will be available at:");
Console.WriteLine("   🔒 HTTPS: https://localhost:5001");
Console.WriteLine("   🔒 HTTPS: https://localhost:5000");
Console.WriteLine("");

app.Run();
