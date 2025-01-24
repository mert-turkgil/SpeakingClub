using System.Globalization;
using System.Reflection;
using UI.Data.Concrete;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using UI.EmailServices;
using UI.Services;
using UI.Identity;
using Microsoft.AspNetCore.Identity;
using UI.Data.Abstract;
using UI.Data.Conrete;
using Data.Abstract;
using Data.Concrete.EfCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add User Secrets configuration
builder.Configuration.AddUserSecrets<Program>();

#region Dependencies
    var config = builder.Configuration;
// Retrieve user information
    var username = config["Data:Users:0:username"];
    var role = config["Data:Users:0:role"];
    var password = config["Data:Users:0:password"];
    var firstName = config["Data:Users:0:firstName"];
    var lastName = config["Data:Users:0:lastName"];
    var email = config["Data:Users:0:email"];
    var userRole = config["Data:Roles:0"];

// Retrieve SMTP configuration
    var smtpHost = config["EmailSender:SMTPMail"] ?? throw new InvalidOperationException("SMTP host not configured.");
    var smtpPort = config.GetValue<int>("EmailSender:Port");
    var smtpEnableSSL = config.GetValue<bool>("EmailSender:EnableSSL");
    var smtpUsername = config["EmailSender:Username"] ?? throw new InvalidOperationException("SMTP username not configured.");
    var smtpPassword = config["EmailSender:Password"] ?? throw new InvalidOperationException("SMTP password not configured.");

// DataBase String
    var connectionString = builder.Configuration.GetConnectionString("MsSqlConnection")
        ?? throw new InvalidOperationException("Connection string 'MsSqlConnection' not found.");
#endregion

#region DataBase
    builder.Services.AddDbContext<ShopContext>(options =>
        options.UseSqlServer(connectionString), ServiceLifetime.Scoped);

    // Add DbContext for Identity
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString), ServiceLifetime.Scoped);
#endregion
#region Identity
    // Add Identity
    builder.Services.AddIdentity<User, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

#endregion
#region Settings

#endregion

#region Localizer
    builder.Services.AddSingleton<LanguageService>();
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
    builder.Services.AddMvc().AddViewLocalization().AddDataAnnotationsLocalization(options => options.DataAnnotationLocalizerProvider=(type, factory) => {
        var assemblyName = new AssemblyName(typeof(SharedResource).GetTypeInfo().Assembly.FullName!);
        return factory.Create(nameof(SharedResource), assemblyName.Name!);
    });
    builder.Services.Configure<RequestLocalizationOptions>(options => {
        var supportedCultures = new List<CultureInfo> {
            new CultureInfo("de-DE"),
            new CultureInfo("tr-TR")
        };
        options.DefaultRequestCulture = new RequestCulture(culture: "de-DE", uiCulture: "de-DE");
        options.SupportedCultures = supportedCultures;
        options.SupportedUICultures = supportedCultures;
        options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    });
#endregion

#region Services
    string resourcesPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources");
    builder.Services.AddScoped<INavbarService, NavbarService>();
    builder.Services.AddScoped<IBlogRepository, BlogRepository>();
    builder.Services.AddScoped<ICarouselRepository, CarouselRepository>();
    builder.Services.AddScoped<IQuizRepository, QuizRepository>();
    builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(resourcesPath));
    builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddScoped<IEmailSender, SmtpEmailSender>(i => 
        new SmtpEmailSender(smtpHost, smtpPort, smtpEnableSSL, smtpUsername, smtpPassword)
    );
#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
    // Seed roles and users
    using (var scope = app.Services.CreateScope())
    {
        var serviceProvider = scope.ServiceProvider;
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>(); // Ensure UserManager<User> is used
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        await SeedIdentity.Seed(userManager, roleManager, configuration);
    }


app.UseHttpsRedirection();
// Serve files from wwwroot by default
app.UseStaticFiles();

// Serve files from node_modules with a custom RequestPath
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "node_modules")),
    RequestPath = "/node_modules"
});

app.UseRequestLocalization();
app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();