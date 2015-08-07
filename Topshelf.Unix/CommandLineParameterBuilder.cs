using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Topshelf.HostConfigurators;

namespace Topshelf
{
	public sealed class CommandLineParameterBuilder
	{
		public CommandLineParameterBuilder(HostConfigurator configurator)
		{
			_configurator = configurator;
			_parameters = new Dictionary<string, object>();
		}


		private readonly HostConfigurator _configurator;
		private readonly Dictionary<string, object> _parameters;


		public CommandLineParameterBuilder AddStringParameter(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException("name");
			}

			_configurator.AddCommandLineDefinition(name, value =>
			{
				_parameters[name] = value;
			});

			return this;
		}

		public CommandLineParameterBuilder AddBooleanParameter(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentNullException("name");
			}

			_configurator.AddCommandLineSwitch(name, value =>
			{
				_parameters[name] = value;
			});

			return this;
		}

		public IDictionary<string, object> GeteParameterValues()
		{
			_parameters.Clear();

			if (MonoHelper.RunninOnLinux)
			{
				_configurator.ApplyCommandLine(TopshelfHelper.NormalizeCommandLine());
			}
			else
			{
				_configurator.ApplyCommandLine();
			}

			return new ReadOnlyDictionary<string, object>(_parameters);
		}
	}
}