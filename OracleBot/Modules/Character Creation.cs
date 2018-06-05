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
    public class CharacterCreation : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("NewCharacter"), Alias("CreateCharacter","NewChar","CreateChar")]
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
            character.ID = col.Insert(character);            
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
            }
            await ReplyAsync(Context.User.Mention+", Character **"+character.Name+"** was created successfuly! Make sure to consult the help files on `.help basics` to complete its set up.");
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
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(5));
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
                }
            }
        }
        [Command("Character"), Alias("Char")]
        [Summary("Show your character or someone else's character. Usage: `.Char Character_name`. Leave empty to show the character you're locked into.")]
        public async Task Get([Remainder] string Name = ""){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            if (Name == "" || Name == null){
                if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                    await ReplyAndDeleteAsync(Context.User.Mention+", you've never made any character so I can't find your character! Please make one with `.newchar Name`!", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                var plr = players
                    .Include(x => x.Character)
                    .Include(x => x.Character.AbilityScores)
                    .FindOne(x => x.DiscordId == Context.User.Id);
                if (plr.Character == null){
                    await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                    return;
                }
                else{
                    var chr = plr.Character;
                    await ReplyAsync("",false,chr.GetSheet());
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
                    await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else if (Query.Count() == 1 || Query.ToList().Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var chr = Query.FirstOrDefault();
                    await ReplyAsync("",false,chr.GetSheet());
                }
            }
        }
    }
}