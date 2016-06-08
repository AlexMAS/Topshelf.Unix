using System;
using System.Collections.Generic;
using System.Configuration.Install;

using Topshelf.HostConfigurators;

namespace Topshelf.Runtime.Windows
{
    internal sealed class WindowsHostEnvironmentImpl : HostEnvironment
    {
        public WindowsHostEnvironmentImpl(HostConfigurator configurator, IDictionary<string, object> arguments)
        {
            _environment = new WindowsHostEnvironment(configurator);
            _arguments = arguments;
        }


        private readonly IDictionary<string, object> _arguments;
        private readonly HostEnvironment _environment;


        public bool IsServiceInstalled(string serviceName)
        {
            return _environment.IsServiceInstalled(serviceName);
        }

        public bool IsServiceStopped(string serviceName)
        {
            return _environment.IsServiceStopped(serviceName);
        }

        public void StartService(string serviceName, TimeSpan startTimeOut)
        {
            _environment.StartService(serviceName, startTimeOut);
        }

        public void StopService(string serviceName, TimeSpan stopTimeOut)
        {
            _environment.StopService(serviceName, stopTimeOut);
        }

        public void InstallService(InstallHostSettings settings, Action<InstallHostSettings> beforeInstall, Action afterInstall, Action beforeRollback, Action afterRollback)
        {
            var installer = new WindowsHostServiceInstallerImpl();

            Action<InstallEventArgs> tryBeforeInstall = x => beforeInstall?.Invoke(settings);
            Action<InstallEventArgs> tryAfterInstall = x => afterInstall?.Invoke();
            Action<InstallEventArgs> tryBeforeRollback = x => beforeRollback?.Invoke();
            Action<InstallEventArgs> tryAfterRollback = x => afterRollback?.Invoke();

            installer.InstallService(settings, _arguments, tryBeforeInstall, tryAfterInstall, tryBeforeRollback, tryAfterRollback);
        }

        public void UninstallService(HostSettings settings, Action beforeUninstall, Action afterUninstall)
        {
            var installer = new WindowsHostServiceInstallerImpl();

            Action<InstallEventArgs> tryBeforeUninstall = x => beforeUninstall?.Invoke();
            Action<InstallEventArgs> tryAfterUninstall = x => afterUninstall?.Invoke();

            installer.UninstallService(settings, _arguments, tryBeforeUninstall, tryAfterUninstall);
        }

        public bool RunAsAdministrator()
        {
            return _environment.RunAsAdministrator();
        }

        public Host CreateServiceHost(HostSettings settings, ServiceHandle serviceHandle)
        {
            return _environment.CreateServiceHost(settings, serviceHandle);
        }

        public void SendServiceCommand(string serviceName, int command)
        {
            _environment.SendServiceCommand(serviceName, command);
        }

        public string CommandLine => _environment.CommandLine;

        public bool IsAdministrator => _environment.IsAdministrator;

        public bool IsRunningAsAService => _environment.IsRunningAsAService;
    }
}