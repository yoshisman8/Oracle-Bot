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
    public class Item_Management : InteractiveBase<SocketCommandContext>
    {
        public LiteDatabase LiteDatabase {get;set;}
        
    }
}