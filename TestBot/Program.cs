using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace TataruBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private DiscordSocketConfig _configs;
        public static void Main(string[] args)
		=> new Program().MainAsync().GetAwaiter().GetResult();

	    public async Task MainAsync()
	    {
            Console.WriteLine("Loading");

            _configs = new DiscordSocketConfig();
            _configs.MessageCacheSize = 1000;

            _client = new DiscordSocketClient(_configs);
            _client.Log += Log;
            _client.MessageReceived += MessageReceived;
            _client.MessageUpdated += MessageUpdated;

            await _client.LoginAsync(TokenType.Bot, "");
            await _client.StartAsync();

            await Task.Delay(-1);   
	    }

        public async Task MessageReceived(SocketMessage msg){
            if (msg.Content == "Test") {
                string text = "Recieved";
                await msg.Channel.SendMessageAsync(text);
            }
            if (msg.Content == "LogOff") {
                await _client.LogoutAsync();
            }
        }

        public async Task MessageUpdated(Cacheable<IMessage, UInt64> old, SocketMessage msg, 
                                         ISocketMessageChannel chan)
        {
            string old_message;
            Console.Write(old.HasValue);
            if (old.HasValue) 
            {
                old_message = old.Value.Content;
            } else
            {
                old_message = "Not Found";
            }
            await chan.SendMessageAsync(old_message);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}