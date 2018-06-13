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
    public class Combat : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("NewAttack")]
        [Summary("Adds a new attack to your character's attack list. Usage: `.NewAttack Name Description Type`. Valid types are 'Melee' and 'Magic'.")]
        public async Task NewAttack(string Name, string Description, AttackType Type = AttackType.Melee){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
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
            else{
                var chr = plr.Character;
                if (chr.Attacks.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var Attacks = chr.Attacks.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    int index = chr.Attacks.IndexOf(Attacks);
                    Attacks.Description = Description;
                    chr.Attacks[index] = Attacks;
                    await ReplyAsync(Context.User.Mention+", Updated "+chr.Name+"'s attack **"+Attacks.Name+"**.");
                }
                else{
                    var ab = new Attack(){
                        Name = Name,
                        Description = Description
                    };
                    chr.Attacks.Add(ab);
                    await ReplyAsync(Context.User.Mention+", Added "+Type+" Attack **"+ab.Name+"** to "+chr.Name+".");
                }
                col.Update(chr);
                await Context.Message.DeleteAsync();
            }
        }
        [Command("RemoveAttack"), Alias("RemAttack", "DeleteAttack","DelAttack")]
        [Summary("Removes an attack from your character's attack list. Usage: `.RemAttack Name`.")]
        public async Task RemAttack([Remainder] string Name){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
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
            else{
                var chr = plr.Character;
                if (!chr.Attacks.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    await ReplyAndDeleteAsync(Context.User.Mention+", your character doesn't have any Attacks whose name starts with "+Name+".", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                if (chr.Attacks.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).Count() > 1 && !chr.Attacks.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var result = chr.Attacks.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    var sb = new StringBuilder().Append(Context.User.Mention+", "+chr.Name+" has more than " + result.Count()+ " Attacks that starts with the word **"+Name+"**. Please specify which one from this list is the one you want to remove by using said Trait's full name: ");
                    foreach (var x in result){
                        sb.Append("`"+x.Name+"`, ");
                    }
                    await ReplyAndDeleteAsync(sb.ToString().Substring(0,sb.Length-2), timeout: TimeSpan.FromSeconds(10));
                    return;
                }
                else{
                    var trait = chr.Attacks.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    chr.Attacks.Remove(trait);
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", you removed "+chr.Name+"'s attack **"+trait.Name+"**.");
                }
                await Context.Message.DeleteAsync();
            }
        }
        [Command("Attack")]
        [Summary("Evoke one of your attacks (And roll their macro if they have one). Usage: `.Attack Name`")]
        public async Task MeleeAttack([Remainder] string Name){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
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
            else{
                var chr = plr.Character;
                if (!chr.Attacks.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    await ReplyAndDeleteAsync(Context.User.Mention+", your character doesn't have any Attacks whose name starts with "+Name+".", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                if (chr.Attacks.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).Count() > 1 && !chr.Attacks.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var result = chr.Attacks.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    var sb = new StringBuilder().Append(Context.User.Mention+", "+chr.Name+" has more than " + result.Count()+ " Attacks that starts with the word **"+Name+"**. Please specify which one from this list is the one you want to remove by using said Trait's full name: ");
                    foreach (var x in result){
                        sb.Append("`"+x.Name+"`, ");
                    }
                    await ReplyAndDeleteAsync(sb.ToString().Substring(0,sb.Length-2), timeout: TimeSpan.FromSeconds(10));
                    return;
                }
                else{
                    var trait = chr.Attacks.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    string title = ((trait.Type == AttackType.Melee) || (trait.Type == AttackType.Spell)) ? chr.Name+" used their technique: " : chr.Name+" casted " ;
                    var embed = new EmbedBuilder()
                        .WithTitle(title+trait.Name)
                        .WithDescription(trait.Description)
                        .WithColor(chr.Color[0],chr.Color[1],chr.Color[2]);
                    await ReplyAsync("",false,embed.Build());
                }
                await Context.Message.DeleteAsync();
            }
        }
        [Command("Attacks")]
        [Summary("Shows someone character's attacks and their details. Usage: `.Attacks Name`. Use this command without any name to show your character's attacks.")]
        public async Task getatks([Remainder] string Name = ""){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            if (Name == "" || Name == null){
                if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                    await ReplyAndDeleteAsync(Context.User.Mention+", you've never made any character so I can't find your character! Please make one with `.newchar Name`!", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                var plr = players
                    .Include(x => x.Character)
                    .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                    .FindOne(x => x.DiscordId == Context.User.Id);
                if (plr.Character == null){
                    await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                    return;
                }
                else{
                    var chr = plr.Character;
                    var embed = new EmbedBuilder()
                        .WithTitle(chr.Name+"'s Attacks");
                    foreach(var x in chr.Attacks){
                        string title = ((x.Type == AttackType.Melee) || (x.Type == AttackType.Spell)) ? "💥 " : "✨ ";
                        embed.AddField(title+x.Name,x.Description);
                    }
                    await ReplyAsync("",false,embed.Build());
                    await Context.Message.DeleteAsync();
                }
            }
            else{
                var Query = col.Find(x => x.Name.StartsWith(Name.ToLower()));
                if(Query.Count() == 0) {
                    await ReplyAndDeleteAsync(Context.User.Mention+", There is no character with that name on the database.", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else if (Query.Count() > 1 && !Query.ToList().Exists(x => x.Name.ToLower() == Name.ToLower())){
                    string msg = Context.User.Mention+", Multiple charactes were found! Please specify which one of the following characters is the one you're looking for: ";
                    foreach (var q in Query)
                    {
                        msg += "`" + q.Name + "` ";
                    }
                    await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                    return;
                }
                else if (Query.Count() == 1 || Query.ToList().Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var chr = Query.FirstOrDefault();
                    var embed = new EmbedBuilder()
                        .WithTitle(chr.Name+"'s Attacks");
                    foreach(var x in chr.Attacks){
                        string title = ((x.Type == AttackType.Melee) || (x.Type == AttackType.Spell)) ? "💥 " : "✨ ";
                        embed.AddField(title+x.Name,x.Description);
                    }
                    await ReplyAsync("",false,embed.Build());
                    await Context.Message.DeleteAsync();
                }
            }
        }
    }
}