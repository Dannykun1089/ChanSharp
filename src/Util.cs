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
        public static string CleanCommentBody(string HtmlComment)
        {
            string retVal = HtmlComment;

            Regex tagPattern = new Regex(@"<.+?>");
            //Regex linkPattern = new Regex(@"<a [^>]+>(.+?)</a>");

            retVal = retVal.Replace("<br>", "\n");
            retVal = tagPattern.Replace(retVal, "");
            //retVal = linkPattern.Replace(retVal, @"\1");

            // Escape misc Html encoded strings
            retVal = WebUtility.HtmlDecode(retVal);

            return retVal;
        }


        /// <summary>
        /// Returns a JObject for use in the Board.MetaData propperty, available in util for static methods
        /// </summary>
        /// <param name="resp"> response object from a request made to 'http(s)://a.4cdn.org/boards.json' </param>
        /// <returns> a JObject for use in the Board.MetaData propperty, available in util for static methods </returns>
        public static JObject BoardsMetadataFromRequest(HttpResponseMessage resp)
        {
            JObject retVal = new JObject();

            // Read the json response content into a JObject 
            JObject responseJson = JObject.Parse(resp.Content.ReadAsString());

            // Iterate over each of the boards
            foreach (JToken boardJson in responseJson["boards"])
            {
                // Add the board data as a value in a key value pair under its own name, E.G. 'a': { 'board': 'a', ... }
                retVal.Add(boardJson.Value<string>("board"), boardJson);
            }

            // Return the return value
            return retVal;
        }


        public static JObject[] JTokenArrayToJObjectArray(JToken[] jtokenArray)
        {
            JObject[] retVal = new JObject[jtokenArray.Length];

            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = JObject.FromObject(jtokenArray[i]);
            }
            return retVal;
        }


        public static byte[] Base64Decode(string b64String)
        {
            if (b64String == null) { return null; }
            return Convert.FromBase64String(b64String);
        }


        // CSharp be like "haha lets not include native array slicing like python" SCREEEEEEEEE
        public static T[] SliceArray<T>(T[] original, int offset)
        {
            // If index is less than 0 or more than the original array's length, throw out of bounds exception
            // If index is equal to original's length, return empty array
            if (offset < 0 || offset > original.Length) { throw new IndexOutOfRangeException(); }
            if (offset == original.Length) { return Array.Empty<T>(); }

            T[] retVal = new T[original.Length - offset];
            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = original[i + offset];
            }
            return retVal;
        }
    }
}
