using LiBCAT;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Oracle
{
    class NewsUtils
    {
        private static Bcat.News.Region usRegion = new Bcat.News.Region
        {
            Country = Bcat.News.Countries.UnitedStates,
            Language = Bcat.News.Languages.AmericanEnglish
        };

        public static List<JToken> GetNintendoNewsArticles()
        {
            List<JToken> articles = JObject.Parse(Bcat.News.GetNxNewsList(usRegion))["directories"][0]["data_list"].ToObject<List<JToken>>();

            if (!Directory.Exists("news_articles"))
            {
                Directory.CreateDirectory("news_articles");
            }

            if (!Directory.Exists("news_articles\\nx_news"))
            {
                Directory.CreateDirectory("news_articles\\nx_news");
            }

            List<JToken> list = new List<JToken>();

            foreach (JToken a in articles)
            {
                if (!File.Exists($"news_articles\\nx_news\\{a["news_id"].ToString()}.json"))
                {
                    File.WriteAllText($"news_articles\\nx_news\\{a["news_id"].ToString()}.json",
                        JToken.Parse(Bcat.GetData(a["languages"][0]["url"].ToString(), true, Bcat.QLaunchTID, Encoding.ASCII.GetString(Bcat.NewsPassphrase)).ToString()).ToString());
                }

                list.Add(JToken.Parse(File.ReadAllText($"news_articles\\nx_news\\{a["news_id"].ToString()}.json")));
            }

            return list;
        }

        public static List<JToken> GetTopics()
        {
            List<JToken> entries = new List<JToken>();

            foreach (JToken t in JArray.Parse(Bcat.News.GetCatalog(usRegion)))
            {
                entries.Add(t);
            }

            return entries;
        }

        public static List<JToken> GetArticles(string topicID)
        {
            JObject articles = JObject.Parse(Bcat.News.GetNewsList(usRegion, topicID));

            if (!Directory.Exists("news_articles"))
            {
                Directory.CreateDirectory("news_articles");
            }

            if (!Directory.Exists($"news_articles\\{articles["topic_id"].ToString()}"))
            {
                Directory.CreateDirectory($"news_articles\\{articles["topic_id"].ToString()}");
            }

            List<JToken> list = new List<JToken>();

            foreach (JToken a in articles["directories"][0]["data_list"])
            {
                if (!File.Exists($"news_articles\\{articles["topic_id"].ToString()}\\{a["news_id"].ToString()}.json"))
                {
                    File.WriteAllText($"news_articles\\{articles["topic_id"].ToString()}\\{a["news_id"].ToString()}.json",
                        JToken.Parse(Bcat.GetData(a["languages"][0]["url"].ToString(), true, Bcat.QLaunchTID, Encoding.ASCII.GetString(Bcat.NewsPassphrase)).ToString()).ToString());
                }

                list.Add(JToken.Parse(File.ReadAllText($"news_articles\\{articles["topic_id"].ToString()}\\{a["news_id"].ToString()}.json")));
            }

            return list;
        }
    }
}
