using System;
using System.Threading.Tasks;
using System.IO;
using Discord;
using Discord.WebSocket;

namespace TataruBot
{
    class TataruBot
    {
        private DiscordSocketClient _client;

        static void Main(string[] args) => 
            new TataruBot().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync() {
            Console.WriteLine("Running Async");

            var _configs = new DiscordSocketConfig { MessageCacheSize = 100 };
            _client = new DiscordSocketClient(_configs);
            string file_path = Path.Combine(Environment.CurrentDirectory, "CompletelySecure.txt");
            string token = File.ReadAllText(file_path);
            Console.WriteLine(token);

            // Events:
            _client.Connected += Ready;


            // Connect:
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Prevent connection from closing
            await Task.Delay(-1);
        }

        public Task Ready() {
            Console.Write("Connected");
            return Task.CompletedTask;
        }
    }

}
