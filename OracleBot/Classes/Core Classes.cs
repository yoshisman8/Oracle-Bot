using System;
using System.Linq;
using System.Collections.Generic;
using DiceNotation;
using LiteDB;
using DiceNotation.MathNet;
using System.Text.RegularExpressions;

namespace OracleBot.Classes{
    public class Character{
        [BsonId]
        public int Id {get;set;}
        public ulong Owner {get;set;} = 165212654388903936;
        public string Name {get;set;} = "Joey";
        public string Class {get;set;} = "Wanderer";
        public string Race {get;set;} = "Racially Ambigous";
        public int MaxHP {get;set;} = 10;
        public int CurrHP {get;set;} = 10;
        public string Image {get;set;} = "https://media.discordapp.net/attachments/357593658586955776/411586696145272845/question-mark-clipart-transparent-3.png?width=337&height=559";
        public Level Level {get; set;} = new Level();
        public Statblock AbilityScores {get;set;} = new Statblock();
        public List<Skill> Skill {get;set;} = new List<Skill>(){};
        public List <Trait> Traits {get;set;} = new List<Trait>(){};
        public List<Effect> Aliments {get;set;} = new List<Effect>(){};
        public Dictionary<int,int> Inventory {get;set;} = new Dictionary<int, int>(){};
        public Decimal Money {get;set;} = 0;
        [BsonRef("Items")]
        public List<Item> Equipment {get;set;} = new List<Item>(){};


        public string BuildStat(Stats stat){
            if(stat == Stats.Might){
                int Total = AbilityScores.MGT;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == Stats.Might);
                    if (effects.Count() == 0) {
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
                    else{
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
            }
            else if(stat== Stats.Agility){
                int Total = AbilityScores.AGI;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat);
                    if (effects.Count() == 0) {
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
                    else{
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
            }
            else if(stat==Stats.Constitution){
                int Total = AbilityScores.CON;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat);
                    if (effects.Count() == 0) {
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
                    else{
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
            }
            else if (stat == Stats.Perception){
                int Total = AbilityScores.PER;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat);
                    if (effects.Count() == 0) {
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));                         
                        return subtotal.ToString();
                    }
                    else{
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
            }
            else if (stat == Stats.Magic){
                int Total = AbilityScores.MAG;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat);
                    if (effects.Count() == 0) {
                    var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                    return subtotal.ToString();
                    }
                    else{
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
            }
            else if (stat == Stats.Luck){
                int Total = AbilityScores.LCK;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    foreach (var x in Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat)) {
                        Total =+ int.Parse(x.Dice);
                    }
                    {
                        var subtotal = Math.Floor(Convert.ToDouble(Total/2));
                        return subtotal.ToString();
                    }
            }
            else if (stat == Stats.Fortitude){
                int Total = AbilityScores.Fort;
                var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat));
                    if (equips.Count() > 0) {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                        }
                    }
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat);
                    if (effects.Count() == 0) {
                        return (Total).ToString();
                    }
                    else{
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                        return (Total).ToString();
                    }
            }
            else if (stat == Stats.Protection){
                int Total = AbilityScores.Prot;
                    foreach (var x in this.Traits.Where(y => y.Effects.Exists(z => z.AffectedStat == stat))){
                        foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                            Total += int.Parse(y.Dice);
                        }
                    }
                    var effects = Aliments.Where(x => x.type == Status.Debuff && x.AffectedStat == stat);
                    if (effects.Count() > 0) {
                        foreach (var x in effects){
                            Total += int.Parse(x.Dice);
                        }
                    }
                    var equips = this.Equipment.Where(x => x.Effects.Exists(y => y.AffectedStat == stat) || x.ItemType == ItemType.Armor);
                    if (equips.Count() == 0){
                        return Total.ToString();
                    }
                    else {
                        foreach (var x in equips){
                            foreach (var y in x.Effects.Where(z => z.AffectedStat == stat)){
                                Total += int.Parse(y.Dice);
                            }
                            Total += x.Value;
                        }
                        return Total.ToString();
                    }
            }
            else {
                var hit = this.Level.CurrLevel;
                foreach (var x in this.Equipment.Where(x => x.ItemType == ItemType.Weapon)){
                    hit += x.Value;
                }
                return hit.ToString();
            }
        }
        public string CheckPoints(int Type = 0){
            switch (Type){
                case 1:
                    if (this.Level.StatPoints > 0) return "(Stat points remaining: "+Level.StatPoints+")";
                    else return "";
                default:
                    if (this.Level.SkillPoints > 0) return "(Skill points remaining: "+Level.SkillPoints+")";
                    else return "";
            }

        }
        public void Update(LiteDatabase database){
            var col = database.GetCollection<Character>("Characters");
            col.Update(this);
        }
    }

    public class Level {
        public int CurrLevel {get;set;} = 1;
        public int StatPoints {get;set;} = 18;
        public int SkillPoints {get;set;} = 3;
    }

    public class Statblock{
        public int MGT {get;set;} = 0;
        public int AGI {get;set;} = 0;
        public int CON {get;set;} = 0;
        public int PER {get;set;} = 0;
        public int MAG {get;set;} = 0;
        public int LCK {get;set;} = 0;
        public int Fort {get;set;} = 0;
        public int Prot {get;set;} = 0;

    }
    public class Skill{
        public string Name {get;set;} = " ";
        public string Description {get;set;} = " ";
        public int Level {get;set;} = 1;
        public Cooldown Cooldown {get;set;} = new Cooldown();
        public List<Effect> Effects {get;set;}= new List<Effect>(){};
        public Target Target {get;set;} = Target.Single;
    }
    public class Trait{
        public string Name {get;set;} = " ";
        public string Description {get;set;} = " ";
        public List<Effect> Effects {get;set;}= new List<Effect>(){};
    }
    public class Effect {
        public string Name {get;set;} = " ";
        public string Description {get;set;} = " ";
        public Status type {get;set;} = Status.Misc;
        public string Dice {get;set;} = " ";
        public int Turns {get;set;} = 1;
        public Stats AffectedStat {get;set;} = Stats.None;
    }
    public class Cooldown{
        public bool TimedOut {get;set;} = false;
        public int Timer {get;set;} = 0;
        public int MaxTime {get;set;} = 2;
    }
    public class Item {
        [BsonId]
        public int ID {get;set;}
        public string Name {get;set;} = " ";
        public string Image {get;set;} = "https://media.discordapp.net/attachments/357593658586955776/411586696145272845/question-mark-clipart-transparent-3.png?width=337&height=559";
        public string Description {get;set;} = " ";
        public ItemType ItemType {get;set;} = ItemType.Charm;
        public int Value {get;set;} = 0;
        public List<Effect> Effects {get;set;} = new List<Effect>(){};
    }
    public enum Status { Damage, Debuff, Heal, Misc, DmgOverTime, Restraint, ChanceOfSkip }
    public enum Stats { Might, Agility, Constitution, Perception, Magic, Luck, Fortitude, Protection, None}
    public enum ItemType { Weapon, Armor, Charm, Consumable, Ammo, Shield}
    public enum Target {Self, All, Single}
}