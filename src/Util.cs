using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ChanSharp
{
    using Extensions;

    internal class Util
    {
        public static string CleanCommentBody(string htmlComment)
        {
            return htmlComment;
        }


        /// <summary>
        /// Returns a JObject for use in the Board.MetaData propperty, available in util for static methods
        /// </summary>
        /// <param name="resp"> response object from a request made to 'http(s)://a.4cdn.org/boards.json' </param>
        /// <returns> a JObject for use in the Board.MetaData propperty, available in util for static methods </returns>
        public static JObject MetaDataFromRequest(HttpResponseMessage resp)
        {
            JObject retVal = new JObject();

            // Read the json response content into a JObject 
            JObject responseJson = JObject.Parse( resp.Content.ReadAsString() );

            // Iterate over each of the boards
            foreach (JToken boardJson in responseJson["boards"])
            {
                // Add the board data as a value in a key value pair under its own name, E.G. 'a': { 'board': 'a', ... }
                retVal.Add( boardJson.Value<string>("board"), boardJson );
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
            return Convert.FromBase64String(b64String);
        }


        // Neater looking version of the ArraySegment method
        public static T[] SliceArray<T>(T[] original, int offset)
        {
            return new ArraySegment<T>(original, offset, original.Length - offset).Array;
        }
    }
}
