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

        public Embed BuildProfile(SocketCommandContext Context, LiteDatabase Database){
            var user = Context.Client.GetUser(DiscordId);
            var db = Database.GetCollection<Character>("Characters");
            db.EnsureIndex(x => x.Owner);
            var sb = new StringBuilder();
            var embed = new EmbedBuilder()
            .WithTitle(user.Username)
            .WithThumbnailUrl(user.GetAvatarUrl());
            foreach(var x in db.Find(x=> x.Owner == user.Id)){
                if (x.Name.ToLower() == Character.Name.ToLower()) sb.AppendLine("• **"+x.Name+"**");
                else sb.AppendLine("• "+x.Name);
            }
            if (sb.Length == 0) sb.Append(user.Username+" doesn't have any characters.");
            embed.AddField("Characters",sb.ToString(),true);
            sb.Clear();
            return embed.Build();
        }
    }
}