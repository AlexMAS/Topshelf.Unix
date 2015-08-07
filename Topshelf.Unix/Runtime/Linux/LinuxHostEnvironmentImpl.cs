using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.ServiceProcess;
using System.ServiceProcess.Linux;

using Topshelf.HostConfigurators;
using Topshelf.Logging;
using Topshelf.Properties;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxHostEnvironmentImpl : HostEnvironment
	{
		public LinuxHostEnvironmentImpl(HostConfigurator configurator, IDictionary<string, object> arguments)
		{
			_configurator = configurator;
			_arguments = arguments;
			_logWriter = HostLogger.Get<LinuxHostEnvironmentImpl>();
		}


		private readonly HostConfigurator _configurator;
		private readonly IDictionary<string, object> _arguments;
		private readonly LogWriter _logWriter;


		public bool IsServiceInstalled(string serviceName)
		{
			var service = LsbLinuxServiceController.GetService(serviceName);

			return (service != null);
		}

		public bool IsServiceStopped(string serviceName)
		{
			var service = LsbLinuxServiceController.GetService(serviceName);

			return (service == null || service.GetStatus() == ServiceControllerStatus.Stopped);
		}

		public void StartService(string serviceName, TimeSpan timeout)
		{
			var service = LsbLinuxServiceController.GetService(serviceName);

			if (service == null)
			{
				throw new InvalidOperationException(string.Format(Resources.ServiceHasntInstalled, serviceName));
			}

			try
			{
				service.Start(timeout);
			}
			catch (Exception error)
			{
				_logWriter.Error(error.Message, error);
			}
		}

		public void StopService(string serviceName, TimeSpan timeout)
		{
			var service = LsbLinuxServiceController.GetService(serviceName);

			if (service == null)
			{
				throw new InvalidOperationException(string.Format(Resources.ServiceHasntInstalled, serviceName));
			}

			try
			{
				service.Stop(timeout);
			}
			catch (Exception error)
			{
				_logWriter.Error(error.Message, error);
			}
		}

		public void InstallService(InstallHostSettings settings, Action beforeInstall, Action afterInstall, Action beforeRollback, Action afterRollback)
		{
			var installer = new LinuxHostServiceInstallerImpl();

			Action<InstallEventArgs> tryBeforeInstall = x =>
				{
					if (beforeInstall != null)
					{
						beforeInstall();
					}
				};

			Action<InstallEventArgs> tryAfterInstall = x =>
			{
				if (afterInstall != null)
				{
					afterInstall();
				}
			};

			Action<InstallEventArgs> tryBeforeRollback = x =>
			{
				if (beforeRollback != null)
				{
					beforeRollback();
				}
			};

			Action<InstallEventArgs> tryAfterRollback = x =>
			{
				if (afterRollback != null)
				{
					afterRollback();
				}
			};

			installer.InstallService(settings, _arguments, tryBeforeInstall, tryAfterInstall, tryBeforeRollback, tryAfterRollback);
		}

		public void UninstallService(HostSettings settings, Action beforeUninstall, Action afterUninstall)
		{
			var installer = new LinuxHostServiceInstallerImpl();

			Action<InstallEventArgs> tryBeforeUninstall = x =>
				{
					if (beforeUninstall != null)
					{
						beforeUninstall();
					}
				};

			Action<InstallEventArgs> tryAfterUninstall = x =>
			{
				if (afterUninstall != null)
				{
					afterUninstall();
				}
			};

			installer.UninstallService(settings, _arguments, tryBeforeUninstall, tryAfterUninstall);
		}

		public bool RunAsAdministrator()
		{
			throw new NotSupportedException();
		}

		public Host CreateServiceHost(HostSettings settings, ServiceHandle serviceHandle)
		{
			return new LinuxServiceHost(settings, serviceHandle);
		}

		public void SendServiceCommand(string serviceName, int command)
		{
			throw new NotSupportedException();
		}

		public string CommandLine
		{
			get { return TopshelfHelper.NormalizeCommandLine(); }
		}

		public bool IsAdministrator
		{
			get { return MonoHelper.RunningAsRoot; }
		}

		public bool IsRunningAsAService
		{
			get { return true; }
		}
	}
}