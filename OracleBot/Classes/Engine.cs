using System;
using Discord;
using Discord.Commands;

namespace OracleBot.Classes
{
    public class Result : IResult
    {
        public CommandError? Error => throw new NotImplementedException();
        public string ErrorReason {get;}
        public object TargetItem {get;}
        public bool IsSuccess {get;}
        public Result(bool _IsSuccess,string Error = "", object obj = null)
        {
            IsSuccess = _IsSuccess;
            ErrorReason = Error;
            TargetItem = obj;
        }
    }
}