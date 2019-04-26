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
    public class Character : InteractiveBase<SocketCommandContext>
    {
        [BsonId]
        public int Id {get;set;}
        [BsonRef("Players")]
        public ulong Owner {get;set;}
        public string Name {get;set;}
        public AbilityScore[] AbilityScores {get;set;} = new AbilityScore[6];
        public int Level {get;set;} = 1; //Determines the skill ranks cap
        public int Armor {get;set;} = 0; //Flat mitigation of incomming damage
        public Pool HealthPool {get;set;} = new Pool(10);
        public Pool StaminaPool {get;set;} = new Pool(5);
        public List<Skill> Skills {get;set;} = new List<Skill>
        {
            new Skill("Athletics","A measure of your general athleticism and physical endurance. Used when running, Jumping, climbing, swimming and doing similar activites.",SkillType.Physical),
            new Skill("Acrobatics","A measure of your ability to move your body with grace, eleganceand dexteiry. Used during maneuvers such escaping grapples, moving through obstacles, making precies jumps, etc.",SkillType.Physical),
            new Skill("Crush","A measure of our ability to efficiency crush and pulverize things or people.",SkillType.Physical),
            new Skill("Slash","A measure of your ability to cut with sharp edges.",SkillType.Physical),
            new Skill("Stab","A measure of your ability to puncture objets or people in vital locations.",SkillType.Physical),
            new Skill("Aim","A measure of your ability to aim down and shoot down targets at a distance.",SkillType.Physical),
            new Skill("Throw","A measure of your ability to throw objects with accurace and power.",SkillType.Physical),
            new Skill("Block","A measure of the ability to properly brace yourself for incoming damage.",SkillType.Physical),
            new Skill("Evade","A measure of your ability to move out of danger on time.",SkillType.Physical),
            new Skill("Parry","A measure of your ability to receive an incomming damage using your own weapon and potentially performing a counter-attack.",SkillType.Physical),
            new Skill("Ride","A measure of your ability to ride vehicles and animals.",SkillType.Physical),
            new Skill("Stealth","A measure of your ability to move silently and undetected.",SkillType.Physical),
            new Skill("Bluff","A measure of your ability to convince others to believe your intents, regardless of whether they're true or not.",SkillType.Mental),
            new Skill("Diplomacy","A measure of your ability to calm down and talk reason into someone. Such as when trying to disengage a situation without violence or convince someone to see something through your point of view.",SkillType.Mental),
            new Skill("Sense Motive","A measure of your ability to discern true intent behind people's actions and words.",SkillType.Mental),
            new Skill("Perception","A measure of your ability to focus on and notice small, subtle details around you.",SkillType.Mental),
            new Skill("Recall","A measure of your ability to recall details about information you know.", SkillType.Mental),
            new Skill("Intimidate","A measure of your ability to exert pressure and terror on others.",SkillType.Mental)
        };
        public List<Talent> Talents {get;set;} = new List<Talent>();
        public Inventory Inventory {get;set;} = new Inventory();

        
        public void Save()
        {
            var Collection = Services.Database.GetCollection<Character>("Characters");
            Collection.Update(this);
        }
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
        public Ability Score {get;set;}
        public int Value {get;set;}
        public int Modifier
        {
            get
            {
                float value = (Modifier-10)/2;
                if (value<0) return (int)Math.Ceiling(value);
                else return (int)Math.Floor(value);
            }
            set
            {
            }
        }
    }
    public class Inventory
    {
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
        public int MainMax {get;set;}
        public int MainCurrent {get;set;}
        public int SubMax {get;set;}
        public int SubCurrent {get;set;}
        public Pool(int PoolMax, int BurnMax = 4)
        {
            MainMax = PoolMax;
            MainCurrent = MainMax;
            SubMax = BurnMax;
            SubCurrent = 0;
        }
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
        public string Description {get;set;}
        public SkillType Type {get;set;}
        [BsonIgnore]
        private DiceParser DiceParser {get;} = new DiceParser();
        public DiceResult Roll()
        {
            return DiceParser.Parse("2d6 + "+Ranks).Roll(); 
        }
        public Skill (string _Name, string _Description, SkillType _Type)
        {
            Name = _Name;
            Description = _Description;
            Type = _Type;
        }
    }
    public class Talent
    {
        public string Name {get;set;}
        public string Description {get;set;}
        public int Ranks {get;set;} = 1;
        public Talent(string _Name, string _Description)
        {
            Name = _Name;
            Description = _Description;
        }
    }
    public enum SkillType {Physical, Mental, Unafected}
    public enum Ability 
    {
        Might = 0, 
        MGT = 0, 
        Agility = 1, 
        AGI = 1,
        Endruance = 2,
        END = 2,
        Memory = 3,
        MEM = 3,
        Wisdom = 4,
        WIS = 4,
        Charisma =5,
        CHA = 5
        }
}