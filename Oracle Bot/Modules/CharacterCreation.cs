using System;
using Discord;
using System.Threading.Tasks;
using System.Text;
using Discord.Commands;
using LiteDB;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using OracleBot.Classes;

namespace OracleBot.Modules
{

    public class CharacterCreation : ModuleBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        [Command("Char")]
        public async Task find([Remainder] string Name){
            var col = Database.GetCollection<Character>("Characters");
            var Items = Database.GetCollection<Item>("Items");
            var query = col.Find(x => x.Name.StartsWith(Name.ToLower()));

            if (query.Count() == 0){
                await ReplyAsync("There are no character's whose name starts with \""+Name+"\".");
                return;
            }
            if (query.Count() > 1){
                var sb = new StringBuilder();
                foreach (var x in query){
                    sb.Append(", `"+x.Name+"`");
                }
                await ReplyAsync("Multiple character's names with start with **"+Name+"**."+ 
                "Please specify which one of these character's is the one you're lookig for: "+ 
                sb.ToString().Substring(0,sb.Length -2)+".");
                return;
            }
            else {
                var character = query.FirstOrDefault();
                var sb = new StringBuilder();
                var embed = new EmbedBuilder()
                .WithTitle(character.Name+" the level "+character.Level+" "+character.Class)
                .WithThumbnailUrl(character.Image)
                .WithFooter(Context.Client.GetUser(character.Owner).Username,Context.Client.GetUser(character.Owner).GetAvatarUrl())
                .AddInlineField("Ability Points"+ character.CheckPoints(1),
                    "\\💪 "+character.AbilityScores.MGT.ToString()+" ("+character.BuildStat(Stats.Might)+")\n"+
                    "\\🏃 "+character.AbilityScores.AGI.ToString()+" ("+character.BuildStat(Stats.Agility)+")\n"+
                    "\\🔋 "+character.AbilityScores.CON.ToString()+" ("+character.BuildStat(Stats.Constitution)+")\n"+
                    "\\👁 "+character.AbilityScores.PER.ToString()+" ("+character.BuildStat(Stats.Perception)+")\n"+
                    "\\✨ "+character.AbilityScores.MAG.ToString()+" ("+character.BuildStat(Stats.Magic)+")\n"+
                    "\\🍀 "+character.AbilityScores.LCK.ToString()+" ("+character.BuildStat(Stats.Luck)+")")
                .AddInlineField("Equipment points",
                    "Total \\🛡: "+character.BuildStat(Stats.Protection)+"\n Total\\✨\\🛡: "+character.BuildStat(Stats.Fortitude)+
                    "\nOTotal \\⚔: "+character.BuildStat(Stats.None));
                foreach (var x in character.Equipment){
                    sb.AppendLine("["+x.ItemType+"] "+x.Name+" ("+x.Effects.FirstOrDefault().Dice+")");
                }
                if (sb.Length == 0) sb.Append("This character has no gear");
                embed.AddInlineField("Gear",sb.ToString());
                sb.Clear();
                foreach (var x in character.Aliments){
                    sb.AppendLine("• "+ x.Name+" ["+x.Turns+" turns]");
                }
                if (sb.Length == 0) sb.Append("This character has no aliments or afflictions");
                embed.AddInlineField("Status Aliments",sb.ToString());
                foreach (var x in character.Traits){
                    sb.AppendLine("• "+x.Name);
                }
                if (sb.Length == 0) sb.Append("This character has no traits.");
                embed.AddInlineField("Traits",sb.ToString());
                sb.Clear();
                foreach (var x in character.Skill){
                    sb.AppendLine("• "+x.Name+"["+Statics.ToRoman(x.Level)+"]");
                }
                if (sb.Length == 0) sb.Append("This character has no Skills.");
                embed.AddField("Skills",sb.ToString());
                sb.Clear();
                sb.AppendLine(Statics.ParseCoins(character.Money));
                foreach(var x in character.Inventory){
                    var I = Items.FindById(x.Key);
                    sb.AppendLine("• "+I.Name+" (x"+x.Value+")");
                }
                embed.AddField("Inventory",sb);

                
            }
        }
    }
}