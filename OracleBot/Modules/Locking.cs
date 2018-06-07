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
    public class Locking : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("Lock")]
        public async Task Lock([Remainder] string Name){
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
                    msg += "`" + q.Name + "`, ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(5));
                return;
            }
            else if (Query.Count() == 1){
                var character = Query.FirstOrDefault();
                var user = Context.User as SocketGuildUser;
                var plr= Database.GetCollection<player>("Players");

                if(!plr.Exists(x => x.DiscordId == Context.User.Id)){
                    plr.Insert(new player(){
                        DiscordId = Context.User.Id,
                        Character = null
                    });
                }
                var LockFile = plr.FindOne(x => x.DiscordId == Context.User.Id); 
                if (character.Owner != Context.User.Id && !user.GuildPermissions.ManageMessages){
                    await ReplyAndDeleteAsync(user.Mention+ ", You can't lock into characters you don't own!",false,null,TimeSpan.FromSeconds(5));
                    return;
                }
                else {
                    LockFile.Character = character;
                    plr.Update(LockFile);
                    await ReplyAndDeleteAsync(user.Mention+", you've been locked as **"+character.Name+"**.");
                    return;
                }
            }
        }
    }
}