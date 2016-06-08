using System.Collections.Generic;

using Topshelf.Builders;
using Topshelf.HostConfigurators;

namespace Topshelf.Runtime.Windows
{
    internal sealed class WindowsHostEnvironmentBuilderImpl : EnvironmentBuilder
    {
        public WindowsHostEnvironmentBuilderImpl(HostConfigurator configurator, IDictionary<string, object> arguments)
        {
            _configurator = configurator;
            _arguments = arguments;
        }


        private readonly HostConfigurator _configurator;
        private readonly IDictionary<string, object> _arguments;


        public HostEnvironment Build()
        {
            return new WindowsHostEnvironmentImpl(_configurator, _arguments);
        }
    }
}