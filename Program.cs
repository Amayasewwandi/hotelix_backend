
var builder = WebApplication.CreateBuilder(args);

// 1. Configure CORS for the Hotelix frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://hotelix-project.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 2. Register controllers and views
builder.Services.AddControllersWithViews();

// 3. Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 4. Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

// CORS must be after UseRouting
// and before authorization
app.UseCors("AllowFrontend");

app.UseAuthorization();

// 5. Map API controllers
app.MapControllers();

// 6. Map MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
