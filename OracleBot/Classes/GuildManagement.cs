using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LiteDB;
using OracleBot;
using Discord.Commands;

namespace OracleBot.Classes
{

    public class Server
    {
        [BsonId]
        public ulong Id {get;set;}
        [BsonRef("Players")]
        public List<Player> ActivePlayers {get;set;} = new List<Player>();
        public char Prefix {get;set;} = '$';
        public Dictionary<ModuleInfo,bool> Modules {get;set;} = new Dictionary<ModuleInfo,bool>();

        public Server(CommandService Commands)
        {
            foreach(ModuleInfo c in Commands.Modules.Where(x=>x.Attributes.Any(y => y is Include)))
            {
               Modules.Add(c,true);
            }
        }
        public void EnsureModules(CommandService Commands)
        {
            foreach(ModuleInfo c in Commands.Modules.Where(x=>x.Attributes.Any(y => y is Include)))
            {
               if(!Modules.Any(x=>x.Key==c)) Modules.Add(c,true);
            }
        }
        public ModuleInfo GetModule(string Name)
        {
            return Modules.Keys.Single(x=>x.Name.ToLower().StartsWith(Name.ToLower()));
        }
        public bool ToggleModule(ModuleInfo Module)
        {
            Modules[Module] ^= true;
            return Modules[Module];
        }
    }
    public class Player 
    {
        [BsonId]
        public ulong DiscordId {get;set;}
        [BsonRef("Characters")]
        public Character LockedCharacter {get;set;} = null;
        public Player(ulong id) => DiscordId = id;
    }
}