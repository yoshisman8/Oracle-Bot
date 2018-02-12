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
                    await ReplyAndDeleteAsync("You can't raise or decrease a stat by 0 you silly!", timeout: TimeSpan.FromSeconds(10));
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
                if (Stat.ToLower() == "agi"){
                    C.AbilityScores.AGI+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Agility;
                }
                if (Stat.ToLower() == "con"){
                    C.AbilityScores.CON+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Constitution;
                }
                if (Stat.ToLower() == "per"){
                    C.AbilityScores.PER+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Perception;
                }
                if (Stat.ToLower() == "mag"){
                    C.AbilityScores.MAG+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Magic;
                }
                if (Stat.ToLower() == "lck"){
                    C.AbilityScores.LCK+= Amount;
                    C.Level.StatPoints-= Amount;
                    S = Stats.Luck;
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
                        if (effect != null) skill.Effects.Add(effect);
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
                var msg = context.Channel.SendMessageAsync("This is the effect creator, here you can add some power to your skills/gears/traits/etc.\n"+
                    "Let's start simple, What's the name of this effect? (Note, names only matter if the effect is not immediate (such as buffs and debuffs).\n"+
                    "(Remember you can say \"Cancel\" to cancel this creation process.");
                var reply = await interactive.NextMessageAsync(context);
                if (reply.Content.ToLower() == "cancel") return null;
                else effect.Name = reply.Content;
            }

            return effect;
        }
    }
}