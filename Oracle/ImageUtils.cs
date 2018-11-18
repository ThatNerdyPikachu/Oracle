using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace Oracle
{
    class ImageUtils
    {
        public static string UploadBase64Image(string clientID, string image)
        {
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers["Authorization"] = $"Client-ID {clientID}";

                NameValueCollection requestParams = new NameValueCollection
                {
                    { "image", image },
                    { "type", "base64" }
                };

                byte[] response = webClient.UploadValues("https://api.imgur.com/3/image", requestParams);

                JObject parsedResponse = JObject.Parse(Encoding.ASCII.GetString(response));

                return parsedResponse["data"]["link"].ToString();
            }
        }
    }
}
