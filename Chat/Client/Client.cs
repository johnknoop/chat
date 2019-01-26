using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat
{
	interface IInboundMessage { }

	class ServerShutdownMessage : IInboundMessage { }

	class TextMessage : IInboundMessage
	{
		public string User { get; }
		public string Text { get; }

		public TextMessage(string user, string text)
		{
			User = user;
			Text = text;
		}
	}

	public class Client
	{
		public async Task Start(CancellationToken shutdownRequested, IObservable<bool> sigterms)
		{
			Console.WriteLine("What is your name?");
			var name = await Console.In.ReadLineAsync();

			var consoleController = new ConsoleController(name);

			consoleController.Clear();

			using (var client = new TcpClient())
			{
				await client.ConnectAsync(IPAddress.Loopback, Constants.ServerPort);

				using (var networkReader = new StreamReader(client.GetStream(), Encoding.UTF8))
				using (var networkWriter = new StreamWriter(client.GetStream(), Encoding.UTF8))
				{
					// Vårt protokoll säger att första raden innehåller användarens namn
					await networkWriter.WriteLineAsync(name);
					await networkWriter.FlushAsync();

					Console.WriteLine($"Welcome to the chat room, {name}");
					consoleController.PrintPrompt();

					var networkStream = GetNetworkStream(networkReader);

					networkStream
						.ObserveOn(NewThreadScheduler.Default)
						.Subscribe(msg =>
						{
							Console.CursorLeft = 0;

							switch (msg)
							{
								case ServerShutdownMessage _:
									consoleController.PrintServerMessage("Server is shutting down. Bye bye");
									Environment.Exit(0);
									break;
								case TextMessage textMessage:
									consoleController.PrintTextMessage(textMessage.User, textMessage.Text);
									break;
							}
						});

					sigterms.Subscribe(async x =>
					{
						// Vi använder ascii 0 som kontrolltecken för att frånsluta
						await networkWriter.WriteLineAsync((char)0);
						await networkWriter.FlushAsync();

						Console.WriteLine("Bye bye");
						Environment.Exit(0);
					});

					while (!shutdownRequested.IsCancellationRequested)
					{
						var message = await consoleController.GetNextLine();
						await networkWriter.WriteLineAsync(message);
						await networkWriter.FlushAsync();
					}
				}
			}
		}

		private IObservable<IInboundMessage> GetNetworkStream(StreamReader networkReader)
		{
			return Observable.FromAsync(networkReader.ReadLineAsync).Repeat()
				.Select<string, IInboundMessage>(unparsedMessage =>
				{
					if (unparsedMessage.Length == 1 && unparsedMessage[0] == 0)
					{
						return new ServerShutdownMessage();
					}

					return new TextMessage(unparsedMessage.Substring(0, 10), unparsedMessage.Substring(10));
				});
		}
	}
}
