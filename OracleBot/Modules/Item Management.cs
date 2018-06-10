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
    public class Item_Management : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public enum ItemLocation {Vault, Global}

        [Command("NewItem")]
        [Summary("Makes a new item and adds it to your local ItemVault. Usage: `.NewItem Name Description`. \n**For DMs**: add 'Global' after the description to add the item to the global Item Database.")]
        public async Task NewItem(string Name, string Description, ItemLocation Global = ItemLocation.Vault){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var ItemDb = Database.GetCollection<Item>("Items");
            var User = Context.User as SocketGuildUser;
            ItemDb.EnsureIndex("Name","LOWER($.Name)");

            if(!players.Exists(x => x.DiscordId == Context.User.Id)){
                players.Insert(new player(){
                    DiscordId = Context.User.Id,
                    Character = null
                });
            }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (Global == ItemLocation.Global && !User.GuildPermissions.ManageMessages){
                await ReplyAndDeleteAsync(User.Mention+", You can't add items to the global database! Talk to your GM to add this item there.",timeout: TimeSpan.FromSeconds(5));
                return;
            }

            if (Global == ItemLocation.Vault && plr.ItemVault.Exists(x =>x.Name == Name.ToLower())){
                var item = plr.ItemVault.Find(x =>x.Name == Name.ToLower());
                var index = plr.ItemVault.IndexOf(item);
                item.Description = Description;
                plr.ItemVault[index] = item;
                players.Update(plr);
                await ReplyAsync(Context.User.Mention+", Updated the item **"+item.Name+"** on your Item Vault.");
            }
            
            else if (Global == ItemLocation.Global && ItemDb.Exists(x =>x.Name == Name.ToLower()) && User.GuildPermissions.ManageMessages){
                var item = ItemDb.FindOne(x =>x.Name == Name.ToLower());
                item.Description = Description;
                ItemDb.Update(item);
                await ReplyAsync(Context.User.Mention+", Updated the item **"+item.Name+"** on The global Item Database.");
            }
            else if(Global == ItemLocation.Vault && !plr.ItemVault.Exists(x =>x.Name == Name.ToLower())){
                var item = new Item(){
                    Name = Name,
                    Description = Description
                };
                plr.ItemVault.Add(item);
                players.Update(plr);
                await ReplyAsync(Context.User.Mention+", Added the item **"+item.Name+"** to your Item Vault.");
            }
            else if(Global == ItemLocation.Global && !ItemDb.Exists(x =>x.Name == Name.ToLower()) && User.GuildPermissions.ManageMessages){
                var item = new Item(){
                    Name = Name,
                    Description = Description
                };
                ItemDb.Insert(item);
                await ReplyAsync(Context.User.Mention+", Added the item **"+item.Name+"** to your Item Vault.");
            }
            await Context.Message.DeleteAsync();
        }
        [Command(".DeleteItem"),Alias("RemoveItem","DelItem","RemItem")]
        [Summary("Removes an Item from your Item Vault. Usage: `.RemItem Name` \n**For DMs**: add 'Global' after the description to add the item to the global Item Database.")]
        public async Task DelItem(string Name, ItemLocation Global = ItemLocation.Vault){
            var players = Database.GetCollection<player>("Players");
            var ItemDb = Database.GetCollection<Item>("Items");
            var User = Context.User as SocketGuildUser;
            ItemDb.EnsureIndex("Name","LOWER($.Name)");

            if(!players.Exists(x => x.DiscordId == Context.User.Id)){
                players.Insert(new player(){
                    DiscordId = Context.User.Id,
                    Character = null
                });
            }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (Global == ItemLocation.Global && !User.GuildPermissions.ManageMessages){
                await ReplyAndDeleteAsync(User.Mention+", You can't delete items from global database! Only GMs can do that.",timeout: TimeSpan.FromSeconds(5));
                return;
            }
            else if (Global == ItemLocation.Vault && plr.ItemVault.Exists(x =>x.Name == Name.ToLower())){
                var item = plr.ItemVault.Find(x => x.Name.ToLower() == Name.ToLower());
                plr.ItemVault.Remove(item);
                players.Update(plr);
                await ReplyAsync(User.Mention+", Deleted the item **"+item.Name+"** from your Item Vault.");
                await Context.Message.DeleteAsync();
                return;
            }
            else if (Global == ItemLocation.Global && ItemDb.Exists(x =>x.Name == Name.ToLower())){
                var item = ItemDb.FindOne(x => x.Name == Name.ToLower());
                ItemDb.Delete(item.Id);
                await ReplyAsync(User.Mention+", Deleted the item **"+item.Name+"** from the item Database.");
                await Context.Message.DeleteAsync();
                return;
            }
            await ReplyAndDeleteAsync(User.Mention+", I couldn't find an item whose name is **"+Name+"**. (It has to be the full name!)",timeout: TimeSpan.FromSeconds(5));
        }
        [Command("UploadItem")]
        [Summary("GM-Only command. Promotes an item to the global database. Usage: `.UploadItem PlayerName ItemName`")]
        public async Task PromoteItem(SocketGuildUser User, [Remainder] string Name){

            if (!User.GuildPermissions.ManageMessages){
                await ReplyAndDeleteAsync(User.Mention+", You can't delete items from global database! Only GMs can do that.",timeout: TimeSpan.FromSeconds(5));
                return;
            }
            var players = Database.GetCollection<player>("Players");
            var ItemDb = Database.GetCollection<Item>("Items");
            ItemDb.EnsureIndex("Name","LOWER($.Name)");

            if(!players.Exists(x => x.DiscordId == User.Id)){
                players.Insert(new player(){
                    DiscordId = Context.User.Id,
                    Character = null
                });
            }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == User.Id);
            if (plr.ItemVault.Exists(x =>x.Name == Name.ToLower())){
                var item = plr.ItemVault.Find(x => x.Name.ToLower() == Name.ToLower());
                ItemDb.Insert(item);
                await ReplyAsync(User.Mention+", item **"+item.Name+"** Has been promoted from "+User.Username+"'s Item vault into the Global Database!");
                await Context.Message.DeleteAsync();
                return;
            }
            await ReplyAndDeleteAsync(User.Mention+", I couldn't find an item whose name is **"+Name+"**. (It has to be the full name!)",timeout: TimeSpan.FromSeconds(5));
        }
        [Command("GetItem")]
        [Summary("Gets an item from either your Item Vault or the Item Database. Usage: `.GetItem Name Amount Global/Vault`. Global/Vault defaults to 'Global'.")]
        public async Task GetITem(string Name, int Amount = 1, ItemLocation Global = ItemLocation.Global){
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            var ItemDb = Database.GetCollection<Item>("Items");
            ItemDb.EnsureIndex("Name","LOWER($.Name)");

            if(!players.Exists(x => x.DiscordId == Context.User.Id)){
                players.Insert(new player(){
                    DiscordId = Context.User.Id,
                    Character = null
                });
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
            Item item = null;
            if (Global == ItemLocation.Vault){
                var Query = plr.ItemVault.Where(x => x.Name.ToLower().StartsWith(Name));
                if (Query.Count() == 0){
                    await ReplyAndDeleteAsync(Context.User.Mention+", There isn't an item in your vault whose name starts with '"+Name+"'.",timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else if (Query.Count() > 1){
                string msg = Context.User.Mention+", Multiple items were found! Please specify which one of the following items is the one you're looking for: ";
                foreach (var q in Query)
                {
                    msg += "`" + q.Name + "` ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                return;
                }
                else if (Query.Count() == 1 || Query.ToList().Exists(x=>x.Name.ToLower() == Name.ToLower())){
                    item = Query.First();
                }
            }
            else if (Global == ItemLocation.Global){
                var Query = ItemDb.Find(x => x.Name.ToLower().StartsWith(Name));
                if (Query.Count() == 0){
                    await ReplyAndDeleteAsync(Context.User.Mention+", There isn't an item in the database whose name starts with '"+Name+"'.",timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else if (Query.Count() > 1){
                string msg = Context.User.Mention+", Multiple items were found! Please specify which one of the following items is the one you're looking for: ";
                foreach (var q in Query)
                {
                    msg += "`" + q.Name + "` ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                return;
                }
                else if (Query.Count() == 1 || Query.ToList().Exists(x=>x.Name.ToLower() == Name.ToLower())){
                    item = Query.First();
                }
            }
            if (item == null){
                await ReplyAndDeleteAsync("Something unexpected happened. I couldn't find the item nor detect that I didn't this item. Please contact my creator to let him know of this unexpected error!",false,null,TimeSpan.FromSeconds(8));
                return;
            }
            if (chr.Inventory.Exists(x=> x.Item.Id == item.Id || x.Item.Name.ToLower() == item.Name.ToLower())){
                var index = chr.Inventory.FindIndex(x => x.Item.Id == item.Id || x.Item.Name.ToLower() == item.Name.ToLower());
                chr.Inventory[index].Quantity += Amount;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", you gave "+chr.Name+" "+Amount+" **"+item.Name+"**(s).");
                await Context.Message.DeleteAsync();
                return;
            }
            else {
                chr.Inventory.Add(new PlayerItem(){
                    Item = item,
                    Quantity = Amount
                });
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", you gave "+chr.Name+" "+Amount+" **"+item.Name+"**(s).");
                await Context.Message.DeleteAsync();
                return;
            }
        }
        [Command("Use"),Alias("Consume")]
        [Summary("Use an item from your locked character's inventory. Usage: `.Use ItemName Amount`. Amount defaults to 1")]
        public async Task Use(string Item, int Amount = 1){

        }
    }
}