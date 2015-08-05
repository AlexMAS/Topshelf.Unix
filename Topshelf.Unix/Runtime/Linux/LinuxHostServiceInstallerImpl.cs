using System.Configuration.Install;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxHostServiceInstallerImpl : BaseHostServiceInstallerImpl
	{
		protected override Installer CreateInstaller(InstallHostSettings settings, string commandLine)
		{
			return new LinuxHostInstaller(settings, commandLine, null);
		}

		protected override Installer CreateUninstaller(HostSettings settings, string commandLine)
		{
			return new LinuxHostInstaller(settings, commandLine, null);
		}
	}
}