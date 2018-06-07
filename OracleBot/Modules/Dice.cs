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

namespace OracleBot.Modules
{
    public class Dice : InteractiveBase<SocketCommandContext>
    {
        IDiceParser parser = new DiceParser();
        public LiteDatabase Database {get;set;}

        [Command("Roll"), Alias("r")]
        [Summary("Rolls a die on a xdy expression format. \nUsage: `.Roll <dice expression>`.")]
        public async Task DieRoll([Remainder]string input = "d20")
        {
            string[] sinput = new string[0];
            if (input.Contains(">>")){
                sinput = input.Split(">>",StringSplitOptions.RemoveEmptyEntries);
                if (sinput.Length == 1){
                    await ReplyAsync("You didn't add a conditional!");
                    return;
                }
                if (sinput.Length > 2){
                    await ReplyAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sinput[1], out int n)){
                    await ReplyAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sinput[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sinput[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sinput[0].ToLower()).Roll();
                string steps = "";
                var successes = result.Results.Where(x => x.Value >= conditional);
                foreach (var x in result.Results){
                    if (x.Value >= conditional){
                        steps += "**"+ x.Value + "**, ";
                    }
                    else{
                        steps += x.Value + ", ";
                    }
                }
                steps = steps.Substring(0,steps.Length - 2);
                if (successes.Count() == 0){
                    await ReplyAsync(Context.User.Mention+" has **failed** all their rolls! ("+steps+")");
                    return;
                }
                else {
                    await ReplyAsync(Context.User.Mention+" has **succeeded "+successes.Count()+" times.** ("+steps+")");
                    return;
                }
            }

            if (input.Contains(">")){
                sinput = input.Split(">",StringSplitOptions.RemoveEmptyEntries);
                if (sinput.Length == 1){
                    await ReplyAsync("You didn't add a conditional!");
                    return;
                }
                if (sinput.Length > 2){
                    await ReplyAsync("You can't add more than one conditional!");
                    return;
                }
                if (!int.TryParse(sinput[1], out int n)){
                    await ReplyAsync("This expression has an invalid/non-numeric conditional!");
                    return;
                }
                int conditional = int.Parse(sinput[1]);
                var valid = System.Text.RegularExpressions.Regex.IsMatch(sinput[0].ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
                if (!valid){
                    await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                    return;
                }
                var result = parser.Parse(sinput[0].ToLower()).Roll();
                string steps = "";
                foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
                steps = steps.Substring(0,steps.Length - 2);
                if (result.Value < conditional){
                    await ReplyAsync(Context.User.Mention+" has **failed**. (Total: "+result.Value+", lower than "+conditional+"; "+steps+")");
                    return;
                }
                else {
                    await ReplyAsync(Context.User.Mention+" has **succeeded**. "+ "(Total: "+result.Value+", greater than "+conditional+"; "+steps+")");
                    return;
                }
            }

            else {
            var valid = System.Text.RegularExpressions.Regex.IsMatch(input.ToLower(), @"^[d-dk-k0-9\+\-\s\*]*$");
            if (!valid){
                await ReplyAsync(Context.User.Mention+" This is not a valid dice expression!");
                return;
            }
            var result = parser.Parse(input.ToLower()).Roll();
            string steps = "";
            foreach(var x in result.Results){
                if (x.Scalar == -1){
                    steps += "-"+x.Value + " + ";
                }
                else if (x.Scalar >= 2 || x.Scalar <= -2){
                    steps += x.Value+"x"+x.Scalar + " + ";
                }
                else {
                steps += x.Value + " + ";
                }
            }
            steps = steps.Substring(0,steps.Length - 2).Replace(" + -", " - ");
            await Context.Channel.SendMessageAsync(Context.User.Mention + ", Your Roll: " + steps + " = **" + result.Value+"**.");
            }
        }
        [Command("SkillCheck"), Alias("SC")]
        [Summary("Rolls a skill check for your locked character. Usage: `.SC Skill_Name`")]
        public async Task SkillCheck([Remainder] string Name){
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
                if (!chr.Skills.Exists(x => x.Name.ToLower().StartsWith(Name.ToLower()))){
                    await ReplyAndDeleteAsync(Context.User.Mention+", "+chr.Name+" doesn't have a skill that starts with "+Name+".", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                if (chr.Skills.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).Count() > 1 && !chr.Skills.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var result = chr.Skills.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    var sb = new StringBuilder().Append(Context.User.Mention+", "+chr.Name+" has more than " + result.Count()+ "skills that starts with the word **"+Name+"**. Please specify which one from this list is the one you want to remove by using said skill's full name: ");
                    foreach (var x in result){
                        sb.Append("`"+x.Name+"`, ");
                    }
                    await ReplyAndDeleteAsync(sb.ToString().Substring(0,sb.Length-2), timeout: TimeSpan.FromSeconds(10));
                }
                else{
                    var skill = chr.Skills.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    string mod = (chr.AbilityScores[(int)skill.Ability].GetMod(true));
                    string Prof = ((int)skill.Proficiency*chr.Profiency).ToString();
                    var result = parser.Parse("1d20 + "+mod+"+"+Prof).Roll();
                    await ReplyAsync(Context.User.Mention+", "+chr.Name+" rolled a **"+result.Value+"** ("+result.Results[0].Value+"+"+mod+"+"+Prof+") on their "+skill.Name+" Check");
                    await Context.Message.DeleteAsync();                    
                }
            }
        }
        [Command("SavingThrow"), Alias("ST")]
        [Summary("Rolls a skill check for your locked character. Usage: `.ST Ability`")]
        public async Task SavingThrow(AbilityShort Score){
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
                string Mod = chr.AbilityScores[(int)Score].GetMod(true);
                string Prof = ((int)chr.AbilityScores[(int)Score].Proficient*chr.Profiency).ToString();
                var result = parser.Parse("1d20 + "+ Mod +" + "+Prof).Roll();
                await ReplyAsync(Context.User.Mention+", "+chr.Name+" Rolled a **"+result.Value+"** ("+result.Results[0].Value+"+"+Mod+"+"+Prof+") on their "+Score+" Saving Throw.");
                await Context.Message.DeleteAsync();
            }
        }

        [Command("GenerateAbilityScores"),Alias("RollAbilityScores", "GenerateAB","RollAB")]
        [Summary("Rolls Five values that can be used as Ability Scores.")]
        public async Task Rollstats(){
            var sb = new StringBuilder();
            for (int i = 0; i <5 ; i++){
                var result = parser.Parse("4d6k3").Roll();
                sb.Append("[**"+result.Value+"**] ");
            }
            await ReplyAsync(Context.User.Mention+", Here are your 5 Ability Scores: "+sb.ToString());
            await Context.Message.DeleteAsync();
        }
    }
}