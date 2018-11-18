using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Humanizer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Oracle.ImageUtils;
using static Oracle.KeyDBUtils;
using static Oracle.NewsUtils;
using static Oracle.NotCDNUtils;
using static Oracle.Program;

namespace Oracle
{
    class SwitchCommands : BaseCommandModule
    {
        private List<KeyDBItem> keyDB = GetKeyDB(config["keydb_url"].ToString());

        static readonly string notCDNCreds = Convert.ToBase64String(Encoding.ASCII.GetBytes(config["not_cdn_creds"].ToString()));
        private List<JToken> baseFileList = GetNotCDNFileList($"{config["not_cdn_base_url"].ToString()}/base/", notCDNCreds);
        private List<JToken> updateFileList = GetNotCDNFileList($"{config["not_cdn_base_url"].ToString()}/updates/", notCDNCreds);
        private List<JToken> dlcFileList = GetNotCDNFileList($"{config["not_cdn_base_url"].ToString()}/dlc/", notCDNCreds);

        [Command("keydb")]
        public async Task KeyDBCommand(CommandContext ctx, [RemainingText] string name)
        {
            DiscordMessage message = await ctx.RespondAsync("Please wait...");

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            List<KeyDBItem> items = keyDB.Where(x => x.Name.ToLower().Contains(name.ToLower()) && !x.IsUpdate).Take(25).ToList();

            if (items.Count == 0)
            {
                await message.ModifyAsync("No results found!", embed: null);
                return;
            }

            KeyDBItem item;
            DiscordEmbedBuilder embed;

            if (items.Count == 1)
            {
                item = items[0];
            } else
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Make a selection:",
                    Color = DiscordColor.DarkButNotBlack
                };

                for (int i = 0; i < items.Count; i++)
                {
                    embed.AddField($"{i + 1}. {items[i].Name}", $"ID: {items[i].ID}\nRegion: {items[i].Region}");
                }

                await message.ModifyAsync(null, embed: embed.Build());

                MessageContext responseMessageContext = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Member);
                await ctx.Channel.DeleteMessageAsync(responseMessageContext.Message);

                try
                {
                    item = items[int.Parse(responseMessageContext.Message.Content) - 1];
                }
                catch
                {
                    await message.ModifyAsync("Selection canceled.", embed: null);
                    return;
                }
            }

            await message.ModifyAsync("Please wait...", embed: null);

            embed = new DiscordEmbedBuilder
            {
                Title = "Title Information",
                Color = DiscordColor.DarkButNotBlack
            };

            embed.AddField("ID", item.ID);
            embed.AddField("Rights ID", item.RightsID);
            embed.AddField("Key", item.Key);
            embed.AddField("Name", item.Name);
            embed.AddField("Version", item.Version.ToString());
            embed.AddField("Region", item.Region);

            await message.ModifyAsync(null, embed: embed.Build());
        }

        [Command("nsp")]
        public async Task NSPCommand(CommandContext ctx, string type, [RemainingText] string name)
        {
            DiscordMessage message = await ctx.RespondAsync("Please wait...");

            InteractivityExtension interactivity = ctx.Client.GetInteractivity();

            type = type.ToLower();

            if (type != "base" && type != "update" && type != "dlc")
            {
                await message.ModifyAsync($"Invalid title type ``{type}``! Valid title types are: ``base``, ``update``, and ``dlc``.", embed: null);
                return;
            }

            List<JToken> files = new List<JToken>();

            switch (type) {
                case "base":
                    files = baseFileList;
                    break;
                case "update":
                    files = updateFileList;
                    type = "updates";
                    break;
                case "dlc":
                    files = dlcFileList;
                    break;
            }

            List<JToken> results = files.Where(x => x["Name"].ToString().ToLower().Contains(name.ToLower())).Take(25).ToList();

            if (results.Count == 0)
            {
                await message.ModifyAsync("No results found!", embed: null);
                return;
            }

            JToken file;
            
            if (results.Count == 1)
            {
                file = results[0];
            } else
            {
                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "Make a selection:",
                    Color = DiscordColor.Yellow
                };

                for (int i = 0; i < results.Count; i++)
                {
                    embed.AddField($"{i + 1}. {results[i]["Name"].ToString().Split('[')[0]}", $"Full name: {results[i]["Name"].ToString()}");
                }

                await message.ModifyAsync(null, embed: embed.Build());

                MessageContext responseMessageContext = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Member);
                await ctx.Channel.DeleteMessageAsync(responseMessageContext.Message);

                try
                {
                    file = files[int.Parse(responseMessageContext.Message.Content) - 1];
                }
                catch
                {
                    await message.ModifyAsync("Selection canceled.", embed: null);
                    return;
                }
            }

            await message.ModifyAsync("Please wait...", embed: null);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://not-cdn.now.im/{type}/{file["URL"].ToString().Substring(2, file["URL"].ToString().Length - 2)}");
            request.Accept = "application/json";
            request.Headers["Authorization"] = $"Basic {notCDNCreds}";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    await ctx.Channel.DeleteMessageAsync(message);
                    await ctx.RespondWithFileAsync(file["Name"].ToString(), responseStream);
                }
            }
        }

        [Command("reload"), Hidden, RequireOwner]
        public async Task ReloadCommand(CommandContext ctx)
        {
            DiscordMessage message = await ctx.RespondAsync("Reloading databases...");

            keyDB = GetKeyDB(config["keydb_url"].ToString());

            baseFileList = GetNotCDNFileList($"{config["not_cdn_base_url"].ToString()}/base/", notCDNCreds);
            updateFileList = GetNotCDNFileList($"{config["not_cdn_base_url"].ToString()}/updates/", notCDNCreds);
            dlcFileList = GetNotCDNFileList($"{config["not_cdn_base_url"].ToString()}/dlc/", notCDNCreds);

            await message.ModifyAsync("Done!");
    }

        [Group("news")]
        public class NewsCommandGroup : BaseCommandModule
        {
            [Command("nintendo")]
            public async Task NintendoNewsCommand(CommandContext ctx)
            {
                DiscordMessage message = await ctx.RespondAsync("Please wait...");

                InteractivityExtension interactivity = ctx.Client.GetInteractivity();

                List<JToken> articles = GetNintendoNewsArticles();
                articles.Reverse();
                articles = articles.Take(25).ToList();

                DiscordEmbedBuilder embed = new DiscordEmbedBuilder
                {
                    Title = "Make a selection:",
                    Color = DiscordColor.Red
                };

                for (int i = 0; i < articles.Count; i++)
                {
                    embed.AddField($"{i + 1}. {articles[i]["subject"]["text"].ToString()}",
                        $"Published {DateTimeOffset.FromUnixTimeSeconds(articles[i]["published_at"].ToObject<long>()).Humanize()}");
                }

                await message.ModifyAsync(null, embed: embed.Build());

                MessageContext responseMessageContext = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Member);
                await ctx.Channel.DeleteMessageAsync(responseMessageContext.Message);

                await message.ModifyAsync("Please wait...", embed: null);

                JToken article;

                try
                {
                    article = articles[int.Parse(responseMessageContext.Message.Content) - 1];
                }
                catch
                {
                    await message.ModifyAsync("Selection canceled.", embed: null);
                    return;
                }

                embed = new DiscordEmbedBuilder
                {
                    Title = article["subject"]["text"].ToString(),
                    Color = DiscordColor.Red,
                }
                .WithFooter(article["footer"]["text"].ToString());

                if (!File.Exists("news_articles\\nx_news\\image.txt"))
                {
                    File.WriteAllText("news_articles\\nx_news\\image.txt", UploadBase64Image(config["imgur_client_id"].ToString(), article["topic_image"].ToString()));
                }

                embed = embed.WithAuthor(article["topic_name"].ToString(), iconUrl: File.ReadAllText($"news_articles\\nx_news\\image.txt"));

                string text = (article["body"] is JArray ? article["body"][0]["text"] : article["body"]["text"]).ToString();

                if (text.ToString().Length >= 2048)
                {
                    embed = embed.WithDescription($"{text.Substring(0, 2045).Replace("<strong>", "**").Replace("</strong>", "**")}...");
                }
                else
                {
                    embed = embed.WithDescription(text.Replace("<strong>", "**").Replace("</strong>", "**"));
                }

                if (!File.Exists($"news_articles\\nx_news\\{article["news_id"].ToString()}_image.txt"))
                {
                    File.WriteAllText($"news_articles\\nx_news\\{article["news_id"].ToString()}_image.txt",
                        UploadBase64Image(config["imgur_client_id"].ToString(), (article["body"] is JArray ? article["body"][0]["main_image"] : article["body"]["main_image"]).ToString()));
                }

                embed = embed.WithImageUrl(File.ReadAllText($"news_articles\\nx_news\\{article["news_id"].ToString()}_image.txt"));

                await message.ModifyAsync(null, embed: embed.Build());
            }

            [Command("title")]
            public async Task TitleNewsCommand(CommandContext ctx, [RemainingText] string name)
            {
                DiscordMessage message = await ctx.RespondAsync("Please wait...");

                InteractivityExtension interactivity = ctx.Client.GetInteractivity();

                List<JToken> topics = GetTopics().Where(x => x["name"].ToString().ToLower().Contains(name.ToLower())).Take(25).ToList();

                if (topics.Count == 0)
                {
                    await message.ModifyAsync("No results found!", embed: null);
                    return;
                }

                JToken topic;
                DiscordEmbedBuilder embed;

                if (topics.Count == 1)
                {
                    topic = topics[0];
                } else
                {
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Make a selection:",
                        Color = DiscordColor.Red
                    };

                    for (int i = 0; i < topics.Count; i++)
                    {
                        embed.AddField($"{i + 1}. {topics[i]["name"].ToString()}", $"{topics[i]["description"].ToString()} (Topic ID: ``{topics[i]["topic_id"].ToString()}``)");
                    }

                    await message.ModifyAsync(null, embed: embed.Build());

                    MessageContext responseMessageContext = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Member);
                    await ctx.Channel.DeleteMessageAsync(responseMessageContext.Message);

                    try
                    {
                        topic = topics[int.Parse(responseMessageContext.Message.Content) - 1];
                    }
                    catch
                    {
                        await message.ModifyAsync("Selection canceled.", embed: null);
                        return;
                    }
                }

                await message.ModifyAsync("Please wait...", embed: null);

                List<JToken> articles = GetArticles(topic["topic_id"].ToString());
                articles.Reverse();
                articles = articles.Take(25).ToList();

                JToken article;

                if (articles.Count == 1)
                {
                    article = articles[0];
                } else
                {
                    embed = new DiscordEmbedBuilder
                    {
                        Title = "Make a selection:",
                        Color = DiscordColor.Red
                    };

                    for (int i = 0; i < articles.Count; i++)
                    {
                        embed.AddField($"{i + 1}. {articles[i]["subject"]["text"].ToString()}",
                            $"Published {DateTimeOffset.FromUnixTimeSeconds(articles[i]["published_at"].ToObject<long>()).Humanize()}");
                    }

                    await message.ModifyAsync(null, embed: embed.Build());

                    MessageContext responseMessageContext = await interactivity.WaitForMessageAsync(x => x.Author == ctx.Member);
                    await ctx.Channel.DeleteMessageAsync(responseMessageContext.Message);

                    try
                    {
                        article = articles[int.Parse(responseMessageContext.Message.Content) - 1];
                    }
                    catch
                    {
                        await message.ModifyAsync("Selection canceled.", embed: null);
                        return;
                    }
                }

                await message.ModifyAsync("Please wait...", embed: null);

                embed = new DiscordEmbedBuilder
                {
                    Title = article["subject"]["text"].ToString(),
                    Color = DiscordColor.Red,
                }
                .WithFooter(article["footer"]["text"].ToString());

                if (!File.Exists($"news_articles\\{topic["topic_id"].ToString()}\\image.txt"))
                {
                    File.WriteAllText($"news_articles\\{topic["topic_id"].ToString()}\\image.txt", UploadBase64Image(config["imgur_client_id"].ToString(), article["topic_image"].ToString()));
                }

                embed = embed.WithAuthor(article["topic_name"].ToString(), iconUrl: File.ReadAllText($"news_articles\\{topic["topic_id"].ToString()}\\image.txt"));

                string text = (article["body"] is JArray ? article["body"][0]["text"] : article["body"]["text"]).ToString();

                if (text.ToString().Length >= 2048)
                {
                    embed = embed.WithDescription($"{text.Substring(0, 2045).Replace("<strong>", "**").Replace("</strong>", "**")}...");
                }
                else
                {
                    embed = embed.WithDescription(text.Replace("<strong>", "**").Replace("</strong>", "**"));
                }

                if (!File.Exists($"news_articles\\{topic["topic_id"].ToString()}\\{article["news_id"].ToString()}_image.txt"))
                {
                    File.WriteAllText($"news_articles\\{topic["topic_id"].ToString()}\\{article["news_id"].ToString()}_image.txt",
                        UploadBase64Image(config["imgur_client_id"].ToString(), (article["body"] is JArray ? article["body"][0]["main_image"] : article["body"]["main_image"]).ToString()));
                }

                embed = embed.WithImageUrl(File.ReadAllText($"news_articles\\{topic["topic_id"].ToString()}\\{article["news_id"].ToString()}_image.txt"));

                await message.ModifyAsync(null, embed: embed.Build());
            }
        }
    }
}
