using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;

using Topshelf.Properties;

namespace Topshelf.Runtime.Windows
{
    internal sealed class WindowsHostServiceInstallerImpl : BaseHostServiceInstallerImpl
    {
        protected override Installer CreateInstaller(InstallHostSettings settings, string commandLine)
        {
            var baseInstallers = new Installer[]
            {
                CreateServiceInstaller(settings, settings.Dependencies, settings.StartMode),
                CreateServiceProcessInstaller(settings.Credentials?.Account, settings.Credentials?.Username, settings.Credentials?.Password)
            };

            foreach (var installer in baseInstallers)
            {
                var eventLogInstallers = installer.Installers.OfType<EventLogInstaller>().ToArray();

                foreach (var eventLogInstaller in eventLogInstallers)
                {
                    installer.Installers.Remove(eventLogInstaller);
                }
            }

            var mainInstaller = new HostInstaller(settings, commandLine, baseInstallers);

            return CreateTransactedInstaller(mainInstaller);
        }

        protected override Installer CreateUninstaller(HostSettings settings, string commandLine)
        {
            var baseInstallers = new Installer[]
            {
                CreateServiceInstaller(settings, new string[] {}, HostStartMode.Automatic),
                CreateServiceProcessInstaller(null, null, null)
            };

            var mainInstaller = new HostInstaller(settings, commandLine, baseInstallers);

            return CreateTransactedInstaller(mainInstaller);
        }


        private static ServiceInstaller CreateServiceInstaller(HostSettings settings, string[] dependencies, HostStartMode startMode)
        {
            var installer = new ServiceInstaller
            {
                ServiceName = settings.ServiceName,
                Description = settings.Description,
                DisplayName = settings.DisplayName,
                ServicesDependedOn = dependencies
            };

            SetStartMode(installer, startMode);

            return installer;
        }

        private static ServiceProcessInstaller CreateServiceProcessInstaller(ServiceAccount? account, string username, string password)
        {
            var installer = new ServiceProcessInstaller
            {
                Account = account ?? ServiceAccount.LocalService,
                Username = username ?? string.Empty,
                Password = password ?? string.Empty
            };

            return installer;
        }

        private static TransactedInstaller CreateTransactedInstaller(Installer installer)
        {
            var transactedInstaller = new TransactedInstaller();

            transactedInstaller.Installers.Add(installer);

            var assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
            {
                throw new TopshelfException(Resources.ServiceMustBeExecutableFile);
            }

            var path = $"/assemblypath={assembly.Location}";
            var commandLine = new[] { path };

            var context = new InstallContext(null, commandLine);
            transactedInstaller.Context = context;

            return transactedInstaller;
        }

        private static void SetStartMode(ServiceInstaller installer, HostStartMode startMode)
        {
            switch (startMode)
            {
                case HostStartMode.Automatic:
                    installer.StartType = ServiceStartMode.Automatic;
                    break;

                case HostStartMode.Manual:
                    installer.StartType = ServiceStartMode.Manual;
                    break;

                case HostStartMode.Disabled:
                    installer.StartType = ServiceStartMode.Disabled;
                    break;

                case HostStartMode.AutomaticDelayed:
                    installer.StartType = ServiceStartMode.Automatic;
                    installer.DelayedAutoStart = true;
                    break;
            }
        }
    }
}