using DragonSpark.Compose;
using DragonSpark.Model.Selection;
using DragonSpark.Model.Sequences;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DragonSpark.Mythbot
{
	public interface IWeatherForecastService
	{
		Task<WeatherForecast[]> GetForecast(DateTime startDate);
	}

	public sealed class WeatherForecastService : IWeatherForecastService
	{
		public static IWeatherForecastService Default { get; } = new WeatherForecastService();

		WeatherForecastService() : this(WeatherForecastElement.Default.Get, Task.FromResult) {}

		readonly Func<(int, DateTime), WeatherForecast>           _select;
		readonly Func<WeatherForecast[], Task<WeatherForecast[]>> _task;

		public WeatherForecastService(Func<(int, DateTime), WeatherForecast> select,
		                              Func<WeatherForecast[], Task<WeatherForecast[]>> task)
		{
			_select = @select;
			_task   = task;
		}

		public Task<WeatherForecast[]> GetForecast(DateTime startDate)
			=> Enumerable.Range(1, 5)
			             .Introduce(startDate)
			             .Select(_select)
			             .ToArray()
			             .To(_task);
	}

	public interface IWeatherForecastElement : ISelect<(int, DateTime), WeatherForecast> {}

	sealed class WeatherForecastElement : IWeatherForecastElement
	{
		public static WeatherForecastElement Default { get; } = new WeatherForecastElement();

		WeatherForecastElement()
			: this(An.Array("Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering",
			                "Scorching")) {}

		readonly Array<string> _summaries;
		readonly uint          _length;
		readonly Random        _random;

		public WeatherForecastElement(Array<string> summaries) : this(summaries, summaries.Length, new Random()) {}

		public WeatherForecastElement(Array<string> summaries, uint length, Random random)
		{
			_summaries = summaries;
			_length    = length;
			_random    = random;
		}

		public WeatherForecast Get((int, DateTime) parameter) => new WeatherForecast
		{
			Date         = parameter.Item2.AddDays(parameter.Item1),
			TemperatureC = _random.Next(-20, 55),
			Summary      = _summaries[_random.Next((int)_length)]
		};
	}

	public sealed class WeatherForecast
	{
		public DateTime Date { get; set; }

		public int TemperatureC { get; set; }

		public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

		public string Summary { get; set; }
	}
}