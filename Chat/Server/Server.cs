using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chat
{
	public class Server
	{
		private readonly List<RemoteClient> _connectedClients = new List<RemoteClient>();
		
		public static bool IsRunning()
		{
			var activeListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
			return activeListeners.Any(x => x.Port == Constants.ServerPort);
		}

		private async Task NotifyClientsAboutServerShutdown()
		{
			foreach (var connectedClient in _connectedClients)
			{
				await connectedClient.NotifyServerShutdown();
			}
		}

		public async Task Start(CancellationToken shutdown, IObservable<bool> onShutdownInitiated)
		{
			var server = new TcpListener(IPAddress.Loopback, Constants.ServerPort);

			server.Start();

			onShutdownInitiated.Subscribe(async x =>
			{
				Console.WriteLine("Shutting down...");
				await NotifyClientsAboutServerShutdown();
				server.Stop();
				Console.WriteLine("Bye bye");

				Environment.Exit(0);
			});

			Console.WriteLine("Server running...");

			while (!shutdown.IsCancellationRequested)
			{
				var client = server.AcceptTcpClient();

				var clientInput = new StreamReader(client.GetStream());
				var clientOutput = new StreamWriter(client.GetStream());
				var userName = clientInput.ReadLine();

				var remoteClient = new RemoteClient(
					output: clientOutput,
					input: clientInput,
					userName: userName,
					onInboundMessage: async message => await BroadcastMessageToAllBut(userName, userName, message),
					onDisconnect: async () =>
					{
						await BroadcastMessageToAllBut(userName, "Server", $"{userName} has disconnected");
						_connectedClients.RemoveAll(x => x.UserName == userName);
					});

				remoteClient.Hookup();

				Console.WriteLine($"{userName} connected.");
				
				await remoteClient.SendMessage("Server", $"Welcome, {userName}");
				await BroadcastMessageToAllBut(userName, "Server", $"{userName} joined the chatroom");

				_connectedClients.Add(remoteClient);
			}

			Console.WriteLine("Bye bye");
		}

		private async Task BroadcastMessageToAllBut(string userName, string from, string inbound)
		{
			Console.WriteLine($"{from}:{inbound}");

			foreach (var c in _connectedClients.Where(x => x.UserName != userName))
			{
				await c.SendMessage(from, inbound);
			}
		}
	}
}
