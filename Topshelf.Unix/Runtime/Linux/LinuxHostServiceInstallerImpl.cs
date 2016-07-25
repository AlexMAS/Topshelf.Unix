using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess.Linux;

using Topshelf.Logging;
using Topshelf.Properties;

namespace Topshelf.Runtime.Linux
{
    internal sealed class LinuxHostServiceInstallerImpl : BaseHostServiceInstallerImpl
    {
        protected override Installer CreateInstaller(InstallHostSettings settings, string commandLine)
        {
            return new LsbLinuxHostInstaller(CreateServiceSettings(settings, commandLine), null, CreateServiceLogWriter());
        }

        protected override Installer CreateUninstaller(HostSettings settings, string commandLine)
        {
            return new LsbLinuxHostInstaller(CreateServiceSettings(settings, commandLine), null, CreateServiceLogWriter());
        }

        private static LinuxServiceSettings CreateServiceSettings(HostSettings settings, string commandLine)
        {
            var currentAssembly = Assembly.GetEntryAssembly();

            if (currentAssembly == null)
            {
                throw new InstallException(Resources.ServiceMustBeExecutableFile);
            }

            var serviceName = settings.Name;

            if (!string.IsNullOrEmpty(settings.InstanceName))
            {
                serviceName += "@" + settings.InstanceName;
            }

            var result = new LinuxServiceSettings
            {
                ServiceName = serviceName,
                DisplayName = settings.DisplayName,
                Description = settings.Description,
                ServiceExe = currentAssembly.Location,
                ServiceArgs = commandLine
            };

            var installSettings = settings as InstallHostSettings;

            if (installSettings != null)
            {
                result.Username = installSettings.Credentials?.Username;
                result.Dependencies = installSettings.Dependencies;
            }

            return result;
        }

        private static LinuxServiceLogWriter CreateServiceLogWriter()
        {
            var topshelfLogWriter = HostLogger.Get<LsbLinuxHostInstaller>();

            return new LinuxServiceLogWriter(
                topshelfLogWriter.DebugFormat,
                topshelfLogWriter.InfoFormat,
                topshelfLogWriter.ErrorFormat,
                topshelfLogWriter.FatalFormat);
        }
    }
}