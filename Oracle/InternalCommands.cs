using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace Oracle
{
    public class InternalCommands : BaseCommandModule
    {
        [Command("die"), Hidden, RequireOwner]
        public async Task DieCommand(CommandContext ctx)
        {
            await ctx.RespondAsync($"Bye, {ctx.User.Mention}!");
            await ctx.Client.DisconnectAsync();
            Program.cancellationTokenSource.Cancel();
        }

        [Command("say"), Hidden, RequireOwner]
        public async Task SayCommand(CommandContext ctx, [Description("The thing to say"), RemainingText] string text)
        {
            await ctx.Message.DeleteAsync();
            await ctx.RespondAsync(text);
        }
    }
}
