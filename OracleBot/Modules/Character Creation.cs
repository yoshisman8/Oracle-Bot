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
    public class Main_Commands : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("NewCharacter"), Alias("CreateCharacter","NewChar","CreateChar","AddChar","AddCharacter")]
        [Summary("Creates a new character and assignes it to you! Usage: `.NewChar Character_Name1.")]
        public async Task NewChar([Remainder] string Name){
            var col = Database.GetCollection<Character>("Characters");
            var players = Database.GetCollection<player>("Players");
            if(col.Exists(x => x.Name == Name.ToLower())){
                await ReplyAndDeleteAsync("Someone already has a character with that exact same name! Please pick another name.",timeout: TimeSpan.FromSeconds(5));
                return;
            }
            var character = new Character();
            character.Name = Name;
            character.Owner = Context.User.Id;
            for (int x = 0; x <5;x++){
                character.AbilityScores[x] = new AbScore();
            }
            col.Insert(character);
            character = col.FindOne(x => x.Name == character.Name.ToLower());            
            col.EnsureIndex("Name","LOWER($.Name)");

            if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                var plr = new player(){
                    DiscordId = Context.User.Id,
                    Character = character
                };
                players.Insert(plr);
            }
            else {
                var plr = players.FindOne(x => x.DiscordId == Context.User.Id);
                plr.Character = character;
                players.Update(plr);
            }
            await ReplyAsync(Context.User.Mention+", Character **"+character.Name+"** was created successfuly! Make sure to consult the help files on `.help` to see all the comands you can do complete the set-up.");
            await Context.Message.DeleteAsync();
        }

        [Command("DeleteCharacter"), Alias("DeleteChar","DelChar","DelCharacter")]
        [Summary("Deletes a character you own. Usage: `.DeleteChar Character_Name`.")]
        public async Task DelChar([Remainder] string Name){
            var col = Database.GetCollection<Character>("Characters");
            var Query = col.Find(x => x.Name.StartsWith(Name.ToLower()));

            if(Query.Count() == 0) {
                await ReplyAndDeleteAsync(Context.User.Mention+", There is no character with that name on the database.", timeout: TimeSpan.FromSeconds(5));
                return;
            }
            else if (Query.Count() > 1){
                string msg = Context.User.Mention+", Multiple charactes were found! Please specify which one of the following characters is the one you're looking for: ";
                foreach (var q in Query)
                {
                    msg += "`" + q.Name + "` ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                return;
            }
            else if (Query.Count() == 1){
                var character = Query.FirstOrDefault();
                var user = Context.User as SocketGuildUser;
                if (Context.User.Id != character.Owner || !user.GuildPermissions.ManageMessages){
                    await ReplyAndDeleteAsync(Context.User.Mention+", You're not the owner of this character/you're not a Dungeon Master so you cannot delete this character!", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else {
                    col.Delete(character.ID);
                    await ReplyAsync(Context.User.Mention+", **"+character.Name+"** has been deleted from the database.");
                    await Context.Message.DeleteAsync();
                }
            }
        }
        [Command("Character"), Alias("Char","C")]
        [Summary("Show your character or someone else's character. Usage: `.Char Character_name`. Leave empty to show the character you're locked into.")]
        public async Task Get([Remainder] string Name = ""){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                    await ReplyAndDeleteAsync(Context.User.Mention+", you've never made any character so I can't find your character! Please make one with `.newchar Name`!", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (Name == "" || Name == null){
                if (plr.Character == null){
                    await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                    return;
                }
                else{
                    var chr = plr.Character;
                    chr.BuildInventory(Database,plr);
                    await ReplyAsync("",false,chr.GetSheet());
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
                    chr.BuildInventory(Database,plr);
                    await ReplyAsync("",false,chr.GetSheet());
                    await Context.Message.DeleteAsync();
                }
            }
        }
        [Command("Traits"), Alias("Trait")]
        [Summary("Shows the Traits of someone else's character in detail. Usage: `.Trait Character_Name`. Use this command without a name to see your locked character's traits.")]
        public async Task getskills([Remainder] string Name = ""){
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
                        .WithTitle(chr.Name+"'s Traits");
                    foreach(var x in chr.Traits){
                        embed.AddField(x.Name,x.Description);
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
                        .WithTitle(chr.Name+"'s Traits");
                    foreach(var x in chr.Traits){
                        embed.AddField(x.Name,x.Description);
                    }
                    await ReplyAsync("",false,embed.Build());
                    await Context.Message.DeleteAsync();
                }
            }
        }
        [Command("Abilities"), Alias("Abilities", "ABs")]
        [Summary("Shows the Abilities of someone else's character in detail. Usage: `.Abilities Character_Name`. Use this command without a name to see your locked character's Abilities.")]
        public async Task getabs([Remainder] string Name = ""){
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
                        .WithTitle(chr.Name+"'s Abilities");
                    foreach(var x in chr.Abilities){
                        embed.AddField(x.Name,x.Description);
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
                        .WithTitle(chr.Name+"'s Abilities");
                    foreach(var x in chr.Abilities){
                        embed.AddField(x.Name,x.Description);
                    }
                    await ReplyAsync("",false,embed.Build());
                    await Context.Message.DeleteAsync();
                }
            }
        }
        [Command("Characters"), Alias("Chars")]
        [Summary("Lists all the characters on the database. Usage: `.Chars SingleLetter`")]
        public async Task allchars(char FirstLetter = ' '){
            var col = Database.GetCollection<Character>("Characters");
            var all = col.FindAll();
            var embed = new EmbedBuilder()
                .WithTitle("Character list");
            var sb = new StringBuilder();
            if (FirstLetter != ' '){
                var Query = col.Find(x => x.Name.StartsWith(FirstLetter.ToString().ToLower()));
                if (Query.Count() == 0){ await ReplyAndDeleteAsync(Context.User.Mention+", there are no characters whose name start with "+FirstLetter+".",timeout: TimeSpan.FromSeconds(5)); return;}
                else{
                    foreach (var x in Query){
                        sb.AppendLine("• "+x.Name);
                    }
                    embed.AddField(FirstLetter.ToString(),sb.ToString());
                    await ReplyAsync("",false,embed.Build());
                    await Context.Message.DeleteAsync();
                    return;
                }
            }
            for (char c = 'a'; c <= 'z'; c++)
            {
                var Query = col.Find(x => x.Name.StartsWith(c));
                if (Query.Count() == 0) continue;
                foreach(var x in Query){
                    sb.AppendLine("• "+x.Name);
                }
                embed.AddField(c.ToString().ToUpper(),sb.ToString(),true);
                sb.Clear();
            }
            await ReplyAsync("",false,embed.Build());
            await Context.Message.DeleteAsync();
        }
    }
}