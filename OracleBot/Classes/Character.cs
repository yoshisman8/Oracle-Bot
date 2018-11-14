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

namespace OracleBot.Classes 
{
    public class Character : InteractiveBase<SocketCommandContext>
    {
        [BsonId]
        public int Id {get;set;}
        public string Name {get;set;} = "";
        public string Title {get;set;} = "";
        public int HealthPoints {get;set;}
        public int Stamina {get;set;}
        public AbilityScore GrossMotorSkill {get;set;} = new AbilityScore() {Score = AbScore.GM};
        public AbilityScore FineMotorSkill {get;set;} = new AbilityScore() {Score = AbScore.GM};
        public AbilityScore InnerMind {get;set;} = new AbilityScore() {Score = AbScore.IM};
        public AbilityScore OuterMind {get;set;} = new AbilityScore() {Score = AbScore.OM};
        public List<Skill> Skills {get;set;} = new List<Skill>();
        public List<Item> Inventory {get;set;} = new List<Item>();
    }
    public class AbilityScore{
        public AbScore Score {get;set;}
        public int Value {get;set;} = 10;
        public int GetModifier(){
            double mod = (Value-10/2);
            var ret = mod >=0 ? Math.Floor(mod) : Math.Round(mod);
            return Convert.ToInt32(ret);
        }
    }
    public class Skill {
        public int Ranks {get;set;}
        public string Name {get;set;}
        public AbScore[] AssociatedScores {get;set;}
    }
    public class Item {
        public string Name {get;set;}
        public string Description {get;set;}
        public bool Equiped {get;set;}
        public ItemPurpose Function {get;set;} = ItemPurpose.Miscellaneous;
        public Dictionary<string,int> Metadata {get;set;} = new Dictionary<string, int>();
    }
    public enum AbScore {GM, FM, IM, OM}
    public enum ItemPurpose {Armor, Weapon, Consumable, Miscellaneous}
}