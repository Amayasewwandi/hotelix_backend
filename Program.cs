using Microsoft.EntityFrameworkCore;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

var configuredConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("MYSQL_URL");

if (string.IsNullOrWhiteSpace(configuredConnection))
    throw new InvalidOperationException("Database connection is missing. Set MYSQL_URL in Railway Variables.");

var connectionString = configuredConnection;
if (Uri.TryCreate(configuredConnection, UriKind.Absolute, out var databaseUri) &&
    (databaseUri.Scheme.Equals("mysql", StringComparison.OrdinalIgnoreCase) ||
     databaseUri.Scheme.Equals("mariadb", StringComparison.OrdinalIgnoreCase)))
{
    var credentials = databaseUri.UserInfo.Split(':', 2);
    if (credentials.Length != 2)
        throw new InvalidOperationException("MYSQL_URL must contain username and password.");

    var mysqlBuilder = new MySqlConnectionStringBuilder
    {
        Server = databaseUri.Host,
        Port = (uint)(databaseUri.IsDefaultPort ? 3306 : databaseUri.Port),
        UserID = Uri.UnescapeDataString(credentials[0]),
        Password = Uri.UnescapeDataString(credentials[1]),
        Database = databaseUri.AbsolutePath.Trim('/'),
        SslMode = MySqlSslMode.Required,
        ConnectionTimeout = 15
    };
    connectionString = mysqlBuilder.ConnectionString;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 0))));

builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("https://hotelix-project.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader()));

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Liveness: confirms that the .NET process is running, without requiring MySQL.
app.MapGet("/health", () => Results.Ok(new
{
    status = "running",
    application = "Hotelix API",
    runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription
}));

// Readiness: confirms that the API can connect to MySQL and read the rooms table.
app.MapGet("/health/db", async (ApplicationDbContext db, ILogger<Program> logger) =>
{
    try
    {
        await db.Database.OpenConnectionAsync();
        await db.Database.CloseConnectionAsync();
        var roomCount = await db.Rooms.CountAsync();
        return Results.Ok(new { status = "connected", database = "defaultdb", roomCount });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Aiven MySQL health check failed.");
        return Results.Problem("MySQL connection or database schema check failed. Check Railway deployment logs.", statusCode: 503);
    }
});

try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (await db.Database.CanConnectAsync())
    {
        var seedRooms = new[]
        {
            new Room { RoomType = "Deluxe Ocean View", Price = 150m, Status = "Available" },
            new Room { RoomType = "Executive Suite", Price = 250m, Status = "Available" },
            new Room { RoomType = "Standard Garden Room", Price = 100m, Status = "Available" },
            new Room { RoomType = "Superior Twin Room", Price = 120m, Status = "Available" },
            new Room { RoomType = "Family Suite", Price = 300m, Status = "Available" },
            new Room { RoomType = "Presidential Suite", Price = 500m, Status = "Available" },
            new Room { RoomType = "Overwater Villa", Price = 350m, Status = "Available" },
            new Room { RoomType = "Penthouse Suite", Price = 450m, Status = "Available" },
            new Room { RoomType = "Cozy Garden Studio", Price = 90m, Status = "Available" },
            new Room { RoomType = "Deluxe Double Room", Price = 130m, Status = "Available" }
        };

        var existingNames = (await db.Rooms.Select(r => r.RoomType).ToListAsync())
            .Where(n => n != null)
            .Select(n => n.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missing = seedRooms.Where(r => !existingNames.Contains(r.RoomType)).ToList();
        if (missing.Count > 0)
        {
            db.Rooms.AddRange(missing);
            await db.SaveChangesAsync();
            logger.LogInformation("Inserted {Count} missing demo rooms.", missing.Count);
        }

        logger.LogInformation("Database connected. Rooms currently in database: {Count}", await db.Rooms.CountAsync());
    }
    else
    {
        logger.LogError("API started, but Aiven MySQL is unreachable. Verify MYSQL_URL in Railway.");
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Database startup or room seeding failed. Check /health/db and Railway logs.");
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
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
