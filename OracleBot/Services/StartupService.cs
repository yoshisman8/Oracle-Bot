using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using OracleBot.Classes;

namespace OracleBot.Services
{
    public class StartupService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public StartupService(
            DiscordSocketClient discord,
            CommandService commands,
            IConfigurationRoot config)
        {
            _config = config;
            _discord = discord;
            _commands = commands;
        }

        public async Task StartAsync(ServiceProvider Provider)
        {
            string discordToken = _config["tokens:discord"];     // Get the discord token from the config file
            if (string.IsNullOrWhiteSpace(discordToken))
                throw new Exception("Please enter your bot's token into the `_configuration.json` file found in the applications root directory.");

            await _discord.LoginAsync(TokenType.Bot, discordToken);     // Login to discord
            await _discord.SetGameAsync(_config["status"]); 
            await _discord.StartAsync();                                // Connect to the websocket
            _commands.AddTypeReader(typeof(List<Character>),new CharacterTypeReader()); //Adds custom TypeReader for Characters
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(),Provider);     // Load commands and modules into the command service
        }
    }
}
