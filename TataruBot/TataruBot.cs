using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json;


// To Do:



namespace TataruBot {
    class TataruBot {
        private DiscordSocketClient _client;
        private string generalchannel {get; set;}
        private Configs configs; 
        private DatabaseManager dbmanager;


        static void Main(string[] args) => 
            new TataruBot().MainAsync().GetAwaiter().GetResult();

        private async Task MainAsync() {
            // Initiate local variables
            PurgeInProgress = new List<ulong>();
            PurgeFlagged = new List<ulong>();

            // Collect parameters from Configs.json
            string json_configs_path = Path.Combine(Environment.CurrentDirectory, 
                                               "Configs.json");
            string json_configs = File.ReadAllText(json_configs_path);
            configs = JsonConvert.DeserializeObject<Configs>(json_configs);
            
            // Creating SQL Client class
            dbmanager = new DatabaseManager(configs.SQLClientData["userID"], 
                                            configs.SQLClientData["pwd"], 
                                            configs.SQLClientData["server"], 
                                            configs.SQLClientData["database"]);

            // Create Discord Socket client with message cache
            var _configs = new DiscordSocketConfig { 
                MessageCacheSize = 100, ExclusiveBulkDelete = true 
            };
            _client = new DiscordSocketClient(_configs);

            // Add Events:
            _client.Log += Logger;
            _client.UserJoined += AssignNewMemberRole;
            _client.UserVoiceStateUpdated += AssignVoiceTextChannel;
            _client.ReactionAdded += AddRoleViaReaction;
            _client.ReactionRemoved += RemoveRoleViaReaction;
            _client.MessageReceived += MessageReceived;

            // Connect:
            await _client.LoginAsync(TokenType.Bot, configs.BotToken);
            await _client.StartAsync();

            // Prevent connection from closing
            await Task.Delay(-1);
        }

        public Task Logger(LogMessage log) {
            // Logs all to console
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }


        public void PrintLog(string Message) {
            // For custom logs
            LogMessage log = new LogMessage(LogSeverity.Info, "Custom", Message);
            Logger(log);
        }
        

        // =================================================
        // GENERAL ROLE ASSIGNMENT METHODS
        // =================================================

        private async Task AddRole(SocketGuildUser member, ulong RoleID) {
            // Get role and add to user
            SocketRole role = member.Guild.GetRole(RoleID);
            await member.AddRoleAsync(role, new RequestOptions());
            // Log Message
            PrintLog($"Member \"{member.Username}\" assigned to role \"{role.Name}\"");
        }

        private async Task RemoveRole(SocketGuildUser member, ulong RoleID) {
            // Get role and remove user
            SocketRole role = member.Guild.GetRole(RoleID);
            await member.RemoveRoleAsync(role, new RequestOptions());
            // Log Message
            PrintLog($"Member \"{member.Username}\" removed from role \"{role.Name}\"");
        }

        
        // =================================================
        // PROCESS CHAT COMMANDS
        // =================================================

        public async Task MessageReceived(SocketMessage message) {
            // Process all incomming chat messages
            // Returns if bot command
            var msg = message as SocketUserMessage;
            if (msg == null) return;

            // Determines whether this is a ping command or text command
            Func<SocketUserMessage, Task> Processor;
            int pos = 0;

            if (msg.HasStringPrefix($"<@{_client.CurrentUser.Id}>", ref pos)) 
                Processor = ProcessPingCommands;
            else if (msg.HasCharPrefix(configs.CommandPrefix, ref pos)) 
                Processor = ProcessTextCommands;
            else return;

            await Processor(msg);
        }

        private async Task ProcessPingCommands(SocketUserMessage msg) {
            string command = msg.Content.Replace($"<@{_client.CurrentUser.Id}>", "")
                                 .Trim().ToLower();
            var guild = GetGuildFromChannel(msg);

            // Check server change command
            if (command.StartsWith("change server")) {
                string param = command.Replace("change server", "").Trim();
                await RelocateServer(guild, msg, param);
            }


            // The following commands can only be done within permitted bot spam channel:
            if (!configs.PermittedBotSpam.Contains(msg.Channel.Id)) {
                return;
            }
            // Database grab from fight resources
            foreach (string fight_name in configs.FightList) {
                // If fight is not mentioned, continue
                if (!command.Contains(fight_name)) {
                    continue;
                }
                string[] columns = new string[] {"fight", "url"};
                var param = new Dictionary<string, string>();
                param.Add("fight", fight_name);

                // Checks for resource types
                foreach (string resource in configs.ResourceList) {
                    if (!command.Contains(resource)) {
                        continue;
                    }
                    param.Add("resource_type", resource);

                    // If resource found, call and print
                    var results = await GetFightResource(columns, param);
                    foreach(string line in results) {
                        if (line != "") {
                            await msg.Channel.SendMessageAsync(line);
                        }
                    }

                }

            }
        }
        
        private async Task ProcessTextCommands(SocketUserMessage msg) {
            string command = msg.Content.Substring(1).Trim().ToLower();
            var guild = GetGuildFromChannel(msg);
            // Check for log off command
            if (command.StartsWith("test")) {
                await msg.Channel.SendMessageAsync("Test Response");

            }

            // Database test grab
            if (command.StartsWith("grab-test")) {
                string table = "test_table";
                string[] columns = new string[]{"ID", "Test"};
                var param = new Dictionary<string, string>();
                param.Add("ID", "1");
                var result = await dbmanager.get_data(table, columns, param);
                PrintLog(result[0]["ID"]);
                PrintLog(result[0]["Test"]);
            }

            
        }

        private async Task<List<string>> GetFightResource(string[] cols, Dictionary<string, string> param) {
            // Gets the results from the database and formats them into a series of strings
            // To be sent to the server, max char length of 1800
            int max_length = 1800;

            try {
                var db_results = await dbmanager.get_data("resources", cols, param);
                string message = "";

                var output = new List<string>();
                output.Add($"**{TitleCaseString(param["resource_type"])} for {param["fight"]}:**");

                foreach (var row_data in db_results) {
                    string addition = row_data["url"] + "\n";

                    // Ensure message isnt past character length
                    if (addition.Length + message.Length > max_length){
                        output.Add(message);
                        message = "";
                    }

                    message += addition;
                }
                output.Add(message);
                return output;

            } catch(Exception e) {
                Console.WriteLine(e.ToString());
                return new List<String>() {"Error Occured while getting resource"};
            }
        }

        private string TitleCaseString(string text) {
            return char.ToUpper(text[0]) + text.Substring(1);
        }

        private SocketGuild GetGuildFromChannel(SocketUserMessage msg) {
            var channel = msg.Channel as SocketGuildChannel;
            return channel.Guild;
        }

        // =================================================
        // NEW MEMBER ROLE ASSIGNMENT
        // =================================================

        public async Task AssignNewMemberRole(SocketGuildUser member) {
            // For new members to be given automatic role
            PrintLog($"New Member Joined - \"{member.Username}\"");
            await AddRole(member, configs.NewMemberRoleID);
        }


        // =================================================
        // ROLE ASSIGNMENTS BASED ON VOICE STATE UPDATE
        // =================================================

        public async Task AssignVoiceTextChannel(SocketUser user, 
                                                 SocketVoiceState vs_before, 
                                                 SocketVoiceState vs_after) {
            // Assign role based on voice channel
            // Gets voice channel IDs before and after
            var guilduser = user as SocketGuildUser;
            SocketVoiceChannel channel_before = vs_before.VoiceChannel;
            SocketVoiceChannel channel_after = vs_after.VoiceChannel;

            // Removes rank from previous voice channel if exists
            if (VerifyVoiceTextChannel(channel_before)) {
                await RemoveRole(guilduser, 
                                 configs.VoiceChannelRoleIDs[channel_before.Id]);
                // Checks if channel is now empty, if it is purge the text channel
                var users_in_channel = channel_before.Users;
                if (users_in_channel.Count == 0) {// Get associated voice channel
                    ulong text_ID = configs.VoiceTextChannelIDs[channel_before.Id];
                    var text_channel = guilduser.Guild.GetChannel(text_ID) as SocketTextChannel;
                    PurgeChannel(text_channel);
                }
            }

            // Adds rank from 
            if (VerifyVoiceTextChannel(channel_after)) {
                await AddRole(guilduser, 
                              configs.VoiceChannelRoleIDs[channel_after.Id]);
                ulong text_ID = configs.VoiceTextChannelIDs[channel_after.Id];
                if (PurgeInProgress.Contains(text_ID))
                    PurgeFlagged.Add(text_ID);
            }

        }

        private bool VerifyVoiceTextChannel(SocketVoiceChannel channel) {
            return ((channel != null) && configs.VoiceChannelRoleIDs.ContainsKey(channel.Id));
        }

        // =================================================
        // PURGE CHANNEL ON SEPARATE THREAD
        // =================================================
        private List<ulong> PurgeInProgress;
        private List<ulong> PurgeFlagged;

        public void PurgeChannel(SocketTextChannel channel) {
            // Initiates another thread to purge the voice channel
            // Required to be on another thread or event thread is blocked
            // Ensure channel is not currently being purged
            if (PurgeInProgress.Contains(channel.Id)) return;

            // Purge channel
            new Thread(delegate() {Purge(channel);}).Start();
        }

        private async void Purge(SocketTextChannel channel) {
            // Delays purging the channel for set period of time. During the delay
            // it will continue to check if the purge should still complete
            // using PurgeFlagged list

            int max_time = 300;  // Delay time in seconds before starting purge
            int message_count = 100;  // Number of messages in one batch

            PrintLog($"Channel {Convert.ToString(channel.Id)} primed for purge");
            string message = "Voice channel is empty, purging channel in 5 minutes";
            var msg = await channel.SendMessageAsync(message);
            PurgeInProgress.Add(channel.Id);

            // Add channel ID to PurgeFlagged to prevent purge
            for(int i = 0; i < max_time; i++) {
                Thread.Sleep(1000);
                if (PurgeFlagged.Contains(channel.Id)) {
                    PurgeFlagged.Remove(channel.Id);
                    PurgeInProgress.Remove(channel.Id);
                    await msg.DeleteAsync();
                    PrintLog($"Channel {Convert.ToString(channel.Id)} purge cancelled");
                    return;
                }
            }
            // Purge will commence
            PrintLog($"Channel {Convert.ToString(channel.Id)} purge imminent");

            // Collect messages and purges
            var msgs = await channel.GetMessagesAsync(message_count).FlattenAsync() 
                       as IReadOnlyCollection<IMessage>;
            // Repeat purges until all detected messages have been removed
            while (msgs.Count > 1) {
                await channel.DeleteMessagesAsync(msgs);
                msg = await channel.SendMessageAsync("Purging voice channel...");
                Thread.Sleep(5000);
                msgs = await channel.GetMessagesAsync(message_count).FlattenAsync() 
                       as IReadOnlyCollection<IMessage>;
            }
            await msg.DeleteAsync();
            
            // Ending purge
            PrintLog($"Channel {Convert.ToString(channel.Id)} purge complete");
            PurgeInProgress.Remove(channel.Id);
            PurgeFlagged.Remove(channel.Id);
        }

        // =================================================
        // REACTION BASED ROLE ASSIGNMENTS
        // =================================================

        public async Task AddRoleViaReaction(Cacheable<IUserMessage, ulong> hist, 
                                             ISocketMessageChannel channel, 
                                             SocketReaction reaction) {
            // Verifies message reacted to and emotes reacted with
            if (VerifyReaction(reaction)) {
                await ManageRoleViaReaction(channel, reaction, AddRole);
            }
        }

        public async Task RemoveRoleViaReaction(Cacheable<IUserMessage, ulong> hist, 
                                                ISocketMessageChannel channel, 
                                                SocketReaction reaction) {
            // Verifies message reacted to and emotes reacted with
            if (VerifyReaction(reaction)) {
                await ManageRoleViaReaction(channel, reaction, RemoveRole);
            }
        }

        private async Task ManageRoleViaReaction(ISocketMessageChannel channel, 
                                                 SocketReaction reaction, 
                                                 Func<SocketGuildUser, ulong, Task> Manager) {
            // Get associated roleID
            string emote = reaction.Emote.ToString();
            ulong roleID = configs.ReactionRolePairIds[emote];
            // Get associated user
            var guildchannel = channel as SocketGuildChannel;
            SocketGuildUser user = guildchannel.GetUser(reaction.UserId);
            // Run callback function
            await Manager(user, roleID);
        }

        private bool VerifyReaction(SocketReaction reaction) {
            return ((reaction.MessageId == configs.RoleMessageId)) &&
                     configs.ReactionRolePairIds.ContainsKey(reaction.Emote.ToString());
        }

        
        // =================================================
        // SERVER RELOCATE COMMAND
        // =================================================

        private async Task RelocateServer(SocketGuild guild, SocketUserMessage msg, string target="") {
            // Names of regions to serach for
            string[] region_list = {"us-south", "us-west", "us-central", "us-east"};

            // If a location was provided, sets the target region
            // If a location was not provided, gets the next region in the list
            if (Array.IndexOf(region_list, target) == -1) {
                int current_region_id = Array.IndexOf(region_list, guild.VoiceRegionId);
                target = region_list[(current_region_id + 1) % region_list.Length];
            }

            // Modifies guild settings
            await guild.ModifyAsync(x => {x.RegionId = target;});
            
            // Posts notification
            await msg.Channel.SendMessageAsync($"Voice Region changed to {target}!");
            PrintLog($"Voice Region changed to {target}");
        }
    }
}
