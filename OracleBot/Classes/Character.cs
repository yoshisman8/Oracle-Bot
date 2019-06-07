using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LiteDB;
using DiceNotation;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Discord.Addons.Interactive;

namespace OracleBot.Classes 
{
    public class Character
    {
        [BsonId]
        public int Id {get;set;}
        public ulong Owner {get;set;}
        public ulong Guild {get;set;}
        public string Name {get;set;}
        public string Class {get;set;} //Entirely Cosmetic
        public AbilityScore[] AbilityScores {get;set;} = new AbilityScore[7];
        public int Level {get;set;} = 1; //Determines the skill ranks cap
        public Pool Stamina {get;set;} = new Pool();
        public Pool Focus {get;set;} = new Pool();
        public List<Skill> Skills {get;set;} = new List<Skill>()
        {
            new Skill{Name="Athletics",Scores=new Ability[]{Ability.Strength,Ability.Constitution}},
            new Skill{Name="Acrobatics",Scores=new Ability[]{Ability.Agility,Ability.Dexterity}},
            new Skill{Name="Crush",Scores=new Ability[]{Ability.Strength,Ability.Constitution}},
            new Skill{Name="Slash",Scores=new Ability[]{Ability.STR,Ability.Dexterity}},
            new Skill{Name="Stab",Scores=new Ability[]{Ability.Strength,Ability.Dexterity}},
            new Skill{Name="Aim",Scores=new Ability[]{Ability.DEX,Ability.INT}},
            new Skill{Name="Throw",Scores=new Ability[]{Ability.STR,Ability.Dexterity}},
            new Skill{Name="Block",Scores=new Ability[]{Ability.Strength,Ability.Constitution}},
            new Skill{Name="Evade",Scores=new Ability[]{Ability.AGI,Ability.DEX}},
            new Skill{Name="Parry",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Ride",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Stealth",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Bluff",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Diplomacy",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Sense Motive",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Perception",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Recall",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Intimidate",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Survival",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            new Skill{Name="Heal",Scores=new Ability[]{Ability.DEX,Ability.Intution}},
            
        };
        public int SkillRanks {get;set;} = 0;
        public List<Talent> Talents {get;set;} = new List<Talent>();
        public Inventory Inventory {get;set;} = new Inventory();
      

        public Result RankUp(Skill skill, int Ranks = 1)
        {
            if (skill.Ranks + Ranks > Level) return new Result(false,"This skill is already at the maximum rank it can be for your level!");
            else
            {
                var index = Skills.IndexOf(skill);
                Skills[index].Ranks++;
                return new Result(true);
            }
        }
    }
    public class AbilityScore
    {
        public int Value {get;set;} = 1;
        public int Max {get;set;} = 7;
    }
    public class Inventory
    {
        public int Armor {get;set;} = 0;
        public List<InvItem> Items {get;set;} = new List<InvItem>();
        public Item GetItem(string Name)
        {
            if(Items.Exists(x=>x.Item.Name.ToLower().StartsWith(Name.ToLower())))
            {
                return Items.Find(x=>x.Item.Name.ToLower().StartsWith(Name.ToLower())).Item;
            }
            else return null;
        }
        public Result ConsumeItem(Item Item, int Amount)
        {
            int Index = Items.FindIndex(x=>x.Item == Item);
            if(Items[Index].Amount-Amount<0) return new Result(false,"You don't have this many "+Items[Index].Item.Name+"(s)!");
            else
            {
                Items[Index].Amount -= Amount;
                return new Result(true);
            }
        }
        public class InvItem
        {
            public Item Item {get;set;}
            public int Amount {get;set;}
        }
    }
    public class Item
    {
        public string Name {get;set;}
        public string Description {get;set;}
    }
    public class Pool
    {
        public int MainMax {get;set;} = 5;
        public int MainCurrent {get;set;} = 5;
        public int SubMax {get;set;} = 1;
        public int SubCurrent {get;set;} = 0;
        public void TakeDamage(int Amount)
        {
            MainCurrent -= Amount;
            if (MainCurrent<=0)
            {
                TakeSubDamage();
                MainCurrent = MainMax - Math.Abs(MainCurrent);
            }
        }
        public void TakeSubDamage()
        {
            if ((SubCurrent+1) > SubMax){
                SubCurrent++;
            }
        }
        public void Heal(int Amount = -1)
        {
            if (Amount == -1||MainCurrent+Amount>MainMax) MainCurrent = MainMax;
            else MainCurrent += Amount;
        }
        public void RestoreSub(int Amount = -1)
        {
            if (Amount == -1||SubCurrent-Amount>0) SubCurrent = 0;
            else MainCurrent -= Amount;
        }
    }
    public class Skill
    {
        public string Name {get;set;}
        public int Ranks {get;set;}
        public Ability[] Scores {get;set;} = new Ability[2];
        public string Description {get;set;}
        public bool Knowledge {get;set;} = false;

    }
    public class Talent
    {
        public string Name {get;set;}
        public string Description {get;set;}
        public int Level {get;set;} = 1;
        public int TalentCost {get;set;} = 0;
        public int RiskFactor {get;set;} = 0;
        public Trigger TalentTrigger {get;set;} = 0;
        public DamageType RiskDamage {get;set;}
        
        public enum Trigger {Simple, Complex, Reaction, Free}
        public class Effect
        {
            public string Name {get;set;}
            public string Description {get;set;}
            public int TP {get;set;}
            public int RF {get;set;}
        }
        public class Modifier
        {

        }
    }
    public enum DamageType {Physical, Mental}
    public enum Ability 
    {
        Strength = 0, 
        STR = 0, 
        Dexterity = 1,
        DEX = 1,
        Agility = 2, 
        AGI = 2,
        Constitution = 3,
        CON = 3,
        Memory = 4,
        MEM = 4,
        Intution = 4,
        INT = 4,
        Charisma = 5,
        CHA = 5
        }
}