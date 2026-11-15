using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Read the database URL from environment configuration. In production, set
// ConnectionStrings__DefaultConnection (or MYSQL_URL) in the hosting dashboard.
var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("MYSQL_URL");

if (string.IsNullOrWhiteSpace(configuredConnection))
{
    throw new InvalidOperationException(
        "Database connection is missing. Set ConnectionStrings__DefaultConnection or MYSQL_URL.");
}

// Aiven/Railway may provide a mysql:// URL, while MySqlConnector expects
// a standard ADO.NET connection string.
var connectionString = configuredConnection;
if (Uri.TryCreate(configuredConnection, UriKind.Absolute, out var databaseUri) &&
    (databaseUri.Scheme.Equals("mysql", StringComparison.OrdinalIgnoreCase) ||
     databaseUri.Scheme.Equals("mariadb", StringComparison.OrdinalIgnoreCase)))
{
    var credentials = databaseUri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
    {
        throw new InvalidOperationException("The MySQL URL must contain a username and password.");
    }

    var mysqlBuilder = new MySqlConnectionStringBuilder
    {
        Server = databaseUri.Host,
        Port = (uint)(databaseUri.IsDefaultPort ? 3306 : databaseUri.Port),
        UserID = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        Database = databaseUri.AbsolutePath.Trim('/'),
        SslMode = MySqlSslMode.Required
    };
    connectionString = mysqlBuilder.ConnectionString;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://hotelix-project.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add the demo room catalogue only when the rooms table is empty.
// Existing room data is never overwritten.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (await db.Database.CanConnectAsync() && !await db.Rooms.AnyAsync())
    {
        db.Rooms.AddRange(
            new Room { RoomType = "Deluxe Ocean View", Price = 150m, Status = "Available" },
            new Room { RoomType = "Executive Suite", Price = 250m, Status = "Available" },
            new Room { RoomType = "Standard Garden Room", Price = 100m, Status = "Available" },
            new Room { RoomType = "Superior Twin Room", Price = 120m, Status = "Available" },
            new Room { RoomType = "Family Suite", Price = 300m, Status = "Available" },
            new Room { RoomType = "Presidential Suite", Price = 500m, Status = "Available" },
            new Room { RoomType = "Overwater Villa", Price = 350m, Status = "Available" },
            new Room { RoomType = "Penthouse Suite", Price = 450m, Status = "Available" },
            new Room { RoomType = "Cozy Garden Studio", Price = 130m, Status = "Available" },
            new Room { RoomType = "Ocean View Studio", Price = 180m, Status = "Available" }
        );
        await db.SaveChangesAsync();
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database startup check or initial room seeding failed.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
