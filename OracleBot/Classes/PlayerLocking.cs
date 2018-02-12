using System;
using LiteDB;
using System.Collections.Generic;
using System.Linq;
using OracleBot.Classes;

namespace OracleBot.Classes
{
    public class Lock
    {
        [BsonId]
        public ulong DiscordId {get;set;}
        public int CharacterId {get;set;}
    }

    public static class PlayerLocking{
        public static void LockPayer(Character Character, ulong Owner, LiteDatabase Database){
            var col = Database.GetCollection<Lock>("Players");
            
            if(!col.Exists(x => x.DiscordId == Owner)){
                col.Insert(new Lock(){
                    DiscordId = Owner,
                    CharacterId = Character.Id
                });
            }
            else {
                var C = col.FindOne(x => x.DiscordId == Owner);
                C.CharacterId = Character.Id;
                col.Update(C);
            }
        }
        public static Character GetLock(LiteDatabase database, ulong Player){
            var col = database.GetCollection<Lock>("Players");
            var col2= database.GetCollection<Character>("Characters");
            try {
                var C = col.FindOne(x => x.DiscordId == Player);
                return col2.FindOne(x => x.Id == C.CharacterId);
            }
            catch{
                return null;
            }
        }
    }
}