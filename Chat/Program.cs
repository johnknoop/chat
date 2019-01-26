using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chat
{
	class Program
	{
		static async Task Main(string[] args)
		{
			var gracefulShutdown = new CancellationTokenSource();

			var sigterms = Observable.Create<bool>(observer =>
					{
						Console.CancelKeyPress += (sender, eventArgs) =>
						{
							observer.OnNext(false);
							eventArgs.Cancel = true;
						};
						return gracefulShutdown;
					})
				.Do(x => gracefulShutdown.Cancel());

			if (!Server.IsRunning())
			{
				await StartAsServer(gracefulShutdown.Token, sigterms);
			} else
			{
				await StartAsClient(gracefulShutdown.Token, sigterms);
			}
		}

		private static async Task StartAsClient(CancellationToken gracefulShutdown, IObservable<bool> sigterms)
		{
			var client = new Client();

			await client.Start(gracefulShutdown, sigterms);
		}

		private static async Task StartAsServer(CancellationToken gracefulShutdown, IObservable<bool> sigterms)
		{
			var server = new Server();

			await server.Start(gracefulShutdown, sigterms);
		}
	}
}
