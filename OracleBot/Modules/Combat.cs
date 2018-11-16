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
using DiceNotation;

namespace OracleBot.Modules
{
    public class Combat : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("Hurt")]
        [Summary("Reduce your character's HP by an amount. Usage: `.Hurt Amount`. Note: GMs can hurt any PC by inputting the name of the character at the end.")]
        public async Task Hurt(int amount, [Remainder] string Name = ""){
            amount = Math.Abs(amount);
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var user = Context.User as SocketGuildUser;
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
                    chr.Health.Current -= amount;
                    if (chr.Health.Current < 0) chr.Health.Current = 0;
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" took **"+amount+"** damage!");
                    await Context.Message.DeleteAsync();
                }
            }
            else if (Name != "" && user.GetPermissions(Context.Channel as IGuildChannel).ManageMessages){
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
                    chr.Health.Current -= amount;
                    if (chr.Health.Current < 0) chr.Health.Current = 0;
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" took **"+amount+"** damage!");
                    if (chr.Health.Current == 0) await ReplyAsync(chr.Name+"'s HP has dropped to 0!");
                    await Context.Message.DeleteAsync();
                }
            }
        }
        [Command("Heal")]
        [Summary("Heals your character's HP by an amount. Usage: `.Heal Amount`. Note: GMs can heal any PC by inputting the name of the character at the end.")]
        public async Task Heal(int amount, [Remainder] string Name = ""){
            amount = Math.Abs(amount);
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var user = Context.User as SocketGuildUser;
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
                    chr.Health.Current += amount;
                    if (chr.Health.Current > chr.Health.GetHealth(chr.AbilityScores[2].GetIntMod().ToString())) chr.Health.Current = chr.Health.GetHealth(chr.AbilityScores[2].GetIntMod().ToString());
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" healed **"+amount+"** Hit Points.");
                    await Context.Message.DeleteAsync();
                }
            }
            else if (Name != "" && user.GetPermissions(Context.Channel as IGuildChannel).ManageMessages){
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
                    chr.Health.Current += amount;
                    if (chr.Health.Current > chr.Health.GetHealth(chr.AbilityScores[2].GetValue())) chr.Health.Current = chr.Health.GetHealth(chr.AbilityScores[2].GetValue());
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" healed **"+amount+"** Hit Points.");
                    await Context.Message.DeleteAsync();
                }
            }
        }
        [Command("FullRestore"), Alias("FullHeal")]
        [Summary("Heals your character's HP completely. Usage: `.FullHeal`. Note: GMs can heal any PC by inputting the name of the character at the end.")]
        public async Task FHeal([Remainder] string Name = ""){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var user = Context.User as SocketGuildUser;
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
                    chr.Health.Current = chr.Health.GetHealth(chr.AbilityScores[2].GetValue());
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" fully healed healed their Hit Points.");
                    await Context.Message.DeleteAsync();
                }
            }
            else if (Name != "" && user.GetPermissions(Context.Channel as IGuildChannel).ManageMessages){
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
                    chr.Health.Current = chr.Health.GetHealth(chr.AbilityScores[2].GetValue());
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" healed fully healed their Hit Points.");
                    await Context.Message.DeleteAsync();
                }
            }
        }
    }
}