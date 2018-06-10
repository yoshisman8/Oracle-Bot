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
    public class Player_Vault : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("Lock")]
        public async Task Lock([Remainder] string Name = ""){
            var col = Database.GetCollection<Character>("Characters");
            var user = Context.User as SocketGuildUser;
            var plr= Database.GetCollection<player>("Players");

            if(!plr.Exists(x => x.DiscordId == Context.User.Id)){
                plr.Insert(new player(){
                    DiscordId = Context.User.Id,
                    Character = null
                });
            }
            var LockFile = plr.Include(x => x.Character).FindOne(x => x.DiscordId == Context.User.Id); 
            
            if(Name == ""){
                if(LockFile.Character == null){
                    await ReplyAsync(Context.User.Mention+", you're currently not locked to any character.");
                    return;
                }
                else{
                    await ReplyAsync(Context.User.Mention+", you're currently locked to **"+LockFile.Character.Name+"**.");
                    return;
                }

            }
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
                if (character.Owner != Context.User.Id && !user.GuildPermissions.ManageMessages){
                    await ReplyAndDeleteAsync(user.Mention+ ", You can't lock into characters you don't own!",false,null,TimeSpan.FromSeconds(5));
                    return;
                }
                else {
                    LockFile.Character = character;
                    plr.Update(LockFile);
                    await ReplyAndDeleteAsync(user.Mention+", you've been locked as **"+character.Name+"**.");
                    await Context.Message.DeleteAsync();
                    return;
                }
            }
        }
        [Command("Profile"),Alias("Whois","Me")]
        [Summary("Shows your player profile or someone elses if you input their *full* username. Usage: `.Profile Username`")]
        public async Task profile([Remainder] IUser User = null){
            var plr= Database.GetCollection<player>("Players");
            if (User == null){
                if(!plr.Exists(x => x.DiscordId == Context.User.Id)){
                    plr.Insert(new player(){
                        DiscordId = Context.User.Id,
                        Character = null
                    });
                }
                var LockFile = plr.Include(x => x.Character).Include(x => x.ItemVault).FindOne(x => x.DiscordId == Context.User.Id); 
                await ReplyAsync("",false,LockFile.BuildProfile(Context,Database));
                await Context.Message.DeleteAsync();
                return;
            }
            else {
                if(!plr.Exists(x => x.DiscordId == User.Id)){
                    plr.Insert(new player(){
                        DiscordId = User.Id,
                        Character = null
                    });
                }
                var LockFile = plr.Include(x => x.Character).Include(x=> x.ItemVault).FindOne(x => x.DiscordId == User.Id); 
                await ReplyAsync("",false,LockFile.BuildProfile(Context,Database));
                await Context.Message.DeleteAsync();
            }
        }
    }
}