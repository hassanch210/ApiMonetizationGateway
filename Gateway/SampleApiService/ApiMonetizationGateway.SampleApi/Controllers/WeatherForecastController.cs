using Microsoft.AspNetCore.Mvc;

namespace ApiMonetizationGateway.SampleApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IEnumerable<WeatherForecast> Get()
    {
        _logger.LogInformation("Weather forecast requested at {Time}", DateTime.UtcNow);

        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("{days}")]
    public IEnumerable<WeatherForecast> Get(int days)
    {
        if (days <= 0 || days > 30)
        {
            throw new ArgumentException("Days must be between 1 and 30");
        }

        _logger.LogInformation("Weather forecast for {Days} days requested at {Time}", days, DateTime.UtcNow);

        return Enumerable.Range(1, days).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }

    [HttpGet("city/{city}")]
    public WeatherForecast GetByCity(string city)
    {
        _logger.LogInformation("Weather forecast for city {City} requested at {Time}", city, DateTime.UtcNow);

        return new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        };
    }

    [HttpPost]
    public IActionResult CreateForecast([FromBody] WeatherForecast forecast)
    {
        _logger.LogInformation("New weather forecast created for {Date}", forecast.Date);
        
        // Simulate saving the forecast
        return CreatedAtAction(nameof(GetByCity), new { city = "Custom" }, forecast);
    }
}

public class WeatherForecast
{
    public DateOnly Date { get; set; }

    public int TemperatureC { get; set; }

    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

    public string? Summary { get; set; }
}