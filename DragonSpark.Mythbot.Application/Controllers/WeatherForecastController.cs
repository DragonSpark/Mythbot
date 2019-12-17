using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace DragonSpark.Mythbot.Application.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class WeatherForecastController : ControllerBase
	{
		readonly IWeatherForecastService            _weather;
		readonly ILogger<WeatherForecastController> _logger;

		public WeatherForecastController(IWeatherForecastService weather, ILogger<WeatherForecastController> logger)
		{
			_weather = weather;
			_logger  = logger;
		}

		[HttpGet]
		public async Task<WeatherForecast[]> Get()
		{
			var result = await _weather.GetForecast(DateTime.Now);
			_logger.Log(LogLevel.Debug, "Served {Number} forecasts.", result.Length.ToString());
			return result;
		}
	}
}