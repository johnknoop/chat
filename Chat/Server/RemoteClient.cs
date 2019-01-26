using System;
using System.IO;
using System.Threading.Tasks;

namespace Chat
{
	public class RemoteClient
	{
		private readonly StreamWriter _output;
		private readonly StreamReader _input;
		private readonly Action<string> _onInboundMessage;
		private readonly Action _onDisconnect;
		public string UserName { get; private set; }

		public RemoteClient(StreamWriter output, StreamReader input, string userName, Action<string> onInboundMessage, Action onDisconnect)
		{
			_output = output;
			_input = input;
			_onInboundMessage = onInboundMessage;
			_onDisconnect = onDisconnect;
			UserName = userName;
		}

		public void Hookup()
		{
			Task.Run(async () =>
			{
				while (true)
				{
					var message = await _input.ReadLineAsync();

					// Vi använder ascii 0 som kontrolltecken för att frånsluta
					if (message.Length == 1 && message[0] == 0)
					{
						_onDisconnect();
					}
					else
					{
						_onInboundMessage(message);
					}
				}
			});
		}

		public async Task SendMessage(string from, string text)
		{
			await _output.WriteLineAsync($"{from.PadRight(10)}{text}");
			await _output.FlushAsync();
		}

		public async Task NotifyServerShutdown()
		{
			await _output.WriteLineAsync((char) 0);
			await _output.FlushAsync();
		}
	}
}