using System;
using System.Collections.Generic;

using Topshelf.HostConfigurators;

using Topshelf.Runtime.Linux;
using Topshelf.Runtime.Windows;

namespace Topshelf
{
	public static class EnvironmentExtensions
	{
		public static IDictionary<string, object> SelectPlatform(this HostConfigurator configurator, Action<CommandLineParameterBuilder> customParameters = null)
		{
			IDictionary<string, object> parameterValues = null;

			if (customParameters != null)
			{
				var parameterBuilder = new CommandLineParameterBuilder(configurator);
				customParameters(parameterBuilder);

				parameterValues = parameterBuilder.GeteParameterValues();
			}

			if (MonoHelper.RunninOnLinux)
			{
				configurator.UseEnvironmentBuilder(c => new LinuxHostEnvironmentBuilderImpl(c, parameterValues));
			}
			else
			{
				configurator.UseEnvironmentBuilder(c => new WindowsHostEnvironmentBuilderImpl(c, parameterValues));
			}

			return parameterValues;
		}
	}
}