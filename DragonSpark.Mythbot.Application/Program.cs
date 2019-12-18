using DragonSpark.Application.Hosting.Server;
using DragonSpark.Model.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DragonSpark.Mythbot.Application
{
	sealed class Program
	{
		static Task Main(string[] args) => Build.A.Program.By.Environment()
		                                        .ConfiguredBy<Configurator>()
		                                        .Get(args);
	}

	sealed class Configurator : DragonSpark.Application.Hosting.Server.Configurator
	{
		[UsedImplicitly]
		public Configurator(IConfiguration configuration)
			: this(configuration, Registrations.Default.Then(ServiceConfiguration.Default)) {}

		public Configurator(IConfiguration configuration, Action<ConfigureParameter> services)
			: base(configuration, services) {}
	}

	sealed class Registrations : Command<ConfigureParameter>
	{
		public static Registrations Default { get; } = new Registrations();

		Registrations() : base(x => x.Services.AddSingleton(WeatherForecastService.Default)) {}
	}
}