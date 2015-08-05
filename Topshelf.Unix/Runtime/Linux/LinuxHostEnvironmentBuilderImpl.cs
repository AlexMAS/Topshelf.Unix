using System.Collections.Generic;

using Topshelf.Builders;
using Topshelf.HostConfigurators;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxHostEnvironmentBuilderImpl : EnvironmentBuilder
	{
		public LinuxHostEnvironmentBuilderImpl(HostConfigurator configurator, IDictionary<string, object> arguments)
		{
			_configurator = configurator;
			_arguments = arguments;
		}


		private readonly HostConfigurator _configurator;
		private readonly IDictionary<string, object> _arguments;


		public HostEnvironment Build()
		{
			return new LinuxHostEnvironmentImpl(_configurator, _arguments);
		}
	}
}