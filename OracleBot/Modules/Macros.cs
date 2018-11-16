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
    }
public static class MacroProcessor{
        public static string ParseReference(string Reference, Character Character){
            var regex = new Regex(@"\[(.*?)\]");
            string returnstring = Reference;
            var Matches = regex.Matches(Reference.ToLower()).Cast<Match>().Select(match => match.Value).ToList();
            string buffer = "";
            foreach (var x in Matches){
                switch (x.ToLower()){      
                    case "[str]":
                        returnstring =returnstring.Replace(x,Character.AbilityScores[0].GetValue(true),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[dex]":
                        returnstring =returnstring.Replace(x,Character.AbilityScores[1].GetValue(true),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[con]":
                        returnstring =returnstring.Replace(x,Character.AbilityScores[2].GetValue(true),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[int]":
                        returnstring = returnstring.Replace(x,Character.AbilityScores[3].GetValue(true),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[wis]":
                        returnstring = returnstring.Replace(x,Character.AbilityScores[4].GetValue(true),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[cha]":
                        returnstring = returnstring.Replace(x,Character.AbilityScores[5].GetValue(true),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[str-mod]":
                        buffer = Character.AbilityScores[0].GetIntMod().ToString();
                        returnstring = returnstring.Replace(x,buffer,StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[dex-mod]":
                        buffer = Character.AbilityScores[1].GetIntMod().ToString();
                        returnstring = returnstring.Replace(x,buffer,StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[con-mod]":
                        buffer = Character.AbilityScores[2].GetIntMod().ToString();
                        returnstring = returnstring.Replace(x,buffer,StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[int-mod]":
                        buffer = Character.AbilityScores[3].GetIntMod().ToString();
                        returnstring = returnstring.Replace(x,buffer,StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[wis-mod]":
                        buffer = Character.AbilityScores[4].GetIntMod().ToString();
                        returnstring = returnstring.Replace(x,buffer,StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[cha-mod]":
                        buffer = Character.AbilityScores[5].GetIntMod().ToString();
                        returnstring = returnstring.Replace(x,buffer,StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[level]":
                        returnstring = returnstring.Replace(x,Character.Health.Level.ToString(),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[hp]":
                        returnstring = returnstring.Replace(x,Character.Health.GetHealth(Character.AbilityScores[2].GetIntMod().ToString()).ToString(),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[ac]":
                        returnstring = returnstring.Replace(x,(10+Character.ParseArmor()).ToString(),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[bab]":
                        returnstring = returnstring.Replace(x, Character.Health.GetBab().ToString(),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[will]":
                        returnstring = returnstring.Replace(x, Character.Health.GetSave(SavingThrows.Will,Character.AbilityScores).ToString(),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[fortitude]":
                        returnstring = returnstring.Replace(x, Character.Health.GetSave(SavingThrows.Fortitude,Character.AbilityScores).ToString(),StringComparison.OrdinalIgnoreCase);
                        break;
                    case "[reflex]":
                        returnstring = returnstring.Replace(x, Character.Health.GetSave(SavingThrows.Reflex,Character.AbilityScores).ToString(),StringComparison.OrdinalIgnoreCase);
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
            return Regex.IsMatch(raw,@"\[(.*?)\]");
        }
    }
}