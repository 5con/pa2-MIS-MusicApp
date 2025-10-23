using FreelanceMusicPlatform.Services;
using FreelanceMusicPlatform.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Use PascalCase
    });
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });

    options.AddPolicy("Development", builder =>
    {
        builder.WithOrigins(
                "http://localhost:8000",
                "https://localhost:8000",
                "http://127.0.0.1:8000",
                "https://127.0.0.1:8000",
                "http://localhost:3000",
                "http://localhost:8080",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:8080")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Register services
builder.Services.AddSingleton<FreelanceMusicPlatform.Services.DatabaseService>();
builder.Services.AddScoped<FreelanceMusicPlatform.Services.AuthenticationService>();
builder.Services.AddScoped<FreelanceMusicPlatform.Services.DummyDataService>();
builder.Services.AddScoped<FreelanceMusicPlatform.Services.LoggingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Add custom middleware (order matters!)
app.UseCorrelationId();
app.UseExceptionHandling();

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors();
app.UseAuthorization();

// Ensure uploads directory exists
var uploadsPath = System.IO.Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!System.IO.Directory.Exists(uploadsPath))
{
    System.IO.Directory.CreateDirectory(uploadsPath);
}

// Serve static files from uploads directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// API routes
app.MapControllers();

app.Run();
