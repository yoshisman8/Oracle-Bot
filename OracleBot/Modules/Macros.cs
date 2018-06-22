using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;
using OracleBot.Classes;
using DiceNotation;
using SimpleExpressionEvaluator;
using static OracleBot.Modules.Item_Management;

namespace OracleBot.Modules
{
    public class Macros : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        [Command("TestMacro")]
        public async Task TestMacro([Remainder] string Macro){
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
                if (!MacroProcessor.IsMacro(Macro)){ await ReplyAndDeleteAsync("This macro isn't valid!",timeout: TimeSpan.FromSeconds(5)); return;}
                var result = MacroProcessor.MacroRoll(Macro,chr).Roll();
                await ReplyAsync(Context.User.Mention+", "+chr.Name+" Rolled a **"+result.Value+"**.");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("AttackMacro"), Alias("SetAttackMacro")]
        [Summary("Sets the macro for one of your character's Attacks. usage: `.AttackMacro Name Macro`. If you want to remove the macro from the attack, just use the command without any macro (ie: `.AttackMacro Punch`) to rest it.")]
        public async Task MeleeAttack(string Name, [Remainder] string Macro = ""){
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
                if (!chr.Attacks.Exists(x => x.Name.ToLower().StartsWith(Name.ToLower()))){
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
                    var attack = chr.Attacks.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    int index = chr.Attacks.IndexOf(attack);
                    attack.Macro = Macro;
                    chr.Attacks[index] = attack;
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", the attack **"+attack.Name+"** now has its own macro!");
                }
                await Context.Message.DeleteAsync();
            }
        }
        [Command("ItemMacro")]
        [Summary("Sets the macro for one of your character's Attacks. usage: `.AttackMacro Name ItemLocation Macro`. Item Location is either 'Vault' or 'Global'. Only DMs can change global items. If you want to remove the macro from the attack, just use the command without any macro (ie: `.ItemMacro Potion Vault`) to rest it.")]
        public async Task DelItem(string Name, ItemLocation Global, [Remainder] string Macro = ""){
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
                .Include(x => x.ItemVault)
                .Include(x => x.Character.AbilityScores) .Include(x => x.Character.Skills)
                .FindOne(x => x.DiscordId == Context.User.Id);
            if (Global == ItemLocation.Global && !User.GuildPermissions.ManageMessages){
                await ReplyAndDeleteAsync(User.Mention+", You can't set macros on items from global database! Only GMs can do that.",timeout: TimeSpan.FromSeconds(5));
                return;
            }
            if (Global == ItemLocation.Vault){
                var Query = plr.ItemVault.Where(x => x.Name.ToLower().StartsWith(Name));
                if (Query.Count() == 0){
                    await ReplyAndDeleteAsync(Context.User.Mention+", There isn't an item in your vault whose name starts with '"+Name+"'.",timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else if (Query.Count() > 1 && !Query.ToList().Exists(x => x.Name.ToLower() == Name.ToLower())){
                string msg = Context.User.Mention+", Multiple items were found! Please specify which one of the following items is the one you're looking for: ";
                foreach (var q in Query)
                {
                    msg += "`" + q.Name + "`, ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                return;
                }
                else if (Query.Count() == 1 || Query.ToList().Exists(x=>x.Name.ToLower() == Name.ToLower())){
                    var item = Query.First();
                    var index = plr.ItemVault.IndexOf(item);
                    plr.ItemVault[index].Macro = Macro;
                    players.Update(plr);
                    await ReplyAsync(User.Mention+", the item **"+item.Name+"** from your Item Vault now has an assigned macro.");
                    await Context.Message.DeleteAsync();
                    return;
                }
            }
            else if (Global == ItemLocation.Global){
                var Query = ItemDb.Find(x => x.Name.ToLower().StartsWith(Name));
                if (Query.Count() == 0){
                    await ReplyAndDeleteAsync(Context.User.Mention+", There isn't an item in the database whose name starts with '"+Name+"'.",timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                else if (Query.Count() > 1 && !Query.ToList().Exists(x => x.Name.ToLower() == Name.ToLower())){
                string msg = Context.User.Mention+", Multiple items were found! Please specify which one of the following items is the one you're looking for: ";
                foreach (var q in Query)
                {
                    msg += "`" + q.Name + "`, ";
                }
                await ReplyAndDeleteAsync(msg.Substring(0,msg.Length-2), timeout: TimeSpan.FromSeconds(10));
                return;
                }
                else if (Query.Count() == 1 || Query.ToList().Exists(x=>x.Name.ToLower() == Name.ToLower())){
                    var item = Query.First();
                    item.Macro = Macro;
                    ItemDb.Update(item);
                    await ReplyAsync(User.Mention+", the item **"+item.Name+"** from the item Database now has an assigned macro.");
                    await Context.Message.DeleteAsync();
                    return;
                }
            }
            await ReplyAndDeleteAsync(User.Mention+", I couldn't find an item whose name is **"+Name+"**. (It has to be the full name!)",timeout: TimeSpan.FromSeconds(5));
        }
    }

    public static class MacroProcessor{
        public static string ParseReference(string Reference, Character Character){
            var regex = new Regex(@"\[(.*?)\]");
            string returnstring = Reference;
            var Matches = regex.Matches(Reference).Cast<Match>().Select(match => match.Value).ToList();
            string buffer = "";
            foreach (var x in Matches){
                switch (x){      
                    case "[str]":
                        returnstring =returnstring.Replace(x,Character.AbilityScores[0].GetValue(true));
                        break;
                    case "[dex]":
                        returnstring =returnstring.Replace(x,Character.AbilityScores[1].GetValue(true));
                        break;
                    case "[con]":
                        returnstring =returnstring.Replace(x,Character.AbilityScores[2].GetValue(true));
                        break;
                    case "[int]":
                        returnstring = returnstring.Replace(x,Character.AbilityScores[3].GetValue(true));
                        break;
                    case "[wis]":
                        returnstring = returnstring.Replace(x,Character.AbilityScores[4].GetValue(true));
                        break;
                    case "[prof]":
                        returnstring= returnstring.Replace(x,Character.Profiency.ToString());
                        break;
                    case "[str-mod]":
                        buffer = Character.AbilityScores[0].GetMod(true);
                        if (int.Parse(buffer) < 0) buffer = "("+buffer+")";
                        returnstring = returnstring.Replace(x,buffer);
                        break;
                    case "[dex-mod]":
                        buffer = Character.AbilityScores[1].GetMod(true);
                        if (int.Parse(buffer) < 0) buffer = "("+buffer+")";
                        returnstring = returnstring.Replace(x,buffer);
                        break;
                    case "[con-mod]":
                        buffer = Character.AbilityScores[2].GetMod(true);
                        if (int.Parse(buffer) < 0) buffer = "("+buffer+")";
                        returnstring = returnstring.Replace(x,buffer);
                        break;
                    case "[int-mod]":
                        buffer = Character.AbilityScores[3].GetMod(true);
                        if (int.Parse(buffer) < 0) buffer = "("+buffer+")";
                        returnstring = returnstring.Replace(x,buffer);
                        break;
                    case "[wis-mod]":
                        buffer = Character.AbilityScores[4].GetMod(true);
                        if (int.Parse(buffer) < 0) buffer = "("+buffer+")";
                        returnstring = returnstring.Replace(x,buffer);
                        break;
                    case "[level]":
                        returnstring = returnstring.Replace(x,Character.Health.Level.ToString());
                        break;
                    case "[hp]":
                        returnstring = returnstring.Replace(x,Character.Health.GetHealth(Character.AbilityScores[2].GetValue(true)).ToString());
                        break;
                    case "[ac]":
                        returnstring = returnstring.Replace(x,Character.ArmorClass.ToString());
                        break;
                    default:
                        returnstring = returnstring.Replace(x, "");
                        break;
                }
            }
            return returnstring;
        }
        public static DiceExpression MacroRoll(string raw, Character Character){
            DiceParser Parser = new DiceParser();
            raw = raw.ToLower();
            var Matches = Regex.Matches(raw,@"([dD0-9\+\s\-]*?)(\{(\[.*\])\})");
            var Reference = Matches[0].Groups[2].Value;
            var Eva = new ExpressionEvaluator();
            string pref = ParseReference(Matches[0].Groups[3].Value,Character);
            raw = raw.Replace(Reference,Eva.Evaluate(pref).ToString());
            var roller = new DiceParser();
            var roll = roller.Parse(raw);
            return roll;
        }
        public static string MacroReference(string raw,Character Character){
            raw = raw.ToLower();
            var Matches = Regex.Matches(raw,@"\{(.*?)\}");
            var Eva = new ExpressionEvaluator();
            foreach (Match x in Matches){
                var Reference = x.Value;
                string pref = ParseReference(x.Groups[1].Value,Character);
                raw = raw.Replace(Reference,Eva.Evaluate(pref).ToString());
            }        
            return raw;
        }
        public static bool IsMacro(string raw){
            if (Regex.IsMatch(raw,@"([dD0-9\+\s\-]*?)(\{(\[.*\])\})")) return true;
            else return false;
        }
        public static bool IsReference(string raw){
            return Regex.IsMatch(raw,@"\{(.*?)\}");
        }
    }
}