using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HabitRPG.Api.Data;
using HabitRPG.Api.Services;
using HabitRPG.Api.Extensions;
using HabitRPG.Api.Middleware;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddValidation();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = (context) =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;

        if (!context.ProblemDetails.Extensions.ContainsKey("traceId"))
        {
            context.ProblemDetails.Extensions.Add("traceId", context.HttpContext.TraceIdentifier);
        }
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var environment = builder.Environment.EnvironmentName;

string connectionString;
if (environment == "Development")
{
    connectionString = Environment.GetEnvironmentVariable("DEV_DATABASE_CONNECTION");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("DEV_DATABASE_CONNECTION environment variable is required for development");
        
    Console.WriteLine("Using PostgreSQL database (Neon Dev)");
}
else if (environment == "Production")
{
    connectionString = Environment.GetEnvironmentVariable("PROD_DATABASE_CONNECTION");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("PROD_DATABASE_CONNECTION environment variable is required for production");
        
    Console.WriteLine("Using PostgreSQL database (Neon Prod)");
}
else
{
    connectionString = Environment.GetEnvironmentVariable("DEV_DATABASE_CONNECTION");
    if (string.IsNullOrEmpty(connectionString))
        throw new InvalidOperationException("DEV_DATABASE_CONNECTION environment variable is required for fallback");
        
    Console.WriteLine($"Using PostgreSQL database (Neon Dev) for environment: {environment}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(30);
    });
    
    if (environment == "Development")
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["JwtSettings:SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    if (environment == "Development")
    {
        Console.WriteLine("Warning: No JWT secret key found. Using development fallback.");
        secretKey = builder.Configuration["JwtSettings:SecretKey"];
    }
    else
    {
        throw new InvalidOperationException("JWT SecretKey is required for production");
    }
}

var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "HabitRPG.Api";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "HabitRPG.Mobile";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameService, GameService>();

builder.Services.AddRepositories();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactNative", policy =>
    {
        if (environment == "Development")
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins("https://www.habitrpg.zlima.dev")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseCors("AllowReactNative");
app.UseMiddleware<ValidationMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
});

app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        Console.WriteLine($"Starting {environment} database operations...");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var canConnect = await context.Database.CanConnectAsync(cts.Token);
        Console.WriteLine($"Can connect to database: {canConnect}");

        if (!canConnect)
        {
            throw new InvalidOperationException($"Cannot connect to {environment} database");
        }

        Console.WriteLine("Applying migrations to PostgreSQL database...");
        await context.Database.MigrateAsync(cts.Token);
        Console.WriteLine($"{environment} database migrations applied successfully");

        var userCount = await context.Users.CountAsync(cts.Token);
        Console.WriteLine($"Database verified. User count: {userCount}");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"{environment} database operation failed: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
        throw;
    }
}

Console.WriteLine("Starting web server...");
app.Run();