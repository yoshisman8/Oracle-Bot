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
    }
}