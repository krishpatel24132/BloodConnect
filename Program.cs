using BloodConnect.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Entity Framework Core + MySQL (Pomelo provider)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// A fixed server version avoids connecting before the database exists (first run).
// Using MariaDB instead of MySQL? Replace with: new MariaDbServerVersion(new Version(10, 11, 0))
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36))));

// State management: server-side session (login state + remembered search filters)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Friendly page for 404 and other status codes
app.UseStatusCodePagesWithReExecute("/Home/PageNotFound", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Create the database / tables on first run and add demo data.
// If MySQL is not running or the password is wrong, show a clear message instead of a long stack trace.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        SeedData.Initialize(db);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine();
        Console.WriteLine("==================================================================");
        Console.WriteLine(" Could not connect to the MySQL database.");
        Console.WriteLine(" 1. Is MySQL running? (XAMPP: press Start next to MySQL)");
        Console.WriteLine(" 2. Is the password correct in appsettings.json (DefaultConnection)?");
        Console.WriteLine("==================================================================");
        Console.ResetColor();
        Console.WriteLine("Details: " + (ex.InnerException?.Message ?? ex.Message));
        return;
    }
}

app.Run();
