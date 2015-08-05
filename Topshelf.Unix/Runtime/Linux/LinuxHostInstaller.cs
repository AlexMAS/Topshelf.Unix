using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Mono.Unix.Native;

using Topshelf.Logging;
using Topshelf.Properties;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxHostInstaller : Installer
	{
		private const int Timeout = 60 * 1000;


		public LinuxHostInstaller(HostSettings settings, string arguments, Installer[] installers)
		{
			_settings = settings;
			_arguments = arguments;
			_installers = installers;
			_logWriter = HostLogger.Get<LinuxHostInstaller>();

			_installTransaction = new TransactionManager<HostSettings>(_logWriter.DebugFormat)
				.Stage(Resources.CreateServiceFileStage, CreateServiceFile, DeleteServiceFile)
				.Stage(Resources.SetServiceFileAsExecutableStage, SetServiceFileAsExecutable)
				.Stage(Resources.RegisterServiceFileStage, RegisterServiceFile, UnregisterServiceFile);
		}


		private readonly string _arguments;
		private readonly Installer[] _installers;
		private readonly HostSettings _settings;
		private readonly LogWriter _logWriter;
		private readonly TransactionManager<HostSettings> _installTransaction;


		public override void Install(IDictionary stateSaver)
		{
			if (_installers != null)
			{
				Installers.AddRange(_installers);
			}

			var serviceName = BuildServiceName(_settings);

			_logWriter.InfoFormat(Resources.InstallingServiceIsStarted, serviceName);

			try
			{
				base.Install(stateSaver);

				_installTransaction.Execute(_settings);

				_logWriter.InfoFormat(Resources.InstallingServiceIsSuccessfullyCompleted, serviceName);
			}
			catch (Exception error)
			{
				error = new InstallException(string.Format(Resources.InstallingServiceFailed, serviceName), error);
				_logWriter.ErrorFormat(Resources.InstallingServiceIsCompletedWithErrors, serviceName, error);
				throw error;
			}
		}

		public override void Uninstall(IDictionary savedState)
		{
			if (_installers != null)
			{
				Installers.AddRange(_installers);
			}

			var serviceName = BuildServiceName(_settings);

			_logWriter.InfoFormat(Resources.UninstallingServiceIsStarted, serviceName);

			var errors = new List<Exception>();

			try
			{
				_installTransaction.Rollback(_settings);
			}
			catch (Exception error)
			{
				errors.Add(error);
			}

			try
			{
				base.Uninstall(savedState);
			}
			catch (Exception error)
			{
				errors.Add(error);
			}

			if (errors.Count > 1)
			{
				Exception error = new AggregateException(string.Format(Resources.UninstallingServiceFailed, serviceName), errors);
				error = new InstallException(string.Format(Resources.UninstallingServiceFailed, serviceName), error);
				_logWriter.ErrorFormat(Resources.UninstallingServiceIsCompletedWithErrors, serviceName, error);
				throw error;
			}

			if (errors.Count == 1)
			{
				var error = new InstallException(string.Format(Resources.UninstallingServiceFailed, serviceName), errors[0]);
				_logWriter.ErrorFormat(Resources.UninstallingServiceIsCompletedWithErrors, serviceName, error);
				throw error;
			}

			_logWriter.InfoFormat(Resources.UninstallingServiceIsSuccessfullyCompleted, serviceName);
		}


		private void CreateServiceFile(HostSettings settings)
		{
			// Создание скрипта в '/etc/init.d'

			var serviceName = BuildServiceName(settings);
			var serviceFile = BuildServicePath(settings);
			var serviceScript = BuildServiceScript(settings, serviceName);

			File.WriteAllText(serviceFile, serviceScript);
		}

		private static void DeleteServiceFile(HostSettings settings)
		{
			// Удаление скрипта из '/etc/init.d'

			var serviceFile = BuildServicePath(settings);

			if (File.Exists(serviceFile))
			{
				File.Delete(serviceFile);
			}
		}

		private static void SetServiceFileAsExecutable(HostSettings settings)
		{
			// Выполнение команды: 'chmod +x <serviceFile>'

			var serviceFile = BuildServicePath(settings);

			Stat fileStatus;

			// Определение состояния файла
			if (Syscall.stat(serviceFile, out fileStatus) != 0)
			{
				throw new InstallException(string.Format(Resources.CantRetrieveFileStatus, serviceFile));
			}

			// Разрешение исполнения файла
			if (Syscall.chmod(serviceFile, fileStatus.st_mode | FilePermissions.S_IXUSR | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH) != 0)
			{
				throw new InstallException(string.Format(Resources.CantSetFileAsExecutable, serviceFile));
			}
		}

		private static void RegisterServiceFile(HostSettings settings)
		{
			// Выполнение команды: 'update-rc.d <serviceFile> defaults'

			var serviceFile = Path.GetFileName(BuildServicePath(settings));
			var commandResult = MonoHelper.ExecuteShellCommand("update-rc.d {0} defaults", Timeout, serviceFile);

			if (commandResult.ExitCode != 0)
			{
				throw new InstallException(string.Format(Resources.CantRegisterServiceFile, serviceFile, commandResult.Completed ? commandResult.Output : null));
			}
		}

		private static void UnregisterServiceFile(HostSettings settings)
		{
			// Выполнение команды: 'update-rc.d -f <serviceFile> remove'

			var serviceFile = Path.GetFileName(BuildServicePath(settings));
			var commandResult = MonoHelper.ExecuteShellCommand("update-rc.d -f {0} remove", Timeout, serviceFile);

			if (commandResult.ExitCode != 0)
			{
				throw new InstallException(string.Format(Resources.CantUnregisterServiceFile, serviceFile, commandResult.Completed ? commandResult.Output : null));
			}
		}


		private string BuildServiceScript(HostSettings settings, string serviceName)
		{
			var installSettings = settings as InstallHostSettings;

			if (installSettings == null)
			{
				throw new InstallException(Resources.InstallHostSettingsWereNotDefined);
			}

			var currentAssembly = Assembly.GetEntryAssembly();

			if (currentAssembly == null)
			{
				throw new InstallException(Resources.ServiceMustBeExecutableFile);
			}

			return new StringBuilder(Resources.LinuxServiceScript)
				.Replace("<ServiceName>", serviceName.Replace("$", @"\$"))
				.Replace("<Dependencies>", BuildDependencies(installSettings))
				.Replace("<DisplayName>", BuildDisplayName(installSettings))
				.Replace("<Description>", BuildDescription(installSettings))
				.Replace("<ServiceDir>", BuildServiceDirectory(currentAssembly))
				.Replace("<ServiceExe>", BuildServiceExecutable(currentAssembly))
				.Replace("<ServiceArgs>", BuildServiceArguments(_arguments))
				.Replace("<ServiceUser>", BuildServiceUser(installSettings))
				.Replace("<ServicePidDir>", "/var/run")
				.Replace("\r\n", "\n")
				.ToString();
		}

		private static string BuildServiceName(HostSettings settings)
		{
			return settings.ServiceName;
		}

		private static string BuildServicePath(HostSettings settings)
		{
			var serviceName = BuildServiceName(settings);
			return Path.Combine("/etc/init.d", serviceName);
		}

		private static string BuildDisplayName(HostSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.DisplayName)
				? BuildServiceName(settings) : settings.DisplayName;
		}

		private static string BuildDescription(HostSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.Description)
				? BuildDisplayName(settings) : settings.Description;
		}

		private static string BuildDependencies(InstallHostSettings settings)
		{
			var dependencies = settings.Dependencies;

			if (dependencies != null && dependencies.Length > 0)
			{
				dependencies = settings.Dependencies.Where(d => !string.IsNullOrWhiteSpace(d)).ToArray();
			}

			if (dependencies == null || dependencies.Length <= 0)
			{
				dependencies = new[] { "$local_fs", "$network", "$remote_fs", "$syslog" };
			}

			return string.Join(" ", dependencies);
		}

		public static string BuildServiceUser(InstallHostSettings settings)
		{
			return string.IsNullOrWhiteSpace(settings.Username) ? Environment.UserName : settings.Username.Trim();
		}

		public static string BuildServiceDirectory(Assembly currentAssembly)
		{
			return Path.GetDirectoryName(currentAssembly.Location);
		}

		public static string BuildServiceExecutable(Assembly currentAssembly)
		{
			return Path.GetFileName(currentAssembly.Location);
		}

		public static string BuildServiceArguments(string arguments)
		{
			return (string.IsNullOrWhiteSpace(arguments) ? string.Empty : arguments.Trim()).Replace(@"""", @"\""");
		}
	}
}