using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using LiteDB;

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
    public static class Services
    {
        public static LiteDatabase Database {get; private set;} = new LiteDatabase(@"Database.db");
    }
    public class CharacterTypeReader : TypeReader
    {
        public async override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var col = Services.Database.GetCollection<Character>("Characters");
            List<Character> results = col.Find(x=>x.Name.StartsWith(input.ToLower())).ToList();
            if (results.Count>0) return TypeReaderResult.FromError(CommandError.ParseFailed,"There is no character whose name begins with \""+input+"\"");
            return TypeReaderResult.FromSuccess(results);
        }
    }
    public class RequiresLockedChar : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var guild = Services.Database.GetCollection<Server>("Servers").IncludeAll().FindById(context.Guild.Id);
            if(!guild.ActivePlayers.Any(x=>x.DiscordId==context.User.Id)) guild.ActivePlayers.Add(new Player(context.User.Id));
            if (guild.ActivePlayers.Single(x=>x.DiscordId==context.User.Id).LockedCharacter == null) return PreconditionResult.FromError("You do not have an active character to perform this command with. Please use `"+guild.Prefix+"SetActive <Character Name>` and then use this command.");
            return PreconditionResult.FromSuccess();
        }
    }
    public class ToggleableModule : PreconditionAttribute
    {
        public async override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.Guild == null) return PreconditionResult.FromSuccess();
            var guild = Services.Database.GetCollection<Server>("Servers").FindById(context.Guild.Id);
            if (guild.Modules.Any(x=>x.Key==command.Module && x.Value == false)) return PreconditionResult.FromError("This Module is disabled.");
            return PreconditionResult.FromSuccess();
        }
    }
    public class Include : Attribute {}
}