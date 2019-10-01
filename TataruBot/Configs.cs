using System.Collections.Generic;

// Configuration class, stores information about the bot
namespace TataruBot
{
    class Configs {
        public ulong NewMemberRoleID {get; set;}
        public string BotToken {get; set;}
        public char CommandPrefix {get; set;}
        public Dictionary<ulong, ulong> VoiceChannelRoleIDs {get; set;}
        public Dictionary<ulong, ulong> VoiceTextChannelIDs {get; set;}
        public Dictionary<string, string> SQLClientData {get; set;}
        public List<string> FightList {get; set;}
        public List<string> ResourceList {get; set;}
        public List<ulong> PermittedBotSpam {get; set;}

        // Properties associating roles with reactions:
        public ulong RoleMessageId {get; set;}
        public Dictionary<string, ulong> ReactionRolePairIds {get; set;}

        public Configs() {
            VoiceChannelRoleIDs = new Dictionary<ulong, ulong>();
            VoiceTextChannelIDs = new Dictionary<ulong, ulong>();
            ReactionRolePairIds = new Dictionary<string, ulong>();
            FightList = new List<string>();
            ResourceList = new List<string>();
            SQLClientData = new Dictionary<string, string>();
            PermittedBotSpam = new List<ulong>();
        }
    }
}