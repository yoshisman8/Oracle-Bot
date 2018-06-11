using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;
using OracleBot.Classes;

namespace OracleBot.Modules
{
    public enum CombatantType {Character, NPC}
    public class Battle{
        [BsonId]
        public ulong Channel {get;set;}
        public List<Combatant> Combatants {get;set;} = new List<Combatant>();
    }
    public class Combatant{
        public string Name {get;set;} = "";
        public int Health {get;set;} = 0;
        public int CurrentHP {get;set;} = 0;
        public int Initiative {get;set;} = 1;
        public CombatantType Type {get;set;} = CombatantType.NPC;
    }
    public class Combat : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        [Command("StartBattle")]
        [Summary("Starts a battle on this text channel. Usage `.Battlestart` Note: Only **one** battle can be happen per channel.")]
        public async Task StartBattle(){
            var col = Database.GetCollection<Battle>("Battles");
            if (!col.Exists(x => x.Channel == Context.Channel.Id)){
                col.Insert(new Battle(){
                    Channel = Context.Channel.Id
                });
            }
            
        }
    }
}