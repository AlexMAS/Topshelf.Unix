using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Mono.Unix.Native;

namespace Topshelf
{
	internal static class MonoHelper
	{
		public static bool RunningAsRoot
		{
			get
			{
				// Root ID is 0
				return Syscall.getuid() == 0;
			}
		}

		public static bool RunninOnUnix
		{
			get
			{
				var p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 6) || (p == 128));
			}
		}

		public static bool RunninOnLinux
		{
			get
			{
				var p = (int)Environment.OSVersion.Platform;
				return ((p == 4) || (p == 128));
			}
		}

		public static bool RunningOnMono
		{
			get
			{
				return (Type.GetType("Mono.Runtime") != null);
			}
		}

		public static string NormalizeCommandLine()
		{
			var commandLineParams = ParseCommandLine(Environment.CommandLine);

			return BuildCommandLine(commandLineParams);
		}

		public static IDictionary<string, object> ParseCommandLine(string commandLine)
		{
			var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			if (!string.IsNullOrWhiteSpace(commandLine))
			{
				var commandLineArgs = Regex.Matches(commandLine, @"(""[^""]*"")|([^\s]+)", RegexOptions.Compiled).Cast<Match>().Skip(1);

				var paramName = "";
				var isPramValue = false;

				foreach (var match in commandLineArgs)
				{
					var argument = match.Value;

					// Parameter Values
					if (isPramValue)
					{
						result[paramName] = argument.Trim('"');
						isPramValue = false;
					}
					// Parameter Names
					else if (argument.Length >= 2 && argument[0] == '-' && argument[1] != '-')
					{
						paramName = argument.Substring(1);
						isPramValue = true;
					}
					// Switches
					else if (argument.Length >= 3 && argument[0] == '-' && argument[1] == '-' && argument[2] != '-')
					{
						paramName = argument.Substring(2);
						result[paramName] = true;
					}
					// Verbs
					else
					{
						paramName = argument;
						result[paramName] = null;
					}
				}
			}

			return result;
		}

		public static string BuildCommandLine(IDictionary<string, object> commandLineParams)
		{
			var commandLine = new StringBuilder();

			if (commandLineParams != null)
			{
				foreach (var param in commandLineParams)
				{
					var key = param.Key;
					var value = param.Value;

					// Verbs
					if (value == null)
					{
						commandLine.AppendFormat("{0} ", key);
					}
					// Switches
					else if (value is bool)
					{
						if ((bool)value)
						{
							commandLine.AppendFormat("--{0} ", key);
						}
					}
					// Parameters
					else
					{
						var valueString = value.ToString();

						if (!string.IsNullOrEmpty(valueString))
						{
							commandLine.AppendFormat("-{0} \"{1}\" ", key, valueString);
						}
					}
				}

				if (commandLine.Length > 0)
				{
					commandLine.Remove(commandLine.Length - 1, 1);
				}
			}

			return commandLine.ToString();
		}

		public static ProcessResult ExecuteShellCommand(string command, int timeout, params object[] arguments)
		{
			if (string.IsNullOrWhiteSpace(command))
			{
				throw new ArgumentNullException("command");
			}

			var result = new ProcessResult();

			command = string.Format(command, arguments).Trim();

			using (var shellProcess = new Process())
			{
				shellProcess.StartInfo.FileName = "sh";
				shellProcess.StartInfo.Arguments = string.Format("-c '{0}'", command);
				shellProcess.StartInfo.UseShellExecute = false;
				shellProcess.StartInfo.RedirectStandardOutput = true;
				shellProcess.Start();

				if (shellProcess.WaitForExit(timeout))
				{
					result.Completed = true;
					result.ExitCode = shellProcess.ExitCode;
					result.Output = shellProcess.StandardOutput.ReadToEnd();
				}
			}

			return result;
		}

		public struct ProcessResult
		{
			public bool Completed;
			public int? ExitCode;
			public string Output;
		}
	}
}