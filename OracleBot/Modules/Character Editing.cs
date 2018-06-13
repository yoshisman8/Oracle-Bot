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
    public class Character_Editing : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}
        [Command("SetScore")]
        [Summary("Set your locked character's ability score. Usage: `.SetScore AbScore Value Proficiency` valid Scores are `STR/DEX/CON/INT/WIS` and Proficiencies are `Untrained/Proficient/Expert`. All case sensitive.")]
        public async Task SetAbScore(AbilityShort Score, int Value, Proficiency Prof = Proficiency.Untrained){
            Value = Math.Abs(Value);
            if (Value > 30){
                await ReplyAndDeleteAsync(Context.User.Mention+", You can't set ability scores higher than 30!", timeout: TimeSpan.FromSeconds(5));
                return;
            }
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
                chr.AbilityScores[(int)Score].Value = Value;
                chr.AbilityScores[(int)Score].Proficient = Prof;
                if (Score == AbilityShort.CON) chr.Fullheal();
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s "+Score+" to "+Value+" ["+Prof+"]");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetEScore")]
        [Summary("Set your locked character's extra ability score bonus. Usage: `.SetEScore AbScore Value Proficiency`.\n"+
            "Valid Scores are `STR/DEX/CON/INT/WIS` and Proficiencies are `Untrained/Proficient/Expert`. All case sensitive.")]        
        public async Task SetEAbScore(AbilityShort Score, int Value, Proficiency Prof = Proficiency.Untrained){
            Value = Math.Abs(Value);
            if (Value > 30){
                await ReplyAndDeleteAsync(Context.User.Mention+", You can't set ability scores higher than 30!", timeout: TimeSpan.FromSeconds(5));
                return;
            }
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
                chr.AbilityScores[(int)Score].Extra = Value;
                chr.AbilityScores[(int)Score].Proficient = Prof;
                if (Score == AbilityShort.CON) chr.Fullheal();
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s bonus "+Score+" value to "+Value+" ["+Prof+"]");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetImage")]
        [Summary("Sets your locked character's thumbnail image. Usage: `.SetImage ImageURL` Alternatively, you can send an image and type the `.SetImage` command with no arguments.")]
        public async Task SetIMG([Remainder] string ImageURL = "https://media.discordapp.net/attachments/357593658586955776/454118701592215554/user-black-close-up-shape.png"){
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
                if (Context.Message.Attachments.Count() > 0){
                    chr.Image = Context.Message.Attachments.First().Url;
                    await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s thumbnail image.");                    
                }
                else if (ImageURL == "https://media.discordapp.net/attachments/357593658586955776/454118701592215554/user-black-close-up-shape.png"){
                    chr.Image = ImageURL;
                    await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s thumbnail image back to its default image.");
                    await Context.Message.DeleteAsync();
                }
                else {
                    chr.Image = ImageURL;
                    await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s thumbnail image.");
                    await Context.Message.DeleteAsync();    
                }
                col.Update(chr);
            }
        }

        [Command("NewSkill"), Alias("AddSkill")]
        [Summary("Adds or Updates a skill to your locked character. Usage: `.NewSkill Name AbilityScore Proficiency`\n"+
            "Valid Scores are `STR/DEX/CON/INT/WIS` and Proficiencies are `Untrained/Proficient/Expert`. All case sensitive.")]
        public async Task NewSkill(string Name, AbilityShort Score, Proficiency Prof = Proficiency.Proficient){
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
                if(chr.Skills.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var index = chr.Skills.FindIndex(x => x.Name.ToLower() == Name.ToLower());
                    var skill = chr.Skills.ElementAt(index);
                    skill.Ability = Score;
                    skill.Proficiency = Prof;
                    chr.Skills[index] = skill;
                    await ReplyAsync(Context.User.Mention+", you updated your character's **"+skill.Name+"** skill to use **"+Score+"** and be **"+Prof+"**.");
                }
                else{
                    var skill = new Skill(){
                        Name = Name,
                        Ability = Score,
                        Proficiency = Prof
                    };
                    chr.Skills.Add(skill);
                    await ReplyAsync(Context.User.Mention+", you added the following skill to your character: **"+skill.Name+"** that uses **"+Score+"** and they're **"+Prof+"**.");
                }
                col.Update(chr);
                await Context.Message.DeleteAsync();
            }
        }
        [Command("RemoveSkill"), Alias("RemSkill","DeleteSkill","DelSkill")]
        [Summary("Removes one of your locked character's skills. Usage: `.RemSkill Name`.")]
        public async Task RemSkill([Remainder] string Name){
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
                    chr.Skills.Remove(skill);
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", you removed "+chr.Name+"'s skill **"+skill.Name+"**.");
                    await Context.Message.DeleteAsync();                    
                }
            }
        }
        [Command("NewTrait"), Alias("AddTrait")]
        [Summary("Adds or update a trait on your locked character. Usage: `.NewTrait Name Description`")]
        public async Task NewTrait(string Name, string Description){
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
                if (chr.Traits.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var trait = chr.Traits.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    int index = chr.Traits.IndexOf(trait);
                    trait.Description = Description;
                    chr.Traits[index] = trait;
                    await ReplyAsync(Context.User.Mention+", Updated "+chr.Name+"'s Trait **"+trait.Name+"**.");
                }
                else{
                    var ab = new Ability(){
                        Name = Name,
                        Description = Description
                    };
                    chr.Traits.Add(ab);
                    await ReplyAsync(Context.User.Mention+", Added Trait **"+ab.Name+"** to "+chr.Name+".");
                }
                col.Update(chr);
                await Context.Message.DeleteAsync();
            }
        }
        [Command("RemoveTrait"), Alias("RemTrait","DelTrait","DeleteTrait")]
        [Summary("Removes a trait from your locked character. Usage: `.RemTrait Name`")]
        public async Task RemTrait([Remainder] string Name){
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
                if (!chr.Traits.Exists(x => x.Name.ToLower().StartsWith(Name.ToLower()))){
                    await ReplyAndDeleteAsync(Context.User.Mention+", your character doesn't have any trait whose name starts with "+Name+".", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                if (chr.Traits.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).Count() > 1 && !chr.Traits.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var result = chr.Traits.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    var sb = new StringBuilder().Append(Context.User.Mention+", "+chr.Name+" has more than " + result.Count()+ " Traits that starts with the word **"+Name+"**. Please specify which one from this list is the one you want to remove by using said Trait's full name: ");
                    foreach (var x in result){
                        sb.Append("`"+x.Name+"`, ");
                    }
                    await ReplyAndDeleteAsync(sb.ToString().Substring(0,sb.Length-2), timeout: TimeSpan.FromSeconds(10));
                    return;
                }
                else{
                    var trait = chr.Traits.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    chr.Traits.Remove(trait);
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", you removed "+chr.Name+"'s trait **"+trait.Name+"**.");
                }
                await Context.Message.DeleteAsync();
            }
        }
        [Command("NewAbility"), Alias("AddAbility")]
        [Summary("Adds or update an ability on your locked character. Usage: `.NewAbility Name Description`")]
        public async Task NewAB(string Name, string Description){
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
                if (chr.Abilities.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var trait = chr.Abilities.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    int index = chr.Abilities.IndexOf(trait);
                    trait.Description = Description;
                    chr.Abilities[index] = trait;
                    await ReplyAsync(Context.User.Mention+", Updated "+chr.Name+"'s ablity **"+trait.Name+"**.");
                }
                else{
                    var ab = new Ability(){
                        Name = Name,
                        Description = Description
                    };
                    chr.Abilities.Add(ab);
                    await ReplyAsync(Context.User.Mention+", Added ability **"+ab.Name+"** to "+chr.Name+".");
                }
                col.Update(chr);
                await Context.Message.DeleteAsync();
            }
        }
        [Command("RemoveAbility"), Alias("RemAbility","DelAbility","DeleteAbility")]
        [Summary("Removes an Ability from your locked character. Usage: `.RemAbility Name`")]
        public async Task RemAB([Remainder] string Name){
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
                if (!chr.Abilities.Exists(x => x.Name.ToLower().StartsWith(Name.ToLower()))){
                    await ReplyAndDeleteAsync(Context.User.Mention+", your character doesn't have any trait whose name starts with "+Name+".", timeout: TimeSpan.FromSeconds(5));
                    return;
                }
                if (chr.Abilities.Where(x => x.Name.ToLower().StartsWith(Name.ToLower())).Count() > 1 && !chr.Abilities.Exists(x => x.Name.ToLower() == Name.ToLower())){
                    var result = chr.Abilities.Where(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    var sb = new StringBuilder().Append(Context.User.Mention+", "+chr.Name+" has more than " + result.Count()+ " Traits that starts with the word **"+Name+"**. Please specify which one from this list is the one you want to remove by using said Trait's full name: ");
                    foreach (var x in result){
                        sb.Append("`"+x.Name+"`, ");
                    }
                    await ReplyAndDeleteAsync(sb.ToString().Substring(0,sb.Length-2), timeout: TimeSpan.FromSeconds(10));
                    return;
                }
                else{
                    var trait = chr.Abilities.Find(x => x.Name.ToLower().StartsWith(Name.ToLower()));
                    chr.Abilities.Remove(trait);
                    col.Update(chr);
                    await ReplyAsync(Context.User.Mention+", you removed "+chr.Name+"'s abilities **"+trait.Name+"**.");
                }
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetColor")]
        [Summary("Sets the color of the vertical bar on your character sheet. Usage: `.Color RED BLUE GREEN` All values are from 0-255.")]
        public async Task SetColor(int R, int B, int G){
            if ((R < 0 || G < 0 || B < 0) || (R > 255 || G > 255 || B > 255)){
                await ReplyAndDeleteAsync(Context.User.Mention+", RGB values go from 0 to 255. Please use correct values!", timeout: TimeSpan.FromSeconds(5));
                return;
            }
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
                chr.Color[0] = R;
                chr.Color[1] = B;
                chr.Color[2] = G;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s custom Sheet color.");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetRace")]
        [Summary("Sets the race of your character. Usage: `.SetRace Race`.")]
        public async Task SetRace([Remainder]string Race){
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
                chr.Race = Race;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s race to "+Race +".");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetClass")]
        [Summary("Sets the Class of your character. Usage: `.SetClass Class`.")]
        public async Task SetClass([Remainder]string Class){
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
                chr.Class = Class;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s Class to "+Class +".");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetAC"), Alias("SetArmorClass")]
        [Summary("Sets the Armor Class of your character. Usage: `.SetAC ArmorClass`.")]
        public async Task SetAC([Remainder]int AC){
            AC = Math.Abs(AC);
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
                chr.ArmorClass = AC;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s Armor Class to "+ AC +".");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetHP")]
        [Summary("Sets the your character's extra HP (adds on top of your CON-based HP). Usage: `.SetHP HealthValue`.")]
        public async Task SetHP([Remainder]int HP){
            HP = Math.Abs(HP);
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
                chr.Health.Extra = HP;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", You set **"+chr.Name+"**'s Extra HP to "+ HP +".");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("LevelUp"), Alias("LU")]
        [Summary("Increases your locked character's level (1 by default). Usage `.LU Amount`. Defaults to 1 level if left empty.")]
        public async Task LevelUp([Remainder]int Levels = 1){
            Levels = Math.Abs(Levels);
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
                chr.Health.Level += Levels;
                chr.Fullheal();
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", **"+chr.Name+"**'s Level went up by "+Levels +"!");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("LevelDown"), Alias("LD")]
        [Summary("Decreases your locked character's level (1 by default). Usage `.LD Amount`. Defaults to 1 level if left empty.")]
        public async Task LevelDown([Remainder]int Levels = 1){
            Levels = Math.Abs(Levels);
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
                chr.Health.Level -= Levels;
                chr.Fullheal();
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", **"+chr.Name+"**'s Level went down by "+Levels +"!");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("SetProf"), Alias("SetProficiency")]
        [Summary("Set's your character's proficiency bonus. Usage: `.SetProf Amount`")]
        public async Task SetProf([Remainder]int Amount = 1){
            Amount = Math.Abs(Amount);
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
                chr.Profiency = Amount;
                col.Update(chr);
                await ReplyAsync(Context.User.Mention+", **"+chr.Name+"**'s Proficiency Bonus was set to "+ Amount +".");
                await Context.Message.DeleteAsync();
            }
        }
        [Command("ToggleCodeBlocks"),Alias("ToggleCB")]
        [Summary("Toggles your Locked character's sheet display mode between Regular and codeblock mode. Usage: `.ToggleCB`")]
        public async Task ToggleCB(){
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
                if (chr.CodeblockMode) {
                    chr.CodeblockMode = false;
                    
                    await ReplyAsync(Context.User.Mention+", Disabled Code Block mode for "+chr.Name+"'s sheet.");
                }
                else {
                    chr.CodeblockMode = true;
                    await ReplyAsync(Context.User.Mention+", Enabled Code Block mode for "+chr.Name+"'s sheet.");
                }
                col.Update(chr);
                await Context.Message.DeleteAsync();
            }
        }
    }
}