using System;
using Discord;
using System.Threading.Tasks;
using System.Text;
using Discord.Commands;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using LiteDB;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using OracleBot.Classes;

namespace OracleBot.Modules
{

    public class CharacterCreation : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        [Command("Character"), Alias("C", "Char")]
        [Summary("Usage: `.Char <Name>`")]
        public async Task find([Remainder] string Name){
            var col = Database.GetCollection<Character>("Characters");
            var Items = Database.GetCollection<Item>("Items");
            var query = col.Include(x => x.Equipment)
            .Find(x => x.Name.StartsWith(Name.ToLower()));

            if (query.Count() == 0){
                await ReplyAsync("There are no character's whose name starts with \""+Name+"\".");
                return;
            }
            if (query.Count() >= 2){
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
                await ReplyAsync("",embed: Statics.BuildCharacterSheet(character,Context,Database));
            }
        }
        [Command("Character"), Alias("C", "Char")]
        public async Task GetCurrChar(){
            var C = PlayerLocking.GetLock(Database,Context.User.Id);
            if (C == null){
                await ReplyAsync("You're not locked to any character!");
                return;
            }
            else{
                await ReplyAsync("", embed: Statics.BuildCharacterSheet(C,Context,Database));
            }
        }
        [Command("AddChar"), Alias("Add-char", "create-char","Newchar")]
        [Summary("Usage: .Addchar <Name> [Race] [Class]`")]
        public async Task Create(string Name, string Race = "Racially undefined", string Class = "Wanderer"){
            var col = Database.GetCollection<Character>("Characters");

            if (col.Exists(x =>x.Name == Name.ToLower())){
                await ReplyAsync("There's already a character with that name on the database, pick something else!");
                return;
            }
            Character Char = new Character(){
                Name = Name,
                Class = Class,
                Race = Race
            };
            col.Insert(Char);
            col.EnsureIndex("Name", "LOWER($.Name)");
            PlayerLocking.LockPayer(Char,Context.User.Id,Database);
            var msg = await ReplyAsync("Character **"+Name+"** Added to the Database.\n"+
            "You've been given 18 stat points and 1 skill point. Use `.StatUp Stat_To_Increase Ammount` and `.AddSkill Skill_Name` to use said points.");
        }
        [Command("DeleteCharacter"), Alias("Delchar","Del-char","RemChar","RemoveCharacter")]
        public async Task Test(string Name){
            var col = Database.GetCollection<Character>("Characters");
            var query = col.Include(x => x.Equipment)
            .Find(x => x.Name.StartsWith(Name.ToLower()));
            SocketGuildUser User = Context.User as SocketGuildUser;
            if (query.Count() == 0){
                await ReplyAsync("There are no character's whose name starts with \""+Name+"\".");
                return;
            }
            if (query.Count() >= 2){
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
                if (character.Owner == Context.User.Id || User.GuildPermissions.ManageMessages){
                    col.Delete(character.Id);
                    await ReplyAsync("Character **"+character.Name+"** deleted from the database.");
                }
                else {
                    await ReplyAsync("This isn't your character, you can't delete it!");
                }
            }
        }
        [Command("Lock")]
        public async Task Lock(string Name){
            var col = Database.GetCollection<Character>("Characters");
            var query = col.Include(x => x.Equipment)
            .Find(x => x.Name.StartsWith(Name.ToLower()));
            SocketGuildUser User = Context.User as SocketGuildUser;
            if (query.Count() == 0){
                await ReplyAsync("There are no character's whose name starts with \""+Name+"\".");
                return;
            }
            if (query.Count() >= 2){
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
                if (character.Owner == Context.User.Id || User.GuildPermissions.ManageMessages){
                    PlayerLocking.LockPayer(character,Context.User.Id,Database);
                    await ReplyAsync("You've been locked to character **"+character.Name+"**.");
                }
                else {
                    await ReplyAsync("This isn't your character, you can't lock it!");
                }
            }
        }
        [Command("StatUp")]
        public async Task StatUp(string Stat, int Amount){
            var C = PlayerLocking.GetLock(Database,Context.User.Id);
            if (C == null){
                await ReplyAsync("You're not locked to any character!");
                return;
            }
            else{
                var S = Stats.None;
                if (Amount == 0){
                    await ReplyAndDeleteAsync("You can't raise or decrease a stat by 0 you silly!", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                if (C.Level.StatPoints < Amount) {
                    await ReplyAsync("You don't have this many stat points!");
                    return;
                }
                if (Stat.ToLower() == "mgt"){
                    C.AbilityScores.MGT+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Might;
                }
                else if (Stat.ToLower() == "agi"){
                    C.AbilityScores.AGI+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Agility;
                }
                else if (Stat.ToLower() == "con"){
                    C.AbilityScores.CON+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Constitution;
                }
                else if (Stat.ToLower() == "per"){
                    C.AbilityScores.PER+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Perception;
                }
                else if (Stat.ToLower() == "mag"){
                    C.AbilityScores.MAG+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Magic;
                }
                else if (Stat.ToLower() == "lck"){
                    C.AbilityScores.LCK+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Luck;
                }
                else {
                    await ReplyAsync("This isn't a valid stat! Stats are `MGT`, `AGI`, `CON`, `PER`, `MAG` and `LCK`");
                    return;
                }
                var col = Database.GetCollection<Character>("Characters");
                if (Amount < 0){
                    col.Update(C);
                    await ReplyAsync("Decreased "+C.Name+"'s "+S.ToString()+" by "+Amount+".");
                }
                if (Amount > 0){
                    col.Update(C);
                    await ReplyAsync("Increased "+C.Name+"'s "+S.ToString()+" by "+Amount+".");
                }
            }
        }
        public async Task SetImage([Remainder]string url){
            var character = PlayerLocking.GetLock(Database,Context.User.Id);
            if (character == null){
                await ReplyAndDeleteAsync("You're not locked to any character!");
                return;
            }
            else {
                character.Image = url;
                character.Update(Database);
                await ReplyAsync("Character image changed successfully!");
            }
        }
        [Command("AddSkill"), Alias("Add-Skill","NewSkill","New-Skill","Learn-Skill", "LearnSkill")]
        public async Task CreateSkill(string Skill_Name = ""){
            var character = PlayerLocking.GetLock(Database,Context.User.Id);
            if (character == null){
                await ReplyAndDeleteAsync("You're not locked to any character!");
                return;
            }
            else {
                if (character.Level.SkillPoints < 1){
                    await ReplyAndDeleteAsync("You don't have any skill points to spend!");
                    return;
                }
                var skill = new Skill(){
                    Name = Skill_Name
                };
                var msg = await ReplyAsync("Starting skill creation menu...");
                for (bool A = true; A == false;){
                    if (skill.Name == ""){
                        for (bool A1 = true; A1 == false;){
                            await msg.ModifyAsync(x => x.Content = "It seems you left the Skill name empty. What's the name for this skill? (You can say \"Cancel\" to stop creating the this skill at any time.");
                            var Areply = await NextMessageAsync();
                            if (Areply.Content.ToLower() == "cancel") return;
                            skill.Name = Areply.Content;
                            await Areply.DeleteAsync();
                            A1 = false;
                        }
                    }
                    await msg.ModifyAsync(x => x.Content = "Let's get started! Down bellow is an embed with your skill, this will change as the skill is being created.\n"+
                        "First things first: Does this spell has a **Single** target, **Multiple** or **All** targets, or does it affect **Self**?\n"+
                        "(Please reply below, you can say \"Cancel\" at any time to cancel the skill creation.");
                    await msg.ModifyAsync(x => x.Embed = Statics.BuildSkill(skill));
                    var reply = await NextMessageAsync();
                    if (reply.Content.ToLower() == "cancel") return;
                    if(reply.Content.ToLower() == "single"){
                            skill.Target = Target.Single;
                            A = false;
                            await reply.DeleteAsync();
                    }
                    if(reply.Content.ToLower() == "multiple" || reply.Content.ToLower() == "all"){
                            skill.Target = Target.All;
                            A = false;
                            await reply.DeleteAsync();
                    }
                    if(reply.Content.ToLower() == "self"){
                            skill.Target = Target.Self;
                            A = false;
                            await reply.DeleteAsync();
                    }
                    await reply.DeleteAsync();
                    if (A == false){
                        await msg.ModifyAsync(x => x.Content= "Do you want to add a description to your skill? Write it down now or respond \"Skip\" to not add one.");
                        reply = await NextMessageAsync();
                        if (reply.Content.ToLower() == "cancel") return;
                        if (reply.Content.ToLower() == "skip") {}
                        else { skill.Description = reply.Content;} 
                        await reply.DeleteAsync();
                    }
                }
                bool B = true;
                while (B){
                    await msg.ModifyAsync(x => x.Content = "Do you want to add an Effect to this skill? Reply 'add' to add one or 'done' if you're done adding effects.\n"+
                        "About effects: Effects are what make skills, items, armor, ect affect your (or someone else's) character.\n"+
                        "Unless your skill doesn't do anything you should always have at least 1 effect.");
                    await msg.ModifyAsync(x => x.Embed = Statics.BuildSkill(skill));
                    var reply = await NextMessageAsync();
                    if (reply.Content.ToLower() == "cancel") return;
                    if (reply.Content.ToLower() == "done") { B = false;}
                    else if (reply.Content.ToLower() == "add"){
                        await msg.ModifyAsync(x => x.Embed = null);
                        await msg.ModifyAsync(x => x.Content = "Please follow the Effect creation menu now.");
                        var effect = await NewEffect(this.Interactive,Context);
                        if (effect != null) {
                            skill.Effects.Add(effect);
                            }
                    }
                }
                character.Skill.Add(skill);
                character.Update(Database);
                await ReplyAsync("Skill "+skill.Name+" created successfully!"); return;
            }
        }
        public async Task<Effect> NewEffect(InteractiveService interactive, SocketCommandContext context, Effect effect = null){
            if (effect == null){
                effect = new Effect();
                var msg = await context.Channel.SendMessageAsync("This is the effect creator, here you can add some power to your skills/gears/traits/etc.\n"+
                    "Let's start simple, What's the name of this effect? (Note, names only matter if the effect is not immediate (such as buffs and debuffs).\n"+
                    "(Remember you can say \"Cancel\" to cancel this creation process.");
                var reply = await interactive.NextMessageAsync(context);
                if (reply.Content.ToLower() == "cancel") return null;
                else {
                    effect.Name = reply.Content;
                    await msg.DeleteAsync();
                    }
            }
            for (bool FirstLoop = true; FirstLoop == false;){
                var msg = await context.Channel.SendMessageAsync("Let's start making this effect. First things first. "+
                "What is this effect's **type**? \nUse the guide below to see what each type means and reply with its corresponding letter."
                ,embed: Statics.EffectInfo());
                var reply = await interactive.NextMessageAsync(context);
                switch (reply.Content.ToLower()){
                    case "a":
                        effect.type = Status.Damage;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "b":
                        effect.type = Status.Debuff;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "c":
                        effect.type = Status.DmgOverTime;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "d":
                        effect.type = Status.Heal;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "e":
                        effect.type = Status.Restraint;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "f":
                        effect.type = Status.ChanceOfSkip;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "g":
                        effect.type = Status.Misc;
                        FirstLoop = false;
                        await reply.DeleteAsync();
                        break;
                    case "cancel":
                        return null;
                    default:
                        await reply.DeleteAsync();
                        break;
                }
                for (bool SecondLoop = true; SecondLoop == false;){
                    await msg.ModifyAsync(x => x.Embed = Statics.EmbedEffect(effect));
                    if (effect.type == Status.Damage){
                        await msg.ModifyAsync(x => x.Content = "What's the damage Dice (Or flat damage number) for this effect?\n"+
                            "You can add X where the skill's level would go (ie: Xd5 = as many d5's as the skill's level)");
                        reply = await interactive.NextMessageAsync(context);
                        if (reply.Content.ToLower()=="cancel") return null;
                        var valid = System.Text.RegularExpressions.Regex.IsMatch(reply.Content.ToLower(), @"^[d-dx-xk-k0-9\+\-\s\*]*$");
                        if (!valid){
                            await reply.DeleteAsync();
                            await interactive.ReplyAndDeleteAsync(context,"This is not a valid dice expression!", timeout: TimeSpan.FromSeconds(5));
                        }
                        else if (valid){
                            effect.Dice = reply.Content.ToLower();
                            await msg.ModifyAsync(x => x.Content = "What type of damage is this? (ie: Blunt, Piercing, Slashing, Etc.)\n"+
                            "Note: System messages will read as `Character did X <Damage type> to <target>`.");
                            reply = await interactive.NextMessageAsync(context);
                            if (reply.Content.ToLower()=="cancel") return null;
                            effect.Description = reply.Content;
                            effect.Turns = 0;
                            await reply.DeleteAsync();
                            SecondLoop = false;
                        }
                    }
                    if (effect.type == Status.DmgOverTime){
                        await msg.ModifyAsync(x => x.Content = "What's the damage Dice (Or flat damage number) for this effect?\n"+
                            "You can add X where the skill's level would go (ie: Xd5 = as many d5's as the skill's level)");
                        reply = await interactive.NextMessageAsync(context);
                        if (reply.Content.ToLower()=="cancel") return null;
                        var valid = System.Text.RegularExpressions.Regex.IsMatch(reply.Content.ToLower(), @"^[d-dx-xk-k0-9\+\-\s\*]*$");
                        if (!valid){
                            await reply.DeleteAsync();
                            await interactive.ReplyAndDeleteAsync(context,"This is not a valid dice expression!", timeout: TimeSpan.FromSeconds(5));
                        }
                        else if (valid){
                            effect.Dice = reply.Content.ToLower();
                            await msg.ModifyAsync(x => x.Content = "What type of damage is this? (ie: Blunt, Piercing, Slashing, Etc.)\n"+
                            "Note: System messages will read as `Character did X <Damage type> to <target>`.");
                            reply = await interactive.NextMessageAsync(context);
                            if (reply.Content.ToLower()=="cancel") return null;
                            effect.Description = reply.Content;
                            await reply.DeleteAsync();
                            for (bool B = true; B == false;){
                                await msg.ModifyAsync(y => y.Content = "How many turns does this effect last?\n"+
                            "Use 0 to indicate that this effect's duration is based on its corresponding skill level (not applicable for items or weapons)");
                                reply = await interactive.NextMessageAsync(context);
                                if (!int.TryParse(reply.Content, out int i)){
                                    await reply.DeleteAsync();
                                    await interactive.ReplyAndDeleteAsync(Context,"This is not a number!",timeout: TimeSpan.FromSeconds(5));
                                }
                                else{
                                    effect.Turns = int.Parse(reply.Content);
                                    await reply.DeleteAsync();
                                    B = false;
                                    SecondLoop = false;
                                }
                            }
                        }
                    }
                    if (effect.type == Status.Heal){
                        await msg.ModifyAsync(x => x.Content = "What's the Healing Dice (Or flat number) for this effect?\n"+
                            "You can add X where the skill's level would go (ie: Xd5 = as many d5's as the skill's level)");
                        reply = await interactive.NextMessageAsync(context);
                        if (reply.Content.ToLower()=="cancel") return null;
                        var valid = System.Text.RegularExpressions.Regex.IsMatch(reply.Content.ToLower(), @"^[d-dx-xk-k0-9\+\-\s\*]*$");
                        if (!valid){
                            await reply.DeleteAsync();
                            await interactive.ReplyAndDeleteAsync(context,"This is not a valid dice expression!", timeout: TimeSpan.FromSeconds(5));
                        }
                        else if (valid){
                            effect.Dice = reply.Content;
                            await reply.DeleteAsync();
                        }
                    }
                    if (effect.type == Status.Restraint){
                        for (bool B = true; B == false;){
                            await msg.ModifyAsync(y => y.Content = "How many turns does this effect last?\n"+
                            "Use 0 to indicate that this effect's duration is based on its corresponding skill level (not applicable for items or weapons)");
                            reply = await interactive.NextMessageAsync(context);
                            if (!int.TryParse(reply.Content, out int i)){
                                await reply.DeleteAsync();
                                await interactive.ReplyAndDeleteAsync(Context,"This is not a number!",timeout: TimeSpan.FromSeconds(5));
                            }
                            else{
                                await reply.DeleteAsync();
                                effect.Turns = int.Parse(reply.Content);
                                B = false;
                                SecondLoop = false;
                            }
                        }
                    }
                    if (effect.type == Status.ChanceOfSkip){
                        for (bool B = true; B == false;){
                            await msg.ModifyAsync(y => y.Content = "How many turns does this effect last?\n"+
                            "Use 0 to indicate that this effect's duration is based on its corresponding skill level (not applicable for items or weapons)");
                            reply = await interactive.NextMessageAsync(context);
                            if (!int.TryParse(reply.Content, out int i)){
                                await reply.DeleteAsync();
                                await interactive.ReplyAndDeleteAsync(Context,"This is not a number!",timeout: TimeSpan.FromSeconds(5));
                            }
                            else{
                                await reply.DeleteAsync();
                                effect.Turns = int.Parse(reply.Content);
                                B = false;
                                SecondLoop = false;
                            }
                        }
                    }
                    if (effect.type == Status.Misc){
                        await msg.ModifyAsync(y => y.Content = "What does this effect do (This is a RP only effect and will have no bearing on stats).");
                        reply = await interactive.NextMessageAsync(context);
                        if (reply.Content.ToLower()=="cancel") return null;
                        effect.Description = reply.Content;
                        for (bool B = true; B == false;){
                            await msg.ModifyAsync(y => y.Content = "How many turns does this effect last?");
                            reply = await interactive.NextMessageAsync(context);
                            if (!int.TryParse(reply.Content, out int i)){
                                await reply.DeleteAsync();
                                await interactive.ReplyAndDeleteAsync(Context,"This is not a number!",timeout: TimeSpan.FromSeconds(5));
                            }
                            else{
                                await reply.DeleteAsync();
                                effect.Turns = int.Parse(reply.Content);
                                B = false;
                                SecondLoop = false;
                            }
                        }
                    }
                    if (effect.type == Status.Debuff){
                        for (bool b = true;b == false;){
                            await msg.ModifyAsync(y => y.Content = "What stat does this effect alter?\n"+
                            "You can choose between the following stats: Might, Agility, Constitution, Perception, Magic, Luck, Fortitude or Protection.");
                            reply = await interactive.NextMessageAsync(context);
                            if (reply.Content.ToLower()=="cancel") return null;
                            switch (reply.Content.ToLower()){
                                case "might":
                                    effect.AffectedStat = Stats.Might;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "agility":
                                    effect.AffectedStat = Stats.Agility;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "constitution":
                                    effect.AffectedStat = Stats.Constitution;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "perception":
                                    effect.AffectedStat = Stats.Perception;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "magic":
                                    effect.AffectedStat = Stats.Magic;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "luck":
                                    effect.AffectedStat = Stats.Luck;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "fortitude":
                                    effect.AffectedStat = Stats.Fortitude;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                case "protection":
                                    effect.AffectedStat = Stats.Protection;
                                    await reply.DeleteAsync();
                                    b = false;
                                    break;
                                default:
                                    await reply.DeleteAsync();
                                    await interactive.ReplyAndDeleteAsync(context,"This is not a valid option!",timeout: TimeSpan.FromSeconds(5));
                                    break;
                            }
                        }
                        await msg.ModifyAsync(y => y.Content = "How much does this effect affect this stat?\n"+
                        "(Use Negative numbers for debuffs and Positive numbers for buffs.)");
                        reply = await interactive.NextMessageAsync(context);
                        if (reply.Content.ToLower()=="cancel") return null;
                        if (!int.TryParse(reply.Content.ToLower(),out int i) || int.Parse(reply.Content) == 0){
                            await reply.DeleteAsync();
                            await interactive.ReplyAndDeleteAsync(context,"This isn't a valid number or is a Zero!",timeout: TimeSpan.FromSeconds(5));
                        }
                        else {
                            effect.Dice = reply.Content;
                            await reply.DeleteAsync();
                            SecondLoop = false;
                        }
                    }
                }
                for (bool confirm = true; confirm == false;){
                    await msg.ModifyAsync(x => x.Embed = Statics.EmbedEffect(effect));
                    await msg.ModifyAsync(x => x.Content = "Is this ok? (y/n)");
                    reply = await interactive.NextMessageAsync(context,timeout: TimeSpan.FromSeconds(5));
                    if (reply.Content.ToLower() == "cancel") return null;
                    if (reply.Content.ToLower() == "n" || reply.Content.ToLower() == "no") FirstLoop = true;
                    if (reply.Content.ToLower() == "y" || reply.Content.ToLower() == "yes") confirm = false;
                }
            }
            return effect;
        }
    }
}