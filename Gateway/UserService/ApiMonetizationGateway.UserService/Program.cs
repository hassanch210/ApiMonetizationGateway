using Microsoft.EntityFrameworkCore;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.UserService.Services;
using StackExchange.Redis;
using ApiMonetizationGateway.Shared.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Monetization Gateway - User Service",
        Version = "v1",
        Description = "User management and authentication service for API Monetization Gateway",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Monetization Gateway",
            Email = "support@apigateway.com"
        }
    });

    // Add JWT Bearer authorization
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// Database configuration
builder.Services.AddDbContext<ApiMonetizationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ApiMonetizationGateway.UserService")));

// Business services
builder.Services.AddScoped<IUserService, ApiMonetizationGateway.UserService.Services.UserService>();
builder.Services.AddScoped<IJwtService, JwtService>();

// Redis configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});
builder.Services.AddSingleton<IRedisService, RedisService>();

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// JWT Authentication configuration
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Service API v1"); c.RoutePrefix = string.Empty; });
}

// Run database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();
    context.Database.EnsureCreated();

    // Seed demo user for testing
    try
    {
        var demoEmail = "demo.user@example.com";
        var demo = await context.Users.FirstOrDefaultAsync(u => u.Email == demoEmail);
        if (demo == null)
        {
            // Ensure a default tier (Free) exists from seeding
            var freeTier = await context.Tiers.FirstOrDefaultAsync(t => t.Name == "Free" && t.IsActive);
            if (freeTier != null)
            {
                var user = new ApiMonetizationGateway.Shared.Models.User
                {
                    Email = demoEmail,
                    FirstName = "Demo",
                    LastName = "User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Passw0rd!"),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                context.Users.Add(user);
                await context.SaveChangesAsync();

                context.UserTiers.Add(new ApiMonetizationGateway.Shared.Models.UserTier
                {
                    UserId = user.Id,
                    TierId = freeTier.Id,
                    AssignedAt = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow,
                    UpdatedBy = "Seeder",
                    IsActive = true,
                    Notes = "Seeded demo user tier"
                });

                await context.SaveChangesAsync();
            }
        }
    }
    catch { }
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
