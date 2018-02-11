using System;
using Discord;
using System.Threading.Tasks;
using System.Text;
using Discord.Commands;
using Discord.Addons.Interactive;
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

        [Command("AddChar"), Alias("Add-char", "create-char","Newchar")]
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
            var msg = await ReplyAsync("Character **"+Name+"** Added to the Database.\n"+
            "You've been given 18 stat points and 1 skill point. Use `.StatUp Stat_To_Increase Ammount` and `.AddSkill Skill_Name` to use said points.");
        }
        [Command("Test")]
        public async Task Test(){
            var test = new Character();
            Database.DropCollection("Characters");
            var col = Database.GetCollection<Character>("Characters");
            col.Insert(test);
            col.EnsureIndex("Name","LOWER($.Name)");
            var c = col.FindOne(x=> x.Name == "joey");
            await ReplyAsync("", embed: Statics.BuildCharacterSheet(c,Context,Database));
            col.Delete(c.Id);
        }
    }
}