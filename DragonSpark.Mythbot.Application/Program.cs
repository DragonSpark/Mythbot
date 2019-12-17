using DragonSpark.Application.Hosting.Server;
using DragonSpark.Compose;
using DragonSpark.Model.Commands;
using JetBrains.Annotations;
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
		public Configurator() : this(Start.An.Instance(ServiceConfiguration.Default)
		                                  .Then(DefaultServiceConfiguration.Default)) {}

		public Configurator(Action<IServiceCollection> services) : base(services) {}
	}

	sealed class ServiceConfiguration : Command<IServiceCollection>
	{
		public static ServiceConfiguration Default { get; } = new ServiceConfiguration();

		ServiceConfiguration() : base(x => x.AddSingleton(WeatherForecastService.Default)) {}
	}
}