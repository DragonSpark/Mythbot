using DragonSpark.Application.Hosting.Server;
using DragonSpark.Model.Commands;
using DragonSpark.Model.Selection;
using DragonSpark.Model.Selection.Alterations;
using JetBrains.Annotations;
using LaunchDarkly.EventSource;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace DragonSpark.Mythbot.Environment
{
	[UsedImplicitly]
	public sealed class Program : ActivatedProgram<DevelopmentModeProgram>
	{
		public static Program Default { get; } = new Program();

		Program() {}
	}

	[UsedImplicitly]
	public sealed class ServiceConfiguration : Command<ConfigureParameter>, IServiceConfiguration
	{
		public static ServiceConfiguration Default { get; } = new ServiceConfiguration();

		ServiceConfiguration() : base(Configuration.Default.Then(DefaultServiceConfiguration.Default)) {}
	}

	sealed class Configuration : ICommand<ConfigureParameter>
	{
		public static Configuration Default { get; } = new Configuration();

		Configuration() : this(RegisterOption<RelayerSettings>.Default) {}

		readonly IAlteration<ConfigureParameter> _alteration;

		public Configuration(IAlteration<ConfigureParameter> alteration) => _alteration = alteration;

		public void Execute(ConfigureParameter parameter)
		{
			_alteration.Get(parameter).Services.AddSingleton<DevelopmentModeProgram>();
		}
	}

	public sealed class RelayerSettings
	{
		public Uri Location { get; set; }
	}

	public sealed class DevelopmentModeProgram : ISelect<IHost, Task>
	{
		readonly LaunchDarkly.EventSource.Configuration _configuration;

		public DevelopmentModeProgram(RelayerSettings settings)
			: this(new LaunchDarkly.EventSource.Configuration(settings.Location)) {}

		public DevelopmentModeProgram(LaunchDarkly.EventSource.Configuration configuration)
			=> _configuration = configuration;

		public async Task Get(IHost parameter)
		{
			var source = new EventSource(_configuration);
			/*source.MessageReceived += (sender, eventArgs) => { Debugger.Break(); };

			source.CommentReceived += (sender, eventArgs) => { Debugger.Break(); };*/
			await Task.WhenAll(source.StartAsync(), parameter.RunAsync());

			source.Close();
		}
	}
}