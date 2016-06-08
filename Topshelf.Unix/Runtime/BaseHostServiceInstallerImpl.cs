using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;

namespace Topshelf.Runtime
{
    internal abstract class BaseHostServiceInstallerImpl
    {
        public void InstallService(InstallHostSettings settings, IDictionary<string, object> arguments = null, Action<InstallEventArgs> beforeInstall = null, Action<InstallEventArgs> afterInstall = null, Action<InstallEventArgs> beforeRollback = null, Action<InstallEventArgs> afterRollback = null)
        {
            var commandLine = BuildCommandLine(settings, arguments);

            using (var installer = CreateInstaller(settings, commandLine))
            {
                if (beforeInstall != null)
                {
                    installer.BeforeInstall += (sender, args) => beforeInstall(args);
                }

                if (afterInstall != null)
                {
                    installer.AfterInstall += (sender, args) => afterInstall(args);
                }

                if (beforeRollback != null)
                {
                    installer.BeforeRollback += (sender, args) => beforeRollback(args);
                }

                if (afterRollback != null)
                {
                    installer.AfterRollback += (sender, args) => afterRollback(args);
                }

                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                installer.Install(new Hashtable());
            }
        }

        public void UninstallService(HostSettings settings, IDictionary<string, object> arguments = null, Action<InstallEventArgs> beforeUninstall = null, Action<InstallEventArgs> afterUninstall = null)
        {
            var commandLine = BuildCommandLine(settings, arguments);

            using (var installer = CreateUninstaller(settings, commandLine))
            {
                if (beforeUninstall != null)
                {
                    installer.BeforeUninstall += (sender, args) => beforeUninstall(args);
                }

                if (afterUninstall != null)
                {
                    installer.AfterUninstall += (sender, args) => afterUninstall(args);
                }

                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                installer.Uninstall(null);
            }
        }


        private static string BuildCommandLine(HostSettings settings, IDictionary<string, object> arguments)
        {
            arguments = (arguments == null)
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>(arguments);

            arguments["instance"] = settings.InstanceName;
            arguments["displayname"] = settings.DisplayName;
            arguments["servicename"] = settings.Name;

            return TopshelfHelper.BuildCommandLine(arguments);
        }


        protected abstract Installer CreateInstaller(InstallHostSettings settings, string commandLine);

        protected abstract Installer CreateUninstaller(HostSettings settings, string commandLine);
    }
}