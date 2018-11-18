using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Oracle
{
    class KeyDBUtils
    {
        public static List<KeyDBItem> GetKeyDB(string url)
        {
            List<KeyDBItem> titles = new List<KeyDBItem>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader responseStreamReader = new StreamReader(responseStream))
                    {
                        string line;
                        while ((line = responseStreamReader.ReadLine()) != null)
                        {
                            if (line.StartsWith("0100"))
                            {
                                titles.Add(new KeyDBItem(line));
                            }
                        }
                    }
                }
            }

            return titles;
        }

        public class KeyDBItem
        {
            // id|rightsId|key|isUpdate|isDLC|isDemo|baseName|name|version|region
            public string ID { get; }
            public string RightsID { get; }
            public string Key { get; }
            public bool IsUpdate { get; }
            public bool IsDLC { get; }
            public bool IsDemo { get; }
            public string BaseName { get; }
            public string Name { get; }
            public int Version { get; }
            public string Region { get; }

            public KeyDBItem(string line)
            {
                string[] item = line.Split('|');

                ID = item[0];
                RightsID = item[1];
                Key = item[2];
                IsUpdate = item[3] == "1" ? true : false;
                IsDLC = item[4] == "1" ? true : false;
                IsDemo = item[5] == "1" ? true : false;
                BaseName = item[6];
                Name = item[7];
                Version = int.Parse(item[8]);
                Region = item[9];
            }
        }
    }
}
