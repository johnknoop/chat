using System;
using System.Threading.Tasks;

namespace Chat
{
	public class ConsoleController
	{
		private readonly string _currentUserName;

		public ConsoleController(string currentUserName)
		{
			_currentUserName = currentUserName;
		}

		public async Task<string> GetNextLine()
		{
			var message = await Console.In.ReadLineAsync();

			Console.SetCursorPosition(0, Console.CursorTop - 1);
			Console.WriteLine($"{_currentUserName.PadRight(10)}{message}");
			PrintPrompt();

			return message;
		}

		public void PrintTextMessage(string from, string text)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"{from.PadRight(10)}{text}");
			Console.ForegroundColor = ConsoleColor.White;

			PrintPrompt();
		}

		public void PrintServerMessage(string text)
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.WriteLine(text);
			Console.ForegroundColor = ConsoleColor.White;

			PrintPrompt();
		}

		public void Clear()
		{
			Console.Clear();
		}

		public void PrintPrompt()
		{
			Console.Write("> ");
		}
	}
}