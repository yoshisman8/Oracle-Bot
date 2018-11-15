using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Discord;
using OracleBot.Classes;

namespace OracleBot.Classes
{
    public class Character
    {
        [BsonId]
        public int ID {get;set;}
        public ulong Owner {get;set;}
        public string Name {get;set;}
        public string Image {get;set;} = "https://media.discordapp.net/attachments/357593658586955776/454118701592215554/user-black-close-up-shape.png";
        public int[] Color {get;set;} = new int[3]{255,255,255};
        public HealthBlock Health {get;set;} = new HealthBlock();
        public string Class {get;set;} = "Classless";
        public string Race {get;set;} = "Generic";
        public AbScore[] AbilityScores {get;set;} = new AbScore[5]; //STR, DEX, CON, INT, WIS, CHA
        public List<Ability> Traits {get;set;} = new List<Ability>();
        public List<Skill> Skills {get;set;} = new List<Skill>();
        public List<Ability> Abilities {get;set;} = new List<Ability>();
        public List<Attack> Attacks {get;set;} = new List<Attack>();
        public List<Item> Inventory {get;set;} = new List<Item>();
        public double Money {get;set;} = 0;
        public bool CodeblockMode {get;set;} = true;

        public Embed GetSheet(){
            var eb = new EmbedBuilder()
                .WithTitle(Name + " the "+ Race + " " + Class)
                .AddField("Ability Scores", "```ini\n⚔️ STR | "+AbilityScores[0].GetValue()+" ["+ AbilityScores[0].GetMod()+"] "+AbilityScores[0].IsTrained()+
                "\n🗡️ DEX | "+AbilityScores[1].GetValue()+" ["+ AbilityScores[1].GetMod()+"] "+
                "\n💗 CON | "+AbilityScores[2].GetValue()+" ["+ AbilityScores[2].GetMod()+"] "+
                "\n🧠 INT | "+AbilityScores[3].GetValue()+" ["+ AbilityScores[3].GetMod()+"] "+
                "\n🧙 WIS | "+AbilityScores[4].GetValue()+" ["+ AbilityScores[4].GetMod()+"] "+
                "\n👥 CHA | "+AbilityScores[5].GetValue()+" ["+ AbilityScores[5].GetMod()+"] "+"```",true)
                .AddField("Statistics", "```css\n🔰 Level: "+ Health.Level+"\n🔴 Health: ["+Health.Current+"/"+Health.GetHealth(AbilityScores[2].GetMod())+"]"+"\n🛡 Defense Rating: "+10+ParseArmor()+"\n💥 Attack Rating: "+Health.GetBab()+"\nREF | FORT | WILL\n"+"[+"+Health.GetSave(SavingThrows.Reflex,AbilityScores)+"] | "+"[+"+Health.GetSave(SavingThrows.Fortitude,AbilityScores)+"] | "+"[+"+Health.GetSave(SavingThrows.Will,AbilityScores)+"]"+"```",true)
                .WithThumbnailUrl(Image)
                .WithColor(new Color(Color[0],Color[1],Color[2]));
            var sb = new StringBuilder();
            if (Traits.Count == 0) eb.AddField("Traits and Feats","Use `.NewTrait Name Description` to add.",true);
            else{
                foreach(var x in Traits){
                    sb.AppendLine("• "+x.Name);
                }
                if (CodeblockMode) eb.AddField("Traits and Feats","```"+sb.ToString()+"```",true);
                else eb.AddField("Traits",sb.ToString(),true);
                sb.Clear();
            }

            if (Abilities.Count == 0) eb.AddField("Abilities", "Use `.NewAbility Name Description` to add.", true);
            else {
                foreach(var x in Abilities){
                    sb.AppendLine("• "+x.Name);
                }
                if (CodeblockMode) eb.AddField("Abilities","```"+ sb.ToString()+"```",true);
                else eb.AddField("Abilities", sb.ToString(),true);
                sb.Clear();
            }

            if (Skills.Count == 0) eb.AddField("Skills","Use `.NewSkill Name Ability_Score Proficiency` to add.",true);
            else{
                var sort = Skills.OrderBy(x=> x.Ranks);
                for(int i = 0; i > 5 ; i++){
                    var x = sort.ElementAt(i);
                    sb.AppendLine("• "+x.Name+"("+x.Ability+") "+"["+(x.Ranks+AbilityScores[(int)x.Ability].GetIntMod())+"]");
                }
                if(CodeblockMode) eb.AddField("Skills","```ini\n"+sb.ToString()+"```",true);
                else eb.AddField("Skills",sb.ToString(),true);
                sb.Clear();
            }
            sb.AppendLine("$"+Money.ToString());
            var inv = Inventory.Where(x=>x.Quantity>0);
            foreach(var x in inv){
                sb.AppendLine("• "+x.Name+" [x"+x.Quantity+"]");
            }
            if (CodeblockMode) eb.AddField("Inventory","```css\n"+sb.ToString()+"```",true);
            else eb.AddField("Inventory",sb.ToString(),true);
            sb.Clear();
            return eb.Build();
        }
        public void Fullheal(){
            Health.Current = Health.GetHealth(AbilityScores[2].GetMod());
        }
        public int ParseArmor(){
            int tot = 0;
            foreach (var x in Inventory.Where(x=> x.Type == ItemType.Armor && x.Worn == true)){
                tot += int.Parse(x.Value);
            }
            return tot+AbilityScores[1].GetIntMod();
        }
    }
    public class HealthBlock {
        public int Level {get;set;} = 1;
        public int Extra {get;set;} = 0;
        public int Current {get;set;} = 5;
        public int Base {get;set;} = 6;
        public bool Reflex {get;set;} = false;
        public bool Fortitude {get;set;} = false;
        public bool Will {get;set;} = false;
        public bool FullBaB {get;set;} = false;

        public int GetHealth(string Constitution){
            var tot = (Base*Level)+Constitution;
            return int.Parse(tot);
        }
        public int GetBab(){
            int bab = FullBaB ? Convert.ToInt32(Math.Floor((double)Level*(3/4))) : Level;
            return bab;
        }
        public int GetSave(SavingThrows Save, AbScore[] Scores){
            int ret = 0;
            switch (Save){
                case SavingThrows.Reflex:
                    ret = Reflex ? Convert.ToInt32(Math.Floor((double)Level*(1/3))) : Convert.ToInt32(Math.Floor((Level+4)*0.5));
                    return ret + Scores[1].GetIntMod();
                case SavingThrows.Fortitude:
                    ret = Fortitude ? Convert.ToInt32(Math.Floor((double)Level*(1/3))) : Convert.ToInt32(Math.Floor((Level+4)*0.5));
                    return ret + Scores[2].GetIntMod();
                case SavingThrows.Will:
                    ret = Will ? Convert.ToInt32(Math.Floor((double)Level*(1/3))) : Convert.ToInt32(Math.Floor((Level+4)*0.5));
                    return ret + Scores[4].GetIntMod();
                default:
                    return ret;
            }
        }
    }
    public class AbScore{
        public int Value {get;set;} = 10;
        public int Extra {get;set;} = 0;
        public Proficiency Trained {get;set;} = Proficiency.Untrained; //Removed
        public string GetValue(bool Int = false){
            if (Int){
                return (Value+Extra).ToString();
            }
            return String.Format("{0:00}",(Value + Extra));
        }
        public string IsTrained(){
            if (Trained == Proficiency.Trained) return "⭐";
            if (Trained == Proficiency.Expert) return "🌟";
            else return "";
        }
        public string GetMod(bool Int = false){
            double mod = ((Value+Extra)-10)/2;
            if (mod < 0) mod = Math.Round(mod);
            else mod = Math.Floor(mod);
            if (Int) return mod.ToString();
            if (mod < 0) return mod.ToString();
            if (mod == 0) return "--";
            if (mod > 0 && mod < 10) return "+"+mod.ToString();
            return String.Format("{0:00}",mod);
        }
        public int GetIntMod(){
            double mod = ((Value+Extra)-10)/2;
            if (mod < 0) mod = Math.Round(mod);
            else mod = Math.Floor(mod);
            return (int)mod;
        }
    }
    public class Ability{
        public string Name {get;set;} = "";
        public string Description {get;set;} = "";
        public string Macro {get;set;} = "";
    }
    public class Attack{
        public string Name {get;set;} = "Unarmed Strike";
        public string Description {get;set;} = "";
        public string Macro {get;set;} = "";
        public AttackType Type {get;set;} = AttackType.Melee;
    }
    public class Skill {
        public string Name {get;set;}
        public int Ranks {get;set;} = 0;
        public AbilityShort Ability {get;set;}
        public Proficiency Proficiency {get;set;} = Proficiency.Untrained;
    }
    public class Item {
        public string Name {get;set;} = "";
        public int Quantity {get;set;} = 1;
        public string Description {get;set;} = "";
        public bool Worn {get;set;} = false;
        public string Value {get;set;} = "";
        public ItemType Type {get;set;} = ItemType.Miscellanous;
    }
    public enum AbilityScores {Strength = 0, Dexterity = 1, Constitution = 2, Intelligance = 3, Wisdom = 4, Charisma = 5}
    public enum AbilityShort {STR = 0, DEX = 1, CON = 2, INT = 3, WIS = 4, CHA = 5}
    public enum SavingThrows {Reflex, Will, Fortitude}
    public enum Proficiency {Untrained = 0, Trained = 1, Expert = 2}
    public enum AttackType {Melee, Spell}
    public enum ItemType {Armor, Weapon, Consumable, Extra, Miscellanous}
}