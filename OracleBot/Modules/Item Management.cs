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
using OracleBot.Modules;
using DiceNotation;

namespace OracleBot.Modules
{
    public class Item_Management : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        public enum ItemLocation {Vault, Global}

        [Command("NewItem")]
        [Summary("Makes a new item and adds it to your local ItemVault. Usage: `.NewItem Name Description`. \n**For DMs**: add 'Global' after the description to add the item to the global Item Database.")]
        public async Task NewItem(string Name, string Description){
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
            
            await Context.Message.DeleteAsync();
        }
        [Command("Use"),Alias("Consume")]
        [Summary("Use an item from your locked character's inventory. Usage: `.Use ItemName Amount`. Amount defaults to 1")]
        public async Task Use(string Name, int Amount = 1){
            Amount = Math.Abs(Amount);
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
                .Include(x => x.Character.Inventory)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (plr.Character == null){
                await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                return;
            }
            var chr = plr.Character;
            var Query = chr.Inventory.Where(x=>x.Name.ToLower().StartsWith(Name.ToLower()));
            if (Query.Count() > 1 && !Query.ToList().Exists(x =>x.Name.ToLower() == Name.ToLower())){
                string msg = Context.User.Mention+", Multiple items were found! Please specify which one of the following items is the one you're looking for: ";
                foreach (var q in Query)
                {
                    msg += "`" + q.Name + "`, ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                return;
            }
            if (Query.Count() == 0){
                    await ReplyAndDeleteAsync(Context.User.Mention+", There isn't an item in the database whose name starts with '"+Name+"'.",timeout: TimeSpan.FromSeconds(5));
                    return;
                }
            else if (Query.Count() == 1 || Query.ToList().Exists(x=>x.Name.ToLower() == Name.ToLower())){
                    var item = Query.First();
                    if(item.Type != ItemType.Miscellanous) {
                        var msg2 = Context.User.Mention+", "+chr.Name+" used their **"+item.Name+"**.";
                        await ReplyAsync(msg2);
                        await Context.Message.DeleteAsync();
                        return;
                    }
                    else if (item.Type == ItemType.Consumable){
                        if (item.Quantity - Amount < 0){
                            await ReplyAndDeleteAsync(Context.User.Mention+", "+chr.Name+" doesn't have that many "+item.Name+"(s).",timeout: TimeSpan.FromSeconds(5));
                            return;
                        }
                        var index = chr.Inventory.IndexOf(item);
                        chr.Inventory[index].Quantity -= Amount;
                        if (chr.Inventory[index].Quantity == 0)
                        {
                            string msg1 = Context.User.Mention+", "+chr.Name+" used up the remaining "+ Amount +" of their **"+item.Name+"**.";
                            await ReplyAsync(msg1);
                        }
                        await ReplyAsync(Context.User.Mention+", "+chr.Name+" used up "+ Amount +" of their **"+item.Name+"**.");
                    }
                    
                    col.Update(chr);
                }
        }
        [Command("GetMoney"), Alias("HolaHolaGetHellaDolla")]
        [Summary("Gives/Takes an amount of money. Usage: `.GetMoney Amount Character` Only GMs can give/take money from other players. Use Negative numbers to substract money.")]
        public async Task HolaHolaGetHellaDolla(double amount, [Remainder]string Name = ""){
            if (amount == 0) return;
            amount = Math.Round(amount,2);
            var players = Database.GetCollection<player>("Players");
            var col = Database.GetCollection<Character>("Characters");
            if (!players.Exists(x => x.DiscordId == Context.User.Id)){
                await ReplyAndDeleteAsync(Context.User.Mention+", you've never made any character so I can't find your character! Please make one with `.newchar Name`!", timeout: TimeSpan.FromSeconds(5));                    return;
            }
            var plr = players
                .Include(x => x.Character)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (Name != ""){
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
                    chr.Money += amount;
                    col.Update(chr);
                    if (amount > 0) await ReplyAsync(Context.User.Mention+", **"+chr.Name+"** obtained $"+amount+".");
                    if (amount < 0) await ReplyAsync(Context.User.Mention+", **"+chr.Name+"** lost $"+amount+".");
                    await Context.Message.DeleteAsync();
                }
            }
            
            if (plr.Character == null){
                await ReplyAndDeleteAsync(Context.User.Mention+", you're not locked to a character! Use `.lock Character_Name` to lock into a character.",false,null,TimeSpan.FromSeconds(5));
                return;
            }
            else{
                var chr = plr.Character;
                chr.Money += amount;
                col.Update(chr);
                if (amount > 0) await ReplyAsync(Context.User.Mention+", **"+chr.Name+"** obtained $"+amount+".");
                if (amount < 0) await ReplyAsync(Context.User.Mention+", **"+chr.Name+"** lost $"+Math.Abs(amount)+".");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("Spend")]
        [Summary("Spends your locked character's money. Usage: `.Spend Amount`")]
        public async Task Spend(double Amount){
            Amount = Math.Round(Math.Abs(Amount),2);

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
                if (chr.Money-Amount < 0)
                {
                    await ReplyAndDeleteAsync(Context.User.Mention+", "+chr.Name+" doesn't have this much money!", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                chr.Money -= Amount;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", "+chr.Name+" spent $"+Amount+"!");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("Inventory"),Alias("I","Bag")]
        [Summary("Displays the contents of your character's inventory in detail. Usage: `.Inventory`")]
        public async Task Inventory(){
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
                var embed = new EmbedBuilder()
                    .WithTitle(chr.Name+"'s Inventory")
                    .WithColor(chr.Color[0],chr.Color[1],chr.Color[2])
                    .WithDescription("Money: $"+chr.Money);
                var sb = new StringBuilder();
                foreach (var x in chr.Inventory){
                    if (MacroProcessor.IsReference(x.Description)){
                        string desc = MacroProcessor.MacroReference(x.Description,chr);
                        embed.AddField(x.Name + " x"+x.Quantity,desc);
                    }
                    else {
                        embed.AddField(x.Name + " x"+x.Quantity,x.Description);
                    }
                }
                await ReplyAsync("",false,embed.Build());
                await Context.Message.DeleteAsync();
            }
        }
    }
}