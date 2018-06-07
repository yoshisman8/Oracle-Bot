using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using LiteDB;
using Discord;
using Discord.Commands;

namespace OracleBot.Classes
{
    public class player{
        [BsonId]
        public ulong DiscordId {get;set;}
        [BsonRef("Characters")]
        public Character Character {get;set;}
        [BsonRef("Characters")]
        public List<Character> Characters {get;set;}
        public List<Item> ItemVault {get;set;}

        public Embed BuildProfile(SocketCommandContext Context, LiteDatabase Database){
            var user = Context.Client.GetUser(DiscordId);
            var sb = new StringBuilder();
            var embed = new EmbedBuilder()
            .WithTitle(user.Username)
            .WithThumbnailUrl(user.GetAvatarUrl());
            if (Character == null) embed.AddField("Locked as","No one",true);
            else embed.AddField("Locked as",Character.Name,true);
            foreach(var x in Characters){
                sb.AppendLine("• "+x.Name);
            }
            if (sb.Length == 0) sb.Append(user.Username+" doesn't have any characters.");
            embed.AddField("Characters",sb.ToString(),true);
            sb.Clear();
            foreach(var x in ItemVault){
                sb.AppendLine("• "+x.Name);
            }
            if (sb.Length == 0) sb.Append(user.Username+" doesn't have any Items in their Vault.");
            embed.AddField("Item Vault",sb.ToString(),true);
            return embed.Build();
        }
    }
}