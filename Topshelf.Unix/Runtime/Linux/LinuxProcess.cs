using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxProcess
	{
		public LinuxProcess(Process process)
		{
			if (process == null)
			{
				throw new ArgumentNullException("process");
			}

			SetProcessInfo(process);
		}


		public int Id { get; private set; }

		public string ProcessName { get; private set; }

		public LinuxProcessState ProcessState { get; private set; }

		public string CommandLine { get; private set; }

		public Process ProcessObject { get; private set; }


		public static LinuxProcess Start(ProcessStartInfo startInfo)
		{
			var process = Process.Start(startInfo);

			return new LinuxProcess(process);
		}

		public static LinuxProcess Start(string fileName, string arguments = null)
		{
			var process = (arguments != null)
				? Process.Start(fileName, arguments)
				: Process.Start(fileName);

			return new LinuxProcess(process);
		}

		public void Kill()
		{
			ProcessObject.Kill();
		}


		public static LinuxProcess GetCurrentProcess()
		{
			var process = Process.GetCurrentProcess();
			return new LinuxProcess(process);
		}

		public static LinuxProcess GetProcessById(int processId)
		{
			var process = Process.GetProcessById(processId);
			return new LinuxProcess(process);
		}

		public static IEnumerable<LinuxProcess> GetProcessesByName(string processName)
		{
			return GetProcesses().Where(p => p.ProcessName == processName);
		}

		public static IEnumerable<LinuxProcess> GetProcesses()
		{
			foreach (var processDirectory in Directory.GetDirectories("/proc"))
			{
				int processId;
				var processDirectoryName = Path.GetFileName(processDirectory);

				if (int.TryParse(processDirectoryName, out processId))
				{
					Process process = null;

					try
					{
						process = Process.GetProcessById(processId);
					}
					catch
					{
					}

					if (process != null)
					{
						yield return new LinuxProcess(process);
					}
				}
			}
		}


		private void SetProcessInfo(Process process)
		{
			var processStatus = GetProcessStatus(process.Id);
			var processCommandLine = GetProcessCommandLine(process.Id);

			Id = process.Id;
			ProcessName = processStatus.Name;
			ProcessState = processStatus.State;
			CommandLine = processCommandLine;
			ProcessObject = process;
		}

		private static LinuxProcessStatus GetProcessStatus(int processId)
		{
			var processStatusFile = string.Format("/proc/{0}/stat", processId);

			try
			{
				if (File.Exists(processStatusFile))
				{
					using (var reader = File.OpenText(processStatusFile))
					{
						var statusText = reader.ReadToEnd();
						return ParseProcessStatus(statusText);
					}
				}
			}
			catch
			{
			}

			return default(LinuxProcessStatus);
		}

		private static string GetProcessCommandLine(int processId)
		{
			var processCommandLineFile = string.Format("/proc/{0}/cmdline", processId);

			try
			{
				if (File.Exists(processCommandLineFile))
				{
					using (var reader = File.OpenText(processCommandLineFile))
					{
						var commandLineText = reader.ReadToEnd();
						return ParseCommandLine(commandLineText);
					}
				}
			}
			catch
			{
			}

			return string.Empty;
		}

		private static LinuxProcessStatus ParseProcessStatus(string statusText)
		{
			var result = new LinuxProcessStatus { State = LinuxProcessState.Unknown };

			if (!string.IsNullOrEmpty(statusText))
			{
				var items = statusText.Split(new[] { ' ' }, StringSplitOptions.None);

				if (items.Length > 0)
				{
					int.TryParse(items[0], out result.Id);

					if (items.Length > 1)
					{
						result.Name = items[1].TrimStart('(').TrimEnd(')');

						if (items.Length > 2 && items[2].Length > 0)
						{
							switch (char.ToUpper(items[2][0]))
							{
								case 'R':
									result.State = LinuxProcessState.Running;
									break;
								case 'S':
									result.State = LinuxProcessState.InterruptableWait;
									break;
								case 'D':
									result.State = LinuxProcessState.UninterruptableDiskWait;
									break;
								case 'Z':
									result.State = LinuxProcessState.Zombie;
									break;
								case 'T':
									result.State = LinuxProcessState.Traced;
									break;
								case 'W':
									result.State = LinuxProcessState.Paging;
									break;
							}
						}
					}
				}
			}

			return result;
		}

		private static string ParseCommandLine(string commandLineText)
		{
			if (!string.IsNullOrEmpty(commandLineText))
			{
				var arguments = commandLineText.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
				return string.Join(" ", arguments);
			}

			return string.Empty;
		}


		internal struct LinuxProcessStatus
		{
			public int Id;
			public string Name;
			public LinuxProcessState State;
		}
	}
}