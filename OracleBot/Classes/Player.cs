using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Discord;

namespace OracleBot.Classes
{
    public class player{
        [BsonId]
        public ulong DiscordId {get;set;}
        [BsonRef("Characters")]
        public Character Character {get;set;}
    }
    public class Character
    {
        [BsonId]
        public int ID {get;set;}
        public ulong Owner {get;set;}
        public string Name {get;set;}
        public string Image {get;set;} = "";
        public HealthBlock Health {get;set;} = new HealthBlock();
        public string Class {get;set;} = "Classless";
        public string Race {get;set;} = "Generic";
        public AbScore[] AbilityScores {get;set;} = new AbScore[5]; //STR, DEX, CON, INT, WIS
        public int ArmorClass {get;set;} = 10;
        public int Profiency {get;set;} = 2;
        public List<Skill> Traits {get;set;} = new List<Skill>();
        public List<Skill> Skills {get;set;} = new List<Skill>();
        public List<Ability> Abilities {get;set;} = new List<Ability>();
        public List<Item> Inventory {get;set;} = new List<Item>();

        public Embed GetSheet(){
            var eb = new EmbedBuilder()
                .WithTitle(Name + " the "+ Race + " " + Class)
                .AddField("Ability Scores", "```ini\n💓 STR | "+AbilityScores[0].GetValue()+" ["+ AbilityScores[0].GetMod()+"]\n💚 DEX | "+AbilityScores[1].GetValue()+" ["+ AbilityScores[1].GetMod()+"]\n💛 CON | "+AbilityScores[2].GetValue()+" ["+ AbilityScores[2].GetMod()+"]\n💙 INT | "+AbilityScores[3].GetValue()+" ["+ AbilityScores[3].GetMod()+"]\n💜 WIS |"+AbilityScores[4].GetValue()+"["+ AbilityScores[4].GetMod()+"]```")
                .AddField("Statistics", "```css\n🔰 Level: "+ Health.Level+"\n🛡 Armor Class: "+ArmorClass+"\n🔴 Health: ["+Health.Current+"/"+Health.GetHealth(AbilityScores[2].GetValue())+"]\n💮 Profficiency: "+Profiency+"```",true)
                .WithThumbnailUrl(Image);
            var sb = new StringBuilder();
            if (Traits.Count == 0) eb.AddField("Traits","Use `.NewTrait Name Description` to add.",true);
            else{
                foreach(var x in Traits){
                    sb.AppendLine("• "+x.Name);
                }
                eb.AddField("Traits",sb.ToString(),true);
                sb.Clear();
            }


            if (Skills.Count == 0) eb.AddField("Skills","Use `.NewSkill Name Ability_Score Proficiency(y/n/e)` to add.",true);
            else{
                foreach(var x in Skills){
                    sb.AppendLine("• "+x.Name+"("+x.Ability+") "+"["+x.Proficiency+"]");
                }
                eb.AddField("Skills",sb.ToString(),true);
                sb.Clear();
            }



            return eb.Build();
        }
    }
    public class HealthBlock {
        public int Level {get;set;}
        public int Health {get;set;}
        public int Extra {get;set;}
        public int Current {get;set;}

        [BsonIgnore]
        public Dictionary<int,int> values = new Dictionary<int, int>(){
            {0,0}, {1,1}, {2,1}, {3,1}, {4,2}, {5,2}, {6,3}, {7,3}, {8,4}, {9,4}, 
            {10,5}, {11,6}, {12,7}, {13,8}, {14,9}, {15,10}, {16,11}, {17,12}, {18,13}, {19,14}, {20,15},
            {21,16}, {22,17}, {23,18}, {24,19}, {25,20}, {26,21}, {27,22}, {28,29}, {29,24}, {30,25}
        };

        public int GetHealth(string Constitution){
            return values.GetValueOrDefault(int.Parse(Constitution));
        }
    }
    public class AbScore{
        public int Value {get;set;} = 10;
        public int Extra {get;set;} = 0;
        public Proficiency Proficient {get;set;} = Proficiency.Untrained;
        public string GetValue(){
            return String.Format("{0:00}",(Value + Extra));
        }
        public string GetMod(){
            double mod = ((Value+Extra)-10)/2;
            mod = Math.Floor(mod);
            if (mod > 0) return mod.ToString();
            return String.Format("{0:00}",mod);
        }
    }
    public class Ability{
        public string Name {get;set;} = "";
        public string Description {get;set;} = "";
        public string Macro {get;set;} = "";
    }
    public class Skill {
        public string Name {get;set;}
        public AbilityShort Ability {get;set;}
        public Proficiency Proficiency = Proficiency.Untrained;
    }
    public class Item {
        public string Name {get;set;} = "";
        public int Ammount {get;set;} = 1;
        public string Description {get;set;} = "";
    }
    public enum AbilityScores {Strength = 0, Dexterity = 1, Constitution = 2, Intelligance = 3, Wisdom = 4}
    public enum AbilityShort {STR = 0, DEX = 1, CON = 2, INT = 3, WIS = 4}
    public enum Proficiency {Untrained = 0, Proficient = 1, Expert = 2}
}