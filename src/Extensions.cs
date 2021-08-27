using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace Extensions
{
    internal static class Extensions
    {
        ////////////////////////////////////
        ///   JToken Extension Methods   ///
        ////////////////////////////////////

        public static bool ContainsKey(this JToken jt, string key)
        {
            return jt[key] != null;
        }



        /////////////////////////////////////////
        ///   HttpContent Extension Methods   ///
        /////////////////////////////////////////

        public static string ReadAsString(this HttpContent content)
        {
            return content.ReadAsStringAsync().Result;
        }



        ////////////////////////////////////////
        ///   HttpClient Extension Methods   ///
        ////////////////////////////////////////

        public static HttpResponseMessage Get(this HttpClient cli, string url)
        {
            return cli.GetAsync(url).Result;
        }


        public static HttpResponseMessage Send(this HttpClient cli, HttpRequestMessage req)
        {
            return cli.SendAsync(req).Result;
        }
    }
}
