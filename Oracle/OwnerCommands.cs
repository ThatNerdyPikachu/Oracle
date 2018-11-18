using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;
using static Oracle.Program;

namespace Oracle
{
    public class OwnerCommands : BaseCommandModule
    {
        [Command("die"), Hidden, RequireOwner]
        public async Task DieCommand(CommandContext ctx)
        {
            await ctx.RespondAsync($"Bye, {ctx.User.Mention}!");
            await ctx.Client.DisconnectAsync();
            cancellationTokenSource.Cancel();
        }
    }
}
