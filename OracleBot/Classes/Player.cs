using System;
using LiteDB;
using OracleBot;

namespace OracleBot.Classes
{
    public class Player 
    {
        [BsonId]
        public ulong DiscordId {get;set;}
        [BsonRef("Characters")]
        public Character LockedCharacter {get;set;} 
    }
}