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
using DiceNotation;

namespace OracleBot.Modules
{
    public class GuideModule : InteractiveBase<SocketCommandContext>
    {
        [Command("Guide", RunMode = RunMode.Async)]
        public async Task guide(){ 
            var guide = new CharacterGuide();
            var n = new Emoji("▶");
            var p = new Emoji("◀");
            var s = new Emoji("⏹");

            var msg = await ReplyAsync("",false,guide.Pages[0]);
            await msg.AddReactionAsync(p);
            await msg.AddReactionAsync(s);
            await msg.AddReactionAsync(n);

            Interactive.AddReactionCallback(msg,new InlineReactionCallback(Interactive,Context,new ReactionCallbackData("",guide.Pages[0],false,false,null)
                .WithCallback(p,(c,r) => guide.GoBack(r.Emote,c,msg))
                .WithCallback(s,(c,r) => guide.End(msg,c))
                .WithCallback(n,(c,r) => guide.Advance(r.Emote,c,msg))));
        }
    }
    public class CharacterGuide{
        public Embed[] Pages{get;set;} =
            new Embed[]{
                new EmbedBuilder()
                    .WithTitle("Character Creation Guide")
                    .WithColor(Color.Orange)
                    .WithTimestamp(DateTime.Now)
                    .WithDescription("In order to get started with making a character. We first need to give it a name. Use `.NewChar Your_Character's_Full_Name_Here` to create your character! Feel free to use spaces, no need to wrap the name in quotation marks.\nOnce you're done, click the ▶ reaction to move to the next step.")
                    .Build(),
                new EmbedBuilder()
                    .WithTitle("Character Creation Guide: Ability Scores")
                    .WithColor(Color.Orange)
                    .WithTimestamp(DateTime.Now)
                    .WithDescription("Now that we have the name of our character its time to define its 6 core ability scores. Below its a quick explanation of each ability score, give them a read!")
                    .AddInlineField("Strength (STR)","This score tracks the physical strength of your character. It is tied to melee attacks, Athletics and the ability to break or lift things.")
                    .AddInlineField("Dexterity (DEX)","This score tracks your character's hand-eye coordination as well as their general sense of agility and maneuverability. It is tied to your Defense Rating and ranged attacks as well as skills such as Acrobatics and it is also tied to your Reflex Saving throws.")
                    .AddInlineField("Constitution (CON)","This score tracks your character's overal healthiness. It is tied to the amount of HP you get as well as to your Fortitude Saving Throws.")
                    .AddInlineField("Intelligence (INT)","This score tracks your character's overall memory and information retention capabilities. It is tied to any Knowledge skill checks as well as checks like SpellCraft or Hacking.")
                    .AddInlineField("Wisdom (WIS)","This score tracks your character's awareness of their surroundings and their mental resilience. It is tied to your Will saving throws as well as skillchecks that involve awareness of your environment like Sense Motive, Survival or Perception.")
                    .AddInlineField("Charisma (CHA)","This score tracks your character's overall ability to project their will and personality onto the world. Although it encompases most Social skill checks such as Bluff, Diplomacy and Intimidation, It is also used whenever you wish to exhert control over forces that you don't exactly comprehend (Such as a sorcerer commanding the magic within them to do something despite not being studied).")
                    .AddField("Picking your Scores","When it comes to picking ability scores, 10 is considered the absolute average. Ehile Values such as 7 or 8 Are often considered the lowest your character should go, indicating a clear handicap on that aspect of them. While values like 14 to 16 are seen as clear advantages. Ideally, you don't want to have too many high scores without at least one or two scores to 'balance' yourself out.\nHowever you can also let the die decide your scores for you. Here are 6 random numbers that you can use for your ability scores: "+generatescores()+". Once you've decided what scores you wish to have, use the command `.SetScore ScoreAbreviation Value` to set each one of your ability scores. \nOnce you're done, click the ▶ reaction to move to the next step.")
                    .Build(),
                new EmbedBuilder()
                    .WithTitle("Character Creation Guide: Traits, Feats and Abilities")
                    .WithColor(Color.Orange)
                    .WithTimestamp(DateTime.Now)
                    .WithDescription("You might be wondering what's the difference between all of these. Well mechanically, they have a heirarchy of Abilities > Feats & Traits. Here's a quick breakdown of each one of them so you're more well informed.")
                    .AddInlineField("Traits","A trait is an aspect of your character that's entirely passive and is often outside of their control. These are more commonly physical traits or background/social traits (such as upbringing or knowing someone or being part of a certain faction). You can add or update one of these to your character by using `.NewTrait Name Description` or `.DelTrait Name` to remove one that's outdated.")
                    .AddInlineField("Feats","Feats are aspects of your character that make them stand out from the crowd. These are often things your character has trained or had some degree of choice in the matter. Good examples are things like Weapon training, Spell Resistance, Skill Focus and so on. You can add or update one of these to your character by using `.NewFeat Name Description` or `.DelFeat Name` to remove one that's outdated.")
                    .AddInlineField("Abilities","Unlike the other two, Ablities have a lot more impact into how your character plays. These tend to come from your character's profession or class. Such as a wizard having the  \"Magic School: Fire\" as an Ability or a Rouge having a \"Sneak Attack\" ability to add damage to their attacks. All in all, Abilities are your character's special, well, abilities. You can add or update one of these to your character by using `.NewAbility Name Description` or `.DelAbility Name` to remove one that's outdated.")
                    .AddField("Time to add your own!","Now that you know what each one of them are, use the commands described above to add at least one of each!")
                    .Build(),
                new EmbedBuilder()
                    .WithTitle("Character Creation Guide: Finishing Touches")
                    .WithColor(Color.Orange)
                    .WithTimestamp(DateTime.Now)
                    .WithDescription("We're almost done! Let's cover up some of the last things about your character. These being Good and Poor Saving throws, Base Attack Bonus and Base HP.")
                    .AddInlineField("Saving Throws","You have Three saving throws: Reflex (Scales with DEX), Fortitude (Scales with CON) and Will (Scales with Wisdom). Reflex is used when you are given the chance to dodge out of the way of something, such as an AOE attack or a trap. Fortitude is used whenenver you're resisting the effects of poisons or when your physical condition is being tested. Will is used whenever you're resisting things that are affecting your mind.")
                    .AddInlineField("Good and Poor saving throws","A saving throw can be either 'Good' or 'Poor'. This means that the base bonus you get (Before you add your DEX/CON/WIS) goes up at diferent rates. A Poor saving throw only gets 1/3rd of your character's level added to it (rounded down). A good saving throw however, gets your character's level +4 multiplied by 0.5 instead. Typically a character has at least one good saving throw and at best two. Never three or none. To change a saving throw to be good or bad you can use `.ToggleSave SavingThrowName`.")
                    .AddInlineField("Base Attack Bonus", "Your Base Attack Bonus is, as the name implies, a bonus you get to your attacks before you apply your Strength (For melee attacks) or Dexterity (For ranged attacks). This bonus is either 3/4 of your level or 1-to-1 with your level. You can toggle this by using the command `.ToggleBaB`")
                    .AddInlineField("Base Hit Points","Your max Hit Points (Or HP) is determined by a base value (Defaults to 6) which is multiplied by your level. 6 Tends to be the average number for most Professions or Classes, 4 Is often used for 'Squishy' classes like spellcasters while 8 or even 10 is used for more hardy classes like Warriors or Barbarians. You can change your Base Hp with `.SetBaseHP Value`.")
                    .AddField("All done!","Feel free to click on the ⏹ icon in order to end this tutorial, or use ◀ or ▶ to go back to a previous or next page.")
            };
            public int index {get;set;} = 0;
            
            public async Task Advance(IEmote reaction, SocketCommandContext context, IUserMessage msg){
                if (index == Pages.Length-1){
                    await msg.RemoveReactionAsync(reaction,context.User);
                    return;
                }
                else {
                    index++;
                    await msg.ModifyAsync(x => x.Embed = Pages[index]);
                    await msg.RemoveReactionAsync(reaction,context.User);
                    return;
                }
            }
            public async Task GoBack(IEmote reaction, SocketCommandContext context, IUserMessage msg){
                if (index == 0){
                    await msg.RemoveReactionAsync(reaction,context.User);
                    return;
                }
                else {
                    index--;
                    await msg.ModifyAsync(x => x.Embed = Pages[index]);
                    await msg.RemoveReactionAsync(reaction,context.User);
                    return;
                }
            }
            public async Task End(IUserMessage msg,SocketCommandContext context){
                await msg.DeleteAsync();
                await context.Message.DeleteAsync();
            }
            public static string generatescores(){
            var parser = new DiceParser();
            var sb = new StringBuilder();
            for (int i = 0; i <6 ; i++){
                var result = parser.Parse("4d6k3").Roll();
                sb.Append("[**"+result.Value+"**] ");
            }
            return sb.ToString();
        }   
    }
}