using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using OracleBot.Classes;
using LiteDB;
using Discord;
using System.Linq;

namespace OracleBot.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        private LiteDatabase _database;

        // DiscordSocketClient, CommandService, IConfigurationRoot, and IServiceProvider are injected automatically from the IServiceProvider
        public CommandHandler(
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config,
            IServiceProvider provider,
            LiteDatabase database)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _database = database;

            _discord.MessageReceived += OnMessageReceivedAsync;
            _discord.MessageUpdated += OnMessageUpdateAsync;
            _discord.Connected += OnConnectAsync;
        }

        private async Task OnConnectAsync()
        {
            var col = _database.GetCollection<Character>("Characters");
            var chars = col.FindAll();
            var filter = chars.Where(x=>x.AbilityScores.Length < 6);
            foreach (var x in chars){
                var c = new Character(){
                  Abilities = x.Abilities,
                  Class = x.Class,
                  CodeblockMode = true,
                  Color = x.Color,
                  Health = x.Health,
                  Image = x.Image,
                  Inventory = x.Inventory,
                  Money = x.Money,
                  Name = x.Name,
                  Owner = x.Owner,
                  Race = x.Race,
                  Skills = x.Skills,
                  Traits =x.Traits,
                  AbilityScores = new AbScore[6],
                  ID = x.ID
                };
                c.AbilityScores[0] = x.AbilityScores[0];
                c.AbilityScores[1] = x.AbilityScores[1];
                c.AbilityScores[2] = x.AbilityScores[2];
                c.AbilityScores[3] = x.AbilityScores[3];
                c.AbilityScores[4] = x.AbilityScores[4];
                c.AbilityScores[5] = new AbScore();
                col.Update(c);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        private async Task OnMessageUpdateAsync(Cacheable<IMessage, ulong> Original, SocketMessage msg, ISocketMessageChannel Channel)
        {
            if (msg == null) return;
            if (msg.Author.IsBot) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            await OnMessageReceivedAsync(msg);
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;     // Ensure the message is from a user/bot
            if (msg == null) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;     // Ignore self when checking commands
            
            var context = new SocketCommandContext(_discord, msg);     // Create the command context

            int argPos = 0;     // Check if the message has a valid command prefix
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);     // Execute the command

                if (!result.IsSuccess && result.Error == CommandError.BadArgCount)  {
                    var DMs = await context.User.GetOrCreateDMChannelAsync();
                    string command = msg.Content.Split(' ')[0].Substring(1);
                    var res = _commands.Search(context, command);

                    if (!res.IsSuccess)
                    {
                        await DMs.SendMessageAsync($"Sorry, I couldn't find a command like **{command}**.");
                        return;
                    }

                    string prefix = _config["prefix"];
                    var builder = new EmbedBuilder()
                    {
                        Color = new Color(114, 137, 218),
                        Description = $"Here are some commands like **{command}**\n"+
                            "Note: If any field you're writing is multi world (except for .addchar, .delchar and .char) make sure to wrap the word on quotation marks like this: `.NewSkill \"Super Attack\" \"Does some super attack\"`."
                    };

                    foreach (var match in res.Commands)
                    {
                        var cmd = match.Command;

                        builder.AddField(x =>
                        {
                            x.Name = string.Join(", ", cmd.Aliases);
                            x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" + 
                                    $"Summary: {cmd.Summary}";
                            x.IsInline = false;
                        });
                    }

                    await DMs.SendMessageAsync("", false, builder.Build());
                }   // If not successful, reply with the error.
                    
            }
        }
    }
}
