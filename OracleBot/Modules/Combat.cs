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

namespace OracleBot.Modules
{
    public class Combat : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase Database {get;set;}

        [Command("NewAttack")]
        [Summary("Adds a new attack to your character's attack list. Usage: `.NewAttack Name Description Type`. Valid types are 'Melee' and 'Magic' and 'Mixed'.")]
        public async Task NewAttack(string Name, string Description, AttackType Type = AttackType.Melee){
            
        }
    }
}