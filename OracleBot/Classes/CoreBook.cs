using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace OracleBot.Classes
{
    public class CoreBook
    {
        public List<Talent.Effect> Effects {get;set;} = new List<Talent.Effect>();
        public List<Talent.Modifier> Modifiers {get;set;} = new List<Talent.Modifier>();
    }
}