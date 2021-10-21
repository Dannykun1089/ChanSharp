//////////////////////////////////////
///   Internal Utility Functions   ///
//////////////////////////////////////


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
        // Some lines excluded due to no percieved usage
        public static string CleanCommentBody(string htmlComment)
        {
            // Replace breaklines with newline chars and remove tags
            htmlComment = htmlComment.Replace("<br>", "\n");
            htmlComment = new Regex(@"<.+?>").Replace(htmlComment, "");

            // Escape misc Html encoded substrings
            htmlComment = WebUtility.HtmlDecode(htmlComment);

            return htmlComment;
        }


        // Takes in a request to http(s)://a.4cdn.org/boards.json and returns the data reconstructed
        // In the Board.BoardsMetaData format specified in the JsonFormats doc
        public static JObject BoardsMetaDataFromRequest(HttpResponseMessage resp)
        {
            // Read the json response content into a JObject 
            JObject responseJson = JObject.Parse(resp.Content.ReadAsString());

            // Iterate over each of the boards and add them to the return value
            JObject retVal = new();
            foreach (JToken boardJson in responseJson["boards"])
            {
                // Add the board data as a value in a key value pair under its own name, E.G. 'a': { 'board': 'a', ... }
                retVal.Add(boardJson.Value<string>("board"), boardJson);
            }

            // Return the return value
            return retVal;
        }


        // CSharp be like "haha lets not include native array slicing like python" SCREEEEEEEEE
        public static T[] SliceArray<T>(T[] src, int start, int stop)
        {
            int newSize = stop - start;

            if (newSize < 0 || start >= src.Length || stop >= src.Length) { throw new IndexOutOfRangeException(); }
            if (newSize == 0) { return Array.Empty<T>(); }

            T[] retVal = new T[newSize];
            for (int i = 0; i < newSize; i++)
            {
                retVal[i] = src[start + i];
            }

            return retVal;
        }


        // Returns a new HttpClient with the appropriate headers for the wrapper
        public static HttpClient NewCSHttpClient()
        {
            HttpClient retVal = new();
            retVal.DefaultRequestHeaders.Add("User-Agent", "ChanSharp");
            return retVal;
        }
    }
}
