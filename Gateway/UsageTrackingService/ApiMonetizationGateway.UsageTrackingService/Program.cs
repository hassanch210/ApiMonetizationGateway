using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using ApiMonetizationGateway.Shared.Data;
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

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Usage Tracking Service API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "API Monetization Gateway - Usage Tracking Service";
        c.DefaultModelsExpandDepth(-1);
        c.DefaultModelExpandDepth(2);
        c.DisplayRequestDuration();
    });
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

app.MapControllers();

app.Run();
