namespace Topshelf.Runtime.Linux
{
	internal enum LinuxProcessState
	{
		Unknown,

		/// <summary>
		/// R - running or runnable (on run queue).
		/// </summary>
		Running,

		/// <summary>
		/// S - interruptible sleep (waiting for an event to complete).
		/// </summary>
		InterruptableWait,

		/// <summary>
		/// D - uninterruptible sleep (usually IO).
		/// </summary>
		UninterruptableDiskWait,

		/// <summary>
		/// Z - defunct ("zombie") process, terminated but not reaped by its parent.
		/// </summary>
		Zombie,

		/// <summary>
		/// T - stopped, either by a job control signal or because it is being traced.
		/// </summary>
		Traced,

		/// <summary>
		/// W - paging (not valid since the 2.6.xx kernel).
		/// </summary>
		Paging
	}
}