using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ChanSharp
{
    using Extensions;

    internal class Util
    {
        internal static string CleanCommentBody(string htmlComment)
        {
            // Replace breaklines with newline chars and remove tags
            htmlComment = htmlComment.Replace("<br>", "\n");
            htmlComment = Regex.Replace(htmlComment, @"<.+?>", string.Empty);

            // Escape misc Html encoded substrings
            htmlComment = WebUtility.HtmlDecode(htmlComment);

            return htmlComment;
        }


        // Takes in a request to http(s)://a.4cdn.org/boards.json and returns the data reconstructed
        // In the Board.BoardsMetaData format specified in the JsonFormats doc
        internal static JObject BoardsMetaDataFromRequest(HttpResponseMessage resp)
        {
            JObject returnValue = new JObject();

            JObject responseJson = JObject.Parse(resp.Content.ReadAsString());
            foreach (JToken boardJson in responseJson["boards"])
            {
                returnValue.Add(boardJson.Value<string>("board"), boardJson);
            }

            return returnValue;
        }


        // CSharp be like "haha lets not include native array slicing like python" SCREEEEEEEEE
        internal static T[] SliceArray<T>(T[] src, int start, int stop)
        {
            int newSize = stop - start;

            if (newSize < 0 || start >= src.Length || stop >= src.Length) { throw new IndexOutOfRangeException(); }
            if (newSize == 0) { return Array.Empty<T>(); }

            T[] returnValue = new T[newSize];
            for (int i = 0; i < newSize; i++)
            {
                returnValue[i] = src[i + start];
            }

            return returnValue;
        }


        // Returns a new HttpClient with the appropriate headers for the wrapper
        internal static HttpClient NewCSHttpClient()
        {
            HttpClient returnValue = new HttpClient();
            returnValue.DefaultRequestHeaders.Add("User-Agent", "ChanSharp");
            return returnValue;
        }
    }
}
