using System;
using System.Linq;
using System.Collections.Generic;
using DiceNotation;
using LiteDB;
using DiceNotation.MathNet;
using System.Text.RegularExpressions;

namespace OracleBot.Classes{
    public class Player{
        [BsonId]
        public int Id {get;set;}
    }
}