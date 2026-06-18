using Microsoft.EntityFrameworkCore;
using ProjectMemberService.Data;
using ProjectMemberService.Services;

var builder = WebApplication.CreateBuilder(args);

// Fix Npgsql DateTime: treat Unspecified as UTC (required for PostgreSQL)
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// ===== EF Core - PostgreSQL Database =====
builder.Services.AddDbContext<ProjectDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)
    ));

// ===== Dependency Injection =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<ISprintService, SprintService>();
builder.Services.AddScoped<IMilestoneService, MilestoneService>();

// ===== Controllers =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });



// ===== Built-in OpenAPI (.NET 10) =====
builder.Services.AddOpenApi();

// ===== CORS =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ===== Startup: DB init + Seed =====
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        // Reset DB nếu được yêu cầu (kể cả Production để test dễ dàng)
        if (Environment.GetEnvironmentVariable("RESET_DB") == "true")
        {
            logger.LogWarning("RESET_DB=true — Đang xóa và tạo lại database...");
            dbContext.Database.EnsureDeleted();
        }

        dbContext.Database.EnsureCreated();
        logger.LogInformation("Database connection established successfully.");

        // Seed data
        ProjectMemberService.Data.DataSeeder.Seed(dbContext);


    }
    catch (Exception ex)
    {
        logger.LogWarning("Startup error: {Message}. The app will continue running.", ex.Message);
    }
}

// ===== Middleware Pipeline =====
app.MapOpenApi();

// Swagger UI với JWT Bearer support
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Project & Member Service API v1");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "N1 - Project & Member Service API";
    options.InjectStylesheet("/swagger-custom.css");
});

app.UseCors("AllowAll");



app.MapControllers();

// Redirect root to swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
