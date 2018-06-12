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
        public AbScore[] AbilityScores {get;set;} = new AbScore[5]; //STR, DEX, CON, INT, WIS
        public int ArmorClass {get;set;} = 10;
        public int Profiency {get;set;} = 2;
        public List<Ability> Traits {get;set;} = new List<Ability>();
        public List<Skill> Skills {get;set;} = new List<Skill>();
        public List<Ability> Abilities {get;set;} = new List<Ability>();
        public List<Spell> SpellBook {get;set;} = new List<Spell>();
        public List<PlayerItem> Inventory {get;set;} = new List<PlayerItem>();
        public double Money {get;set;} = 0;
        public bool CodeblockMode {get;set;} = false;

        public Embed GetSheet(){
            var eb = new EmbedBuilder()
                .WithTitle(Name + " the "+ Race + " " + Class)
                .AddField("Ability Scores", "```ini\n⚔️ STR | "+AbilityScores[0].GetValue()+" ["+ AbilityScores[0].GetMod()+"] "+AbilityScores[0].IsProficient()+
                "\n🗡️ DEX | "+AbilityScores[1].GetValue()+" ["+ AbilityScores[1].GetMod()+"] "+AbilityScores[1].IsProficient()+
                "\n💗 CON | "+AbilityScores[2].GetValue()+" ["+ AbilityScores[2].GetMod()+"] "+AbilityScores[2].IsProficient()+
                "\n🧠 INT | "+AbilityScores[3].GetValue()+" ["+ AbilityScores[3].GetMod()+"] "+AbilityScores[3].IsProficient()+
                "\n🧙 WIS | "+AbilityScores[4].GetValue()+" ["+ AbilityScores[4].GetMod()+"] "+AbilityScores[4].IsProficient()+"```",true)
                .AddField("Statistics", "```css\n🔰 Level: "+ Health.Level+"\n🛡 Armor Class: "+ArmorClass+"\n🔴 Health: ["+Health.Current+"/"+Health.GetHealth(AbilityScores[2].GetValue())+"]\n💮 Proficiency: "+Profiency+"\n✨ Spells Known: "+SpellBook.Count+"```",true)
                .WithThumbnailUrl(Image)
                .WithColor(new Color(Color[0],Color[1],Color[2]));
            var sb = new StringBuilder();
            if (Traits.Count == 0) eb.AddField("Traits","Use `.NewTrait Name Description` to add.",true);
            else{
                foreach(var x in Traits){
                    sb.AppendLine("• "+x.Name);
                }
                if (CodeblockMode) eb.AddField("Traits","```"+sb.ToString()+"```",true);
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
                foreach(var x in Skills){
                    sb.AppendLine("• "+x.Name+"("+x.Ability+") "+"["+x.Proficiency+"]");
                }
                if(CodeblockMode) eb.AddField("Skills","```ini\n"+sb.ToString()+"```",true);
                else eb.AddField("Skills",sb.ToString(),true);
                sb.Clear();
            }
            sb.AppendLine("$"+Money.ToString());
            foreach (var x in Inventory){
                sb.AppendLine("• "+x.Item.Name+" [x"+x.Quantity+"]");
            }
            if (CodeblockMode) eb.AddField("Inventory","```css\n"+sb.ToString()+"```",true);
            else eb.AddField("Inventory",sb.ToString(),true);
            sb.Clear();
            return eb.Build();
        }
        public void Fullheal(){
            Health.Current = Health.GetHealth(AbilityScores[2].GetValue());
        }
        public void BuildInventory(LiteDatabase Database, player player){
            var col = Database.GetCollection<Item>("Items");
            var db = Database.GetCollection<Character>("Characters");
            var buffer = new List<PlayerItem>();
            foreach(var x in Inventory){
                var index = Inventory.IndexOf(x);
                if (x.Item.Id != -1 && col.Exists(y => y.Id == x.Item.Id)){
                    var item = col.FindOne(y => y.Id == x.Item.Id);
                    buffer.Add(new PlayerItem(){
                        Item = item,
                        Quantity = x.Quantity
                        });
                }
                else if(player.ItemVault.Exists(y => y.Name.ToLower() == x.Item.Name.ToLower())){
                    var item = player.ItemVault.Find(y => y.Name.ToLower() == x.Item.Name.ToLower());
                    buffer.Add(new PlayerItem(){
                        Item = item,
                        Quantity = x.Quantity
                        });
                }
                else if(!player.ItemVault.Exists(y => y.Name.ToLower() == x.Item.Name.ToLower()) && col.Exists(y => y.Name == x.Item.Name.ToLower())){
                    var item = col.FindOne(y => y.Name == x.Item.Name.ToLower());
                    buffer.Add(new PlayerItem(){
                        Item = item,
                        Quantity = x.Quantity
                        });
                }
            }
            Inventory = buffer;
            db.Update(this);
        }
    }
    public class HealthBlock {
        public int Level {get;set;} = 1;
        public int Extra {get;set;} = 0;
        public int Current {get;set;} = 5;

        [BsonIgnore]
        public Dictionary<int,int> values = new Dictionary<int, int>(){
            {0,0}, {1,1}, {2,1}, {3,1}, {4,2}, {5,2}, {6,3}, {7,3}, {8,4}, {9,4}, 
            {10,5}, {11,6}, {12,7}, {13,8}, {14,9}, {15,10}, {16,11}, {17,12}, {18,13}, {19,14}, {20,15},
            {21,16}, {22,17}, {23,18}, {24,19}, {25,20}, {26,21}, {27,22}, {28,29}, {29,24}, {30,25}
        };

        public int GetHealth(string Constitution){
            var tot = (values.GetValueOrDefault(int.Parse(Constitution))*Level)+Extra;
            return tot;
        }
    }
    public class AbScore{
        public int Value {get;set;} = 10;
        public int Extra {get;set;} = 0;
        public Proficiency Proficient {get;set;} = Proficiency.Untrained;
        public string GetValue(bool Int = false){
            if (Int){
                return (Value+Extra).ToString();
            }
            return String.Format("{0:00}",(Value + Extra));
        }
        public string IsProficient(){
            if (Proficient == Proficiency.Proficient) return "⭐";
            if (Proficient == Proficiency.Expert) return "🌟";
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
    public class Spell{
        public string Name {get;set;} = "Magic Missile";
        public string Description {get;set;} = "It fires a missile made of magic";
        public string Macro {get;set;} = "";
    }
    public class Skill {
        public string Name {get;set;}
        public AbilityShort Ability {get;set;}
        public Proficiency Proficiency {get;set;} = Proficiency.Untrained;
    }
    public class Item {
        [BsonId]
        public int Id {get;set;}
        public string Name {get;set;} = "";
        public string Description {get;set;} = "";
        public string Macro {get;set;} = "";
    }
    public class PlayerItem{
        public Item Item {get;set;}
        public int Quantity {get;set;} = 1;
    }
    public enum AbilityScores {Strength = 0, Dexterity = 1, Constitution = 2, Intelligance = 3, Wisdom = 4}
    public enum AbilityShort {STR = 0, DEX = 1, CON = 2, INT = 3, WIS = 4}
    public enum Proficiency {Untrained = 0, Proficient = 1, Expert = 2}
    public enum AttackType {Melee, Spell}

}