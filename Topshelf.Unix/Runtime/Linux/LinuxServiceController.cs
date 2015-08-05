using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;

using Mono.Unix.Native;

using Topshelf.Properties;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxServiceController
	{
		private LinuxServiceController(string serviceName)
		{
			ServiceName = serviceName;
		}


		public string ServiceName { get; private set; }


		public ServiceControllerStatus GetStatus()
		{
			var monoProcesses = LinuxProcess.GetProcessesByName("mono");

			foreach (var monoProcess in monoProcesses)
			{
				var arguments = monoProcess.CommandLine;

				if (!string.IsNullOrWhiteSpace(arguments))
				{
					var argumentValues = MonoHelper.ParseCommandLine(arguments);

					object name;
					argumentValues.TryGetValue("servicename", out name);

					object instance;
					argumentValues.TryGetValue("instance", out instance);

					if (!string.IsNullOrEmpty(name as string))
					{
						var serviceName = string.IsNullOrEmpty(instance as string) ? (string)name : string.Format("{0}${1}", name, instance);

						if (string.Equals(ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
						{
							return ServiceControllerStatus.Running;
						}
					}
				}
			}

			return ServiceControllerStatus.Stopped;
		}


		public void Start(TimeSpan timeout)
		{
			Start(timeout.Milliseconds);
		}

		public void Start(int timeout = Timeout.Infinite)
		{
			var result = MonoHelper.ExecuteShellCommand("service {0} start", timeout, ServiceName);

			if (result.ExitCode != 0)
			{
				throw new InvalidOperationException(string.Format(Resources.CantStartService, ServiceName, result.Completed ? result.Output : null));
			}
		}


		public void Stop(TimeSpan timeout)
		{
			Stop(timeout.Milliseconds);
		}

		public void Stop(int timeout = Timeout.Infinite)
		{
			var result = MonoHelper.ExecuteShellCommand("service {0} stop", timeout, ServiceName);

			if (result.ExitCode != 0)
			{
				throw new InvalidOperationException(string.Format(Resources.CantStopService, ServiceName, result.Completed ? result.Output : null));
			}
		}


		public static LinuxServiceController GetService(string serviceName)
		{
			return GetServices(serviceName).FirstOrDefault();
		}

		public static IEnumerable<LinuxServiceController> GetServices(string searchPattern = "*")
		{
			var files = Directory.GetFiles("/etc/init.d/", searchPattern, SearchOption.TopDirectoryOnly);

			foreach (var file in files)
			{
				Stat fileStatus;

				if (Syscall.stat(file, out fileStatus) == 0
					&& (fileStatus.st_mode.HasFlag(FilePermissions.S_IXUSR)
						|| fileStatus.st_mode.HasFlag(FilePermissions.S_IXGRP)
						|| fileStatus.st_mode.HasFlag(FilePermissions.S_IXOTH)))
				{
					yield return new LinuxServiceController(Path.GetFileName(file));
				}
			}
		}
	}
}