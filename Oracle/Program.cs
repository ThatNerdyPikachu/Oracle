using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Oracle
{
    class Program
    {
        public static JObject config = JObject.Parse(File.ReadAllText("config.json"));

        public static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public static CancellationToken cancellationToken = cancellationTokenSource.Token;

        static void Main()
        {
            try
            {
                RunBot().GetAwaiter().GetResult();
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        static async Task RunBot()
        {
            DiscordClient client = new DiscordClient(new DiscordConfiguration
            {
                Token = config["token"].ToString(),
                AutoReconnect = true,

                UseInternalLogHandler = true,
                LogLevel = LogLevel.Info
            });

            client.Ready += ClientReady;
            client.Heartbeated += ClientHeartbeated;

            client.UseInteractivity(new InteractivityConfiguration());

            CommandsNextExtension commands = client.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableMentionPrefix = true
            });

            commands.CommandErrored += CommandErrored;

            commands.RegisterCommands<SwitchCommands>();
            commands.RegisterCommands<OwnerCommands>();

            await client.ConnectAsync();

            await Task.Delay(-1, cancellationToken);
        }

        static async Task ClientReady(ReadyEventArgs eventArgs)
        {
            eventArgs.Client.DebugLogger.LogMessage(LogLevel.Info, "Oracle", "Client is connected to Discord!", DateTime.Now);

            await eventArgs.Client.UpdateStatusAsync(new DiscordActivity($"people get banned on {eventArgs.Client.Guilds.Count} " +
                $"{(eventArgs.Client.Guilds.Count == 1 ? "server" : "servers")}", ActivityType.Watching));
        }

        static async Task ClientHeartbeated(HeartbeatEventArgs eventArgs)
        {
            await eventArgs.Client.UpdateStatusAsync(new DiscordActivity($"people get banned on {eventArgs.Client.Guilds.Count} " +
                $"{(eventArgs.Client.Guilds.Count == 1 ? "server" : "servers")}", ActivityType.Watching));
        }

        static async Task CommandErrored(CommandErrorEventArgs eventArgs)
        {
            DiscordEmbedBuilder embed = new DiscordEmbedBuilder();

            if (eventArgs.Exception is ChecksFailedException)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Oops!",
                    Description = "You don't have permission to use that!",
                    Color = DiscordColor.Red
                };
            } else
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Oops!",
                    Description = $"The command ``{eventArgs.Command.Name}`` ran into an error while running: ``{eventArgs.Exception.Message}``",
                    Color = DiscordColor.Red
                };
            }

            await eventArgs.Context.RespondAsync(embed: embed);
        }
    }
}
