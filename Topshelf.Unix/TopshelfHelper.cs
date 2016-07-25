using System;
using System.Collections.Generic;
using System.Diagnostics.Linux;
using System.Linq;
using System.ServiceProcess;
using System.ServiceProcess.Linux;
using System.Text;
using System.Text.RegularExpressions;

namespace Topshelf
{
    internal static class TopshelfHelper
    {
        public static ServiceControllerStatus GetStatus(this LsbLinuxServiceController target)
        {
            var monoProcesses = LinuxProcess.GetProcessesByName("mono");

            foreach (var monoProcess in monoProcesses)
            {
                var arguments = monoProcess.CommandLine;

                if (!string.IsNullOrWhiteSpace(arguments))
                {
                    var argumentValues = ParseCommandLine(arguments);

                    object name;
                    argumentValues.TryGetValue("servicename", out name);

                    object instance;
                    argumentValues.TryGetValue("instance", out instance);

                    if (!string.IsNullOrEmpty(name as string))
                    {
                        var serviceName = string.IsNullOrEmpty(instance as string) ? (string)name : $"{name}@{instance}";

                        if (string.Equals(target.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            return ServiceControllerStatus.Running;
                        }
                    }
                }
            }

            return ServiceControllerStatus.Stopped;
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
            var commandLine = new StringBuilder(" ");

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


        public static string NormalizeCommandLine()
        {
            var commandLineParams = ParseCommandLine(Environment.CommandLine);

            return BuildCommandLine(commandLineParams);
        }
    }
}