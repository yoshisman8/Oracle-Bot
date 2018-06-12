using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using LiteDB;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;
using OracleBot.Classes;
using DiceNotation;

namespace OracleBot.Modules
{
    public enum CombatantType {Character, NPC}
    public class Battle{
        [BsonId]
        public ulong Channel {get;set;}
        public List<Combatant> Combatants {get;set;} = new List<Combatant>();
        public bool Active {get;set;} = false;

        public async Task PrintTurn(LiteDatabase Database, SocketCommandContext context){
            var col = Database.GetCollection<Character>("Characters");
            var turn = Combatants.First();
            
            if (turn.Type == CombatantType.NPC){
                await context.Channel.SendMessageAsync("**"+turn.Name+"'s turn!");
                return;
            }
            else{
                var chr = col.FindOne(x => x.ID == turn.CharacterId);
                var owner = context.Client.GetUser(chr.Owner);
                await context.Channel.SendMessageAsync(owner.Mention+" **"+chr.Name+"**'s Turn ["+chr.Health.Current+"/"+chr.Health.GetHealth(chr.AbilityScores[2].GetValue())+"]");
            }
        }
    }
    public class Combatant{
        public string Name {get;set;} = "";
        public int CharacterId {get;set;}
        public int Initiative {get;set;} = 1;
        public CombatantType Type {get;set;} = CombatantType.NPC;
    }
    public class Turn_Tracker : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        [Command("Battle")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Summary("Starts/stops a battle on this text channel. Usage `.Battlestart` Note: Only **one** battle can be happen per channel. Only GMs can start battles.")]
        public async Task StartBattle(){
            var col = Database.GetCollection<Battle>("Battles");
            if (!col.Exists(x => x.Channel == Context.Channel.Id)){
                col.Insert(new Battle(){
                    Channel = Context.Channel.Id
                });
            }
            var battle = col.FindOne(x => x.Channel == Context.Channel.Id);
            
            if (!battle.Active){
                battle.Active = true;
                col.Update(battle);
                await ReplyAsync("Battle has started on #"+Context.Channel.Name+"!");
                var debug = col.FindOne(x => x.Channel == Context.Channel.Id);
                await Context.Message.DeleteAsync();
                return;
            }
            else if (battle.Active){
                battle.Active = false;
                battle.Combatants.Clear();
                col.Update(battle);
                await ReplyAsync("The battle on #"+Context.Channel.Name+" is now over!");
                var debug = col.FindOne(x => x.Channel == Context.Channel.Id);
                await Context.Message.DeleteAsync();
                return;
            }
        }
        [Command("Join")]
        [Summary("Joins a battle that's happening on the chatroom the command is sent from. Usage: `.Join Initiative`. Note: if you don't include an Initiative value I'll roll D20+Dex modifier automatically.")]
        public async Task Join(int Initiative = -1996){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var battles = Database.GetCollection<Battle>("Battles");
            if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                await ReplyAndDeleteAsync(Context.User.Mention+", you've never made any character so I can't find your character! Please make one with `.newchar Name`!", timeout: TimeSpan.FromSeconds(5));                    return;
            }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (plr.Character == null){
                await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                return;
            }
            var chr = plr.Character;
            if (!battles.Exists(x => x.Channel == Context.Channel.Id)){
                battles.Insert(new Battle(){
                    Channel = Context.Channel.Id
                });
            }
            var battle = battles.FindOne(x => x.Channel == Context.Channel.Id);
            if (!battle.Active){
                await ReplyAndDeleteAsync("There isn't a battle happening on this room!",timeout: TimeSpan.FromSeconds(5));
                await Context.Message.DeleteAsync();
                return; 
            }
            if (Initiative == -1996){
                var parser = new DiceParser();
                string dice = "1D20 + "+chr.AbilityScores[1].GetIntMod();
                int result = parser.Parse(dice).Roll().Value;
                Initiative = result;
            }
            if (battle.Combatants.Exists(x => x.CharacterId == chr.ID)){
                var c = battle.Combatants.Find(x => x.CharacterId == chr.ID);
                var I = battle.Combatants.IndexOf(c);
                c.Initiative = Initiative;
                battle.Combatants[I] = c;
                battles.Update(battle);
                await ReplyAsync(chr.Name+" Rejoined combat with an Initiative of **"+Initiative+"**.");
                await Context.Message.DeleteAsync();
                return;
            }
            else{
                battle.Combatants.Add(new Combatant(){
                    CharacterId = chr.ID,
                    Name = chr.Name,
                    Initiative = Initiative,
                    Type = CombatantType.Character
                });
                battles.Update(battle);
                await ReplyAsync(chr.Name+" Joined combat with an Initiative of **"+Initiative+"**.");
                await Context.Message.DeleteAsync();
                return;
            }
        }
        [Command("AddNPC")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [Summary("Adds an NPC to the battle. Usage: `.AddNPC Name Initiative`. Only GMs can add NPCs to a battle.")]
        public async Task NPCjoin(string Name, int Initiative){
            var battles = Database.GetCollection<Battle>("Battles");
            var battle = battles.FindOne(x => x.Channel == Context.Channel.Id);
            if (!battle.Active){
                await ReplyAndDeleteAsync("There isn't a battle happening on this room!",timeout: TimeSpan.FromSeconds(5));
                return; 
            }
        if (battle.Combatants.Exists(x => x.Name.ToLower() == Name.ToLower())){
                var c = battle.Combatants.Find(x=> x.Name.ToLower() == Name.ToLower());
                var I = battle.Combatants.IndexOf(c);
                c.Initiative = Initiative;
                battle.Combatants[I] = c;
                battles.Update(battle);
                await ReplyAsync(c.Name+" Rejoined combat with an Initiative of **"+Initiative+"**.");
                await Context.Message.DeleteAsync();
                return;
            }
            else{
                battle.Combatants.Add(new Combatant(){
                    Name = Name,
                    Initiative = Initiative,
                    Type = CombatantType.NPC
                });
                battles.Update(battle);
                await ReplyAsync(Name+" Joined combat with an Initiative of **"+Initiative+"**.");
                await Context.Message.DeleteAsync();
                return;
            }
        }
        [Command("StartBattle"),Summary("GM only command. Sort turn order from Highest to Lowest Initiative and start the combat, displaying the first turn. Message is deleted after 10 seconds.")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        public async Task Sort(){
            var battles = Database.GetCollection<Battle>("Battles");
            var battle = battles.FindOne(x => x.Channel == Context.Channel.Id);
            if (!battle.Active){
                await ReplyAndDeleteAsync("There isn't a battle happening on this room!",timeout: TimeSpan.FromSeconds(5));
                return; 
            }
            battle.Combatants = battle.Combatants.OrderByDescending(x =>x.Initiative).ToList();
            battles.Update(battle);
            var sb = new StringBuilder();
            foreach (var q in battle.Combatants)
                {
                    sb.Append("`" + q.Name + "`, ");
                }
            await ReplyAsync("Turn order: "+sb.ToString().Substring(0,sb.Length-2));
            await battle.PrintTurn(Database,Context);
        }
        [Command("Next")]
        [Summary("Ends your turn and notifies the next combatant on queue. Usage: `.Next`")]
        public async Task Next(){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var battles = Database.GetCollection<Battle>("Battles");
            var user = Context.User as SocketGuildUser;
            if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                await ReplyAndDeleteAsync(Context.User.Mention+", you've never made any character so I can't find your character! Please make one with `.newchar Name`!", timeout: TimeSpan.FromSeconds(5));                    return;
            }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (plr.Character == null){
                await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                return;
            }
            var chr = plr.Character;
            if (!battles.Exists(x => x.Channel == Context.Channel.Id)){
                battles.Insert(new Battle(){
                    Channel = Context.Channel.Id
                });
            }
            var battle = battles.FindOne(x => x.Channel == Context.Channel.Id);
            
            if (!battle.Active){
                await ReplyAndDeleteAsync("There isn't a battle happening on this room!",timeout: TimeSpan.FromSeconds(5));
                await Context.Message.DeleteAsync();
                return; 
            }
            if (battle.Combatants.First().Type == CombatantType.NPC && user.GuildPermissions.ManageMessages == false){
                await ReplyAndDeleteAsync("Its not your turn!",timeout: TimeSpan.FromSeconds(5));
                await Context.Message.DeleteAsync();
                return;
            }
            var turn = col.FindOne(x=> x.ID == battle.Combatants.First().CharacterId);
            if (turn.Owner != Context.User.Id && user.GuildPermissions.ManageMessages == false){
                await ReplyAndDeleteAsync("Its not your turn!",timeout: TimeSpan.FromSeconds(5));
                await Context.Message.DeleteAsync();
                return;
            }
            var buffer = battle.Combatants;
            var first = buffer.First();
            buffer.RemoveAt(0);
            buffer.Add(first);
            battle.Combatants = buffer;
            battles.Update(battle);
            await battle.PrintTurn(Database,Context);
            await Context.Message.DeleteAsync();
        }
        [Command("TurnOrder")]
        [Summary("Shows the turn order for the current battle. Usage: `.TurnOreder`.")]
        public async Task TurnOrder(){
            var battles = Database.GetCollection<Battle>("Battles");
            var battle = battles.FindOne(x => x.Channel == Context.Channel.Id);
            if (!battle.Active){
                await ReplyAndDeleteAsync("There isn't a battle happening on this room!",timeout: TimeSpan.FromSeconds(5));
                return; 
            }
            var sb = new StringBuilder();
            foreach (var q in battle.Combatants)
                {
                    sb.Append("`" + q.Name + "`, ");
                }
            await ReplyAsync("Turn order: "+sb.ToString().Substring(0,sb.Length-2));
        }
    }
}