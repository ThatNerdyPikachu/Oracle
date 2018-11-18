using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Oracle
{
    class NotCDNUtils
    {
        public static List<JToken> GetNotCDNFileList(string url, string creds)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "application/json";
            request.Headers["Authorization"] = $"Basic {creds}";

            List<JToken> files = new List<JToken>();

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader responseStreamReader = new StreamReader(responseStream))
                    {
                        JArray titles = JArray.Parse(responseStreamReader.ReadToEnd());
                        foreach (JToken t in titles)
                        {
                            files.Add(t);
                        }
                    }
                }
            }

            return files;
        }
    }
}
