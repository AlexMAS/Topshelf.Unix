using System;
using System.IO;
using System.Threading;
using System.Threading.Linux;
using System.Threading.Tasks;

using Mono.Unix.Native;

using Topshelf.Logging;
using Topshelf.Properties;

namespace Topshelf.Runtime.Linux
{
    internal sealed class LinuxServiceHost : Host, HostControl, IDisposable
    {
        public LinuxServiceHost(HostSettings settings, ServiceHandle serviceHandle)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            if (serviceHandle == null)
            {
                throw new ArgumentNullException(nameof(serviceHandle));
            }

            _settings = settings;
            _serviceHandle = serviceHandle;

            _logWriter = HostLogger.Get<LinuxServiceHost>();
            _stopSignal = new ManualResetEvent(false);
            _signalListener = new LinuxSignalListener();
            _signalListener.Subscribe(Signum.SIGINT, SetStopSignal);
            _signalListener.Subscribe(Signum.SIGTERM, SetStopSignal);
        }


        private readonly HostSettings _settings;
        private readonly ServiceHandle _serviceHandle;
        private readonly LogWriter _logWriter;
        private readonly ManualResetEvent _stopSignal;
        private readonly LinuxSignalListener _signalListener;


        private TopshelfExitCode _exitCode;

        public TopshelfExitCode Run()
        {
            AppDomain.CurrentDomain.UnhandledException += CatchUnhandledException;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            _exitCode = TopshelfExitCode.Ok;

            _signalListener.Listen();

            var started = false;

            try
            {
                StartService();
                started = true;

                WaitStopSignal();
            }
            catch (Exception error)
            {
                _logWriter.Fatal(error);

                _exitCode = TopshelfExitCode.AbnormalExit;
            }
            finally
            {
                if (started)
                {
                    StopService();
                }

                HostLogger.Shutdown();
            }

            return _exitCode;
        }


        void HostControl.RequestAdditionalTime(TimeSpan timeRemaining)
        {
            throw new NotSupportedException();
        }

        void HostControl.Restart()
        {
            throw new NotSupportedException();
        }

        void HostControl.Stop()
        {
            _logWriter.DebugFormat(Resources.ServiceStopRequested, _settings.ServiceName);

            SetStopSignal();
        }


        private void StartService()
        {
            _logWriter.InfoFormat(Resources.StartingService, _settings.ServiceName);

            try
            {
                if (!_serviceHandle.Start(this))
                {
                    throw new TopshelfException(string.Format(Resources.ServiceDidntStartSuccessfully, _settings.ServiceName));
                }
            }
            catch (Exception error)
            {
                _logWriter.Fatal(string.Format(Resources.StartServiceFailed, _settings.ServiceName), error);

                throw;
            }

            _logWriter.InfoFormat(Resources.ServiceStarted, _settings.ServiceName);
        }

        private void StopService()
        {
            _logWriter.InfoFormat(Resources.StoppingService, _settings.ServiceName);

            try
            {
                if (!_serviceHandle.Stop(this))
                {
                    throw new TopshelfException(string.Format(Resources.ServiceDidntStopSuccessfully, _settings.ServiceName));
                }
            }
            catch (Exception error)
            {
                _logWriter.Fatal(string.Format(Resources.StopServiceFailed, _settings.ServiceName), error);
            }

            _logWriter.InfoFormat(Resources.ServiceStopped, _settings.ServiceName);
        }


        private void WaitStopSignal()
        {
            _stopSignal.WaitOne();
        }

        private void SetStopSignal()
        {
            _stopSignal.Set();
        }


        private int _deadThread;

        private void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logWriter.Fatal(string.Format(Resources.UnhandledException, _settings.ServiceName), (Exception)e.ExceptionObject);

            HostLogger.Shutdown();

            if (e.IsTerminating)
            {
                _exitCode = TopshelfExitCode.UnhandledServiceException;

                SetStopSignal();

                // Author Topshelf: it isn't likely that a TPL thread should land here, but if it does let's no block it
                if (!Task.CurrentId.HasValue)
                {
                    // Author Topshelf: this is evil, but perhaps a good thing to let us clean up properly

                    var deadThreadId = Interlocked.Increment(ref _deadThread);

                    Thread.CurrentThread.IsBackground = true;
                    Thread.CurrentThread.Name = "Unhandled Exception " + deadThreadId;

                    while (true)
                    {
                        Thread.Sleep(TimeSpan.FromHours(1));
                    }
                }
            }
        }


        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _signalListener.Dispose();
                _serviceHandle?.Dispose();

                _stopSignal.Close();
                _stopSignal.Dispose();

                _disposed = true;
            }
        }
    }
}