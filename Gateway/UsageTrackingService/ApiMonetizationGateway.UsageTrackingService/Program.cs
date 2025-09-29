using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using ApiMonetizationGateway.Shared.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiMonetizationGateway.Shared.Services;
using ApiMonetizationGateway.UsageTrackingService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Monetization Gateway - Usage Tracking Service",
        Version = "v1",
        Description = "Usage tracking and analytics service for API Monetization Gateway",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "API Monetization Gateway",
            Email = "support@apigateway.com"
        }
    });

    // Simplified Swagger; removed auth decorations
});

// Database configuration
builder.Services.AddDbContext<ApiMonetizationContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ApiMonetizationGateway.UserService")));

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

// Business services
builder.Services.AddScoped<IUsageTrackingService, ApiMonetizationGateway.UsageTrackingService.Services.UsageTrackingService>();

// Background services
builder.Services.AddHostedService<UsageTrackingConsumerService>();

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
    app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Usage Tracking Service API v1"); c.RoutePrefix = string.Empty; });
}

// Run database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApiMonetizationContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
