using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mono.Unix;
using Mono.Unix.Native;

namespace Topshelf.Runtime.Linux
{
	internal sealed class LinuxSignalListener : IDisposable
	{
		private static readonly TimeSpan ListenTimeout = TimeSpan.FromSeconds(60);


		public LinuxSignalListener()
		{
			_stopListen = new AutoResetEvent(false);
			_subscriptions = new Dictionary<Signum, List<Action>>();
		}


		private readonly AutoResetEvent _stopListen;
		private readonly Dictionary<Signum, List<Action>> _subscriptions;


		public void Subscribe(Signum signal, Action handler)
		{
			List<Action> subscribers;

			if (!_subscriptions.TryGetValue(signal, out subscribers))
			{
				subscribers = new List<Action>();

				_subscriptions.Add(signal, subscribers);
			}

			subscribers.Add(handler);
		}

		private void Handle(Signum signal)
		{
			List<Action> subscribers;

			if (_subscriptions.TryGetValue(signal, out subscribers))
			{
				foreach (var subscriber in subscribers)
				{
					try
					{
						subscriber();
					}
					catch
					{
					}
				}
			}
		}

		public Task Listen()
		{
			return Task.Run(() =>
			{
				var signals = _subscriptions.Select(i => new UnixSignal(i.Key)).ToArray();

				while (!_stopListen.WaitOne(0))
				{
					var signalIndex = UnixSignal.WaitAny(signals, ListenTimeout);

					if (signalIndex >= 0 && signalIndex < signals.Length)
					{
						var signal = signals[signalIndex];

						if (signal.IsSet)
						{
							Handle(signal.Signum);
						}

						if (signal.Signum == Signum.SIGINT || signal.Signum == Signum.SIGTERM)
						{
							break;
						}
					}
				}
			});
		}

		public void Dispose()
		{
			_stopListen.Set();
		}
	}
}