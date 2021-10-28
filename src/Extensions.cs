using System.Net.Http;

namespace Extensions
{
    internal static class Extensions
    {
        /////////////////////////////////////////
        ///   HttpContent Extension Methods   ///
        /////////////////////////////////////////

        public static string ReadAsString(this HttpContent content)
        {
            return content.ReadAsStringAsync().Result;
        }


        public static byte[] ReadAsByteArray(this HttpContent content)
        {
            return content.ReadAsByteArrayAsync().Result;
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
