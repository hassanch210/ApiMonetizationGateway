using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using RabbitMQ.Client;
using ApiMonetizationGateway.Shared.Data;
using ApiMonetizationGateway.Shared.Services;
using ApiMonetizationGateway.Gateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot();

// Add other services
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Monetization Gateway",
        Version = "v1",
        Description = "Main API Gateway with JWT authentication and rate limiting",
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

// Redis configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(connectionString);
});
builder.Services.AddSingleton<IRedisService, RedisService>();

// RabbitMQ configuration
builder.Services.AddSingleton<IConnection>(provider =>
{
    var factory = new ConnectionFactory()
    {
        HostName = builder.Configuration.GetValue<string>("RabbitMQ:HostName") ?? "localhost",
        Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        UserName = builder.Configuration.GetValue<string>("RabbitMQ:UserName") ?? "guest",
        Password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest"
    };
    return factory.CreateConnection();
});
builder.Services.AddSingleton<IMessageQueueService, RabbitMQService>();

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

// JWT Authentication configuration (middleware-based, values from env/secret manager)
var jwtKey = Environment.GetEnvironmentVariable("JWT__KEY") ?? "ThisIsMySecretKeyForJWTTokenGenerationAndShouldBeAtLeast256Bits";
var jwtIssuer = Environment.GetEnvironmentVariable("JWT__ISSUER") ?? "ApiMonetizationGateway";
var jwtAudience = Environment.GetEnvironmentVariable("JWT__AUDIENCE") ?? "ApiMonetizationGatewayUsers";

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
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
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1"); c.RoutePrefix = string.Empty; });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// Add JWT authentication middleware
app.UseAuthentication();
app.UseAuthorization();

// Add rate limiting middleware before Ocelot
app.UseMiddleware<RateLimitingMiddleware>();

// Use Ocelot
await app.UseOcelot();

app.Run();
