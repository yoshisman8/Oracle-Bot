using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.Interactive;
using LiteDB;
using OracleBot.Classes;

namespace OracleBot.Modules
{
    [Name("Character Module")][Include]
    public class CharacterModule : InteractiveBase<SocketCommandContext>
    {
        [Command("NewCharacter"),Alias("NewChar","CreateCharacter","CreateChar","AddCharacter","AddChar")]
        public async Task Create([Remainder] string Name)
        {
            
        }
    }

}