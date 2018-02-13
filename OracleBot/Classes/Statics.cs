using LiteDB;
using System;
using System.Text;
using Discord.Commands;
using Discord;
using OracleBot.Classes;
using System.Collections.Generic;
using System.Linq;

namespace OracleBot.Classes
{
    public static class Statics
    {
        public static string ToRoman(int number)
        {
            if ((number < 0) || (number > 3999)) throw new ArgumentOutOfRangeException("insert value betwheen 1 and 3999");
            if (number < 1) return string.Empty;
            if (number >= 1000) return "M" + ToRoman(number - 1000);
            if (number >= 900) return "CM" + ToRoman(number - 900); //EDIT: i've typed 400 instead 900
            if (number >= 500) return "D" + ToRoman(number - 500);
            if (number >= 400) return "CD" + ToRoman(number - 400);
            if (number >= 100) return "C" + ToRoman(number - 100);
            if (number >= 90) return "XC" + ToRoman(number - 90);
            if (number >= 50) return "L" + ToRoman(number - 50);
            if (number >= 40) return "XL" + ToRoman(number - 40);
            if (number >= 10) return "X" + ToRoman(number - 10);
            if (number >= 9) return "IX" + ToRoman(number - 9);
            if (number >= 5) return "V" + ToRoman(number - 5);
            if (number >= 4) return "IV" + ToRoman(number - 4);
            if (number >= 1) return "I" + ToRoman(number - 1);
            throw new ArgumentOutOfRangeException("something bad happened");
        }
        public static string ParseCoins (decimal money){
            int GP = (int)money;
            decimal SP = money %1.0m;
            return SP+"SP "+GP+"GP";
        }
        public static int ParseFort(int MAG){
            return Convert.ToInt32(Math.Floor(Convert.ToDouble(MAG/4)));
        }
        public static Embed BuildCharacterSheet(Character character, SocketCommandContext Context, LiteDatabase database){
            var Items = database.GetCollection<Item>("Items");
            var sb = new StringBuilder();
                var embed = new EmbedBuilder()
                .WithTitle(character.Name+" the Level **"+character.Level.CurrLevel+"** "+character.Race+" "+character.Class)
                .WithThumbnailUrl(character.Image)
                .WithFooter(Context.Client.GetUser(character.Owner).Username,Context.Client.GetUser(character.Owner).GetAvatarUrl())
                .AddInlineField("Ability Points "+ character.CheckPoints(1),
                    "\\💪 "+character.AbilityScores.MGT.ToString()+" ("+character.BuildStat(Stats.Might)+")\n"+
                    "\\🏃 "+character.AbilityScores.AGI.ToString()+" ("+character.BuildStat(Stats.Agility)+")\n"+
                    "\\🔋 "+character.AbilityScores.CON.ToString()+" ("+character.BuildStat(Stats.Constitution)+")\n"+
                    "\\👁 "+character.AbilityScores.PER.ToString()+" ("+character.BuildStat(Stats.Perception)+")\n"+
                    "\\✨ "+character.AbilityScores.MAG.ToString()+" ("+character.BuildStat(Stats.Magic)+")\n"+
                    "\\🍀 "+character.AbilityScores.LCK.ToString()+" ("+character.BuildStat(Stats.Luck)+")")
                .AddInlineField("Equipment points",
                    "Total \\🛡: "+character.BuildStat(Stats.Protection)+"\nTotal\\✨\\🛡: "+character.BuildStat(Stats.Fortitude)+
                    "\nTotal \\⚔: "+character.BuildStat(Stats.None));

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
                embed.AddField("Skills "+character.CheckPoints(),sb.ToString());
                sb.Clear();
                sb.AppendLine(Statics.ParseCoins(character.Money));
                foreach(var x in character.Inventory){
                    var I = Items.FindById(x.Key);
                    sb.AppendLine("• "+I.Name+" (x"+x.Value+")");
                }
                embed.AddField("Inventory",sb);
                return embed.Build();
        }
        public static Embed BuildSkill(Skill skill){
            var sb = new StringBuilder();
            var embed = new EmbedBuilder()
                .WithTitle(skill.Name+" ["+ToRoman(skill.Level)+"]")
                .WithDescription("["+skill.Target+"] [Cooldown "+skill.Cooldown.MaxTime+" turns]\n"+ skill.Description);
            foreach(var x in skill.Effects){
                sb.AppendLine(EffectBuilder(x));
            }
            if (sb.Length != 0) embed.AddField("Effects",sb.ToString());
            return embed.Build();
        }
        public static string EffectBuilder(Effect effect){
            var sb = new StringBuilder();
            sb.Append("• "+effect.Name);
            if (effect.type == Status.Damage){
                sb.AppendLine("  - Damage ("+effect.Dice+")");
            }
            else if (effect.type == Status.Debuff){
                if (int.Parse(effect.Dice) < 0){
                    sb.AppendLine("  - Debuff ("+effect.Dice+" "+effect.AffectedStat+")");
                    sb.AppendLine("  - Duration: "+effect.Turns+" turns)");
                }
                else{
                    sb.AppendLine("  - Buff (+"+effect.Dice+" "+effect.AffectedStat+")");
                    sb.AppendLine("  - Duration: "+effect.Turns+" turns)");
                }
            }
            else if (effect.type == Status.DmgOverTime){
                sb.AppendLine("  - Damage ("+effect.Dice+" per turn while afflicted)");
            }
            else if (effect.type == Status.ChanceOfSkip){
                sb.AppendLine("  - 50% chance of skiped turn");
            }
            else if(effect.type == Status.Heal){
                sb.AppendLine("  - Heals "+effect.Dice+" HP");
            }
            else if(effect.type == Status.Restraint){
                sb.AppendLine("  - Skips the next "+effect.Dice+" turns");
            }
            else {
                sb.AppendLine("  - Causes a RP-related affliction.");
            }
            return sb.ToString();
        }
        public static Embed EmbedEffect(Effect effect){
            var embed = new EmbedBuilder()
            .WithTitle(effect.Name);
            if (effect.type == Status.Damage){
                embed.AddField("Flat Damage","Damage Dice: "+effect.Dice+
                    "\nDamage Type: "+effect.Description);
            }
            if (effect.type == Status.Debuff){
                if (int.Parse(effect.Dice) < 0){
                    embed.AddField("Stat reduction","Stat being reduced: "+effect.AffectedStat+
                    "\nDuration of the debuff: "+effect.Turns+" turns.\n"+
                    "Intensity of the debuff: "+effect.Dice);
                }
                else{
                    embed.AddField("Stat increment","Stat being augmented: "+effect.AffectedStat+
                    "\nDuration of the buff: "+effect.Turns+" turns.");
                }
            }
            if (effect.type == Status.DmgOverTime){
                embed.AddField("Damage over time","Damage: "+effect.Dice+
                    "\nDamage Type: "+effect.Description +
                    "\nDuration: "+effect.Turns+" turns.");
            }
            if (effect.type == Status.Heal){
                embed.AddField("Healing","Health recovered: "+effect.Dice+" HealthPoints.");
            }
            if (effect.type == Status.Restraint){
                embed.AddField("Prevent's target from acting on combat","Duration: "+effect.Turns+" turns.");
            }
            if (effect.type == Status.ChanceOfSkip){
                embed.AddField("50% Chance of preventing target's turn.","Duration: "+effect.Turns+" turns.");
            }
            if (effect.type == Status.Misc){
                embed.AddField("Special effect",effect.Description);
            }
            return embed.Build();
        }
        public static Embed EffectInfo(){
            var embed = new EmbedBuilder()
            .WithTitle("About Effect Types")
            .WithDescription("Mechanically, effects work in one of 7 ways. This is a brief summary of each one.")
            .AddField("A) Damage/Flat Damage","This type of effect is as simple as it gets. It rolls a user-defined dice roll and applies that damage to its target.")
            .AddField("B) Buff/Debuff","This type of effect applies a buff (if value is positive) or a debuff (if value is negative) to one of the stats of the target.\n"+
            "Note that stats also include things such as Fortitude, Hit chance and Protection.")
            .AddField("C) Damage Over Time","This type of effects deal no initial damage, but leave the target receiving user-defined damage for user-defined duration in turns.\n"+
            "This effect type covers things like poison, burning, lingering magic damage, and anything that does damage over time.")
            .AddField("D) Heal","This effect simply restores a user-defined roll of HealthPoints to the target.")
            .AddField("E) Restraint","This effect skips the target's turn for the duration of the effect. Covers things such as petrify, entanglement and the like.")
            .AddField("F) Chance Of Skip","This one is a peculiar effect. It gives the target a 50% (1d2) to have its turn skipped for the duration of the effect.")
            .AddField("G) Miscellaneus","This type of effect has no effect on the target's stats, but is useful for giving more RP-related effects. Effect expires after an user-defined about of turns.");
            return embed.Build();
        }
    }
}