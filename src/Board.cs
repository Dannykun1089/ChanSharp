using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace ChanSharp
{
    using Extensions;
    public class Board
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private JObject MetaData { get; set; }
        private UrlGenerator UrlGenerator { get; }

        public string Name { get; }
        public string Title { get => Title_get(); }
        public bool IsWorksafe { get => IsWorksafe_get(); }
        public int PageCount { get => PageCount_get(); }
        public int ThreadsPerPage { get => ThreadsPerPage_get(); }
        public bool Https { get; }
        public string Protocol { get; }

        internal HttpClient RequestsClient { get; }
        internal Dictionary<int, ChanSharpThread> ThreadCache { get; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public Board(string boardName, bool https = true, HttpClient session = null)
        {
            Name = boardName;
            Https = https;
            Protocol = https ? "https://" : "http://";

            UrlGenerator = new UrlGenerator(boardName, https);

            RequestsClient = session ?? new HttpClient();
            ThreadCache = new Dictionary<int, ChanSharpThread>();

            RequestsClient.DefaultRequestHeaders.Add("User-Agent", "ChanSharp");
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return $"<Board /{ Name }/>";
        }



        ////////////////////////
        ///   Type Methods   ///
        ////////////////////////

        public static Dictionary<string, Board> GetBoards(string[] boardNameList, bool https = true, HttpClient session = null)
        {
            Dictionary<string, Board> retVal = new Dictionary<string, Board>();

            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            foreach (string newBoardName in boardNameList)
            {
                retVal.Add( newBoardName, new Board(newBoardName, https, session) );
            }

            // Return the dictionary
            return retVal;
        }


        // System.Collections.Generic.List<string> overload
        public static Dictionary<string, Board> GetBoards(List<string> boardNameList, bool https = true, HttpClient session = null)
        {
            Dictionary<string, Board> retVal = new Dictionary<string, Board>();

            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            foreach (string newBoardName in boardNameList)
            {
                retVal.Add( newBoardName, new Board(newBoardName, https, session) );
            }

            // Return the dictionary
            return retVal;
        }


        public static Dictionary<string, Board> GetAllBoards(bool https = true, HttpClient session = null)
        {
            // Request a list of all boards from 4Chan
            HttpClient requestsClient = session ?? new HttpClient();
            UrlGenerator urlGenerator = new UrlGenerator(null);
            HttpResponseMessage resp  = requestsClient.Get( urlGenerator.BoardList() );

            // Parse the response content into a JObject
            string responseContent = resp.Content.ReadAsString();
            JObject boardsJson = JObject.Parse( responseContent );

            // Itterate over each board Json and add it's name to the list
            List<string> allBoards = new List<string>();
            foreach (JObject boardJson in boardsJson.Value<JArray>("boards"))
            {
                allBoards.Add(boardJson.Value<string>("board"));
            }

            // pass the list of all the boards into the GetBoards method
            return GetBoards(allBoards, https, session);
        }



        ////////////////////////////////////
        ///   Private Instance Methods   ///
        ////////////////////////////////////

        private void FetchBoardsMetadata(UrlGenerator urlGenerator)
        {
            // Return if there is already metadata
            if (MetaData != null) { return; }

            // Request the boards.json api data
            HttpResponseMessage resp = RequestsClient.Get( urlGenerator.BoardList() );
            resp.EnsureSuccessStatusCode();

            // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
            MetaData = Util.MetaDataFromRequest(resp);

            // Finish up
            resp.Dispose();
        }


        private JToken GetMetaData(string key)
        {
            FetchBoardsMetadata(UrlGenerator);
            return MetaData[Name][key];
        }


        private JToken GetJson(string url)
        {
            // Send request to the url, ensure successfull status code
            HttpResponseMessage resp = RequestsClient.Get( url );
            resp.EnsureSuccessStatusCode();

            // return the Json data as a JToken (JTokens can handle arrays and regular Json)
            string responseContent = resp.Content.ReadAsString();
            return JToken.Parse( responseContent );
        }


        private JArray CatalogToThreads(JArray catalogJson)
        {
            JArray threadsList = new JArray();

            // Reconstruct the catalog.json into [INSERT FORMAT DEFINITION HERE]
            foreach (JToken page in catalogJson)
            {
                foreach (JObject thread in page.Value<JArray>("threads"))
                {
                    JArray posts;
                    if ( thread.ContainsKey("last_replies") )
                    {
                        posts = thread.Value<JArray>("last_replies");
                        thread.Remove("last_replies");
                        posts.Insert(0, thread);
                    }
                    else
                    {
                        posts = new JArray(thread);
                    }
                    threadsList.Add(JToken.Parse($"{{ 'posts': { posts } }}"));
                }
            }

            return threadsList;
        }


        private ChanSharpThread[] RequestThreads(string url)
        {
            // Request the url and turn the Json response into a JToken
            JToken Json = GetJson(url);

            // If the Url is a catalog url, call the catalogToThreads method
            // Else get it from the 'threads' Json propperty
            JArray threadList;
            if (url == UrlGenerator.Catalog())
            {
                threadList = CatalogToThreads(JArray.FromObject(Json));
            }
            else
            {
                threadList = JArray.FromObject(Json["threads"]);
            }

            // Go over each thread Json object
            List<ChanSharpThread> threads = new List<ChanSharpThread>();
            foreach (JToken threadJson in threadList)
            {
                // Get the thread ID
                int id = threadJson["posts"][0].Value<int>("no");

                // If the thread ID is in the cache, retrieve it from the cache and set WantUpdate to true
                // Else, create a new thread object from the Json data and add it to the cache
                ChanSharpThread newThread;
                if (ThreadCache.ContainsKey(id))
                {
                    newThread = ThreadCache[id];
                    newThread.WantUpdate = true;
                }
                else
                {
                    newThread = ChanSharpThread.FromJson(threadJson, this, id, threadJson.Value<string>("last_modified"));
                    ThreadCache.Add(id, newThread);
                }

                // Add the new thread to the list
                threads.Add(newThread);
            }

            // Return the list as an array
            return threads.ToArray();
        }



        ///////////////////////////////////
        ///   Public Instance Methods   ///
        ///////////////////////////////////

        public ChanSharpThread GetThread(int threadID, bool updateIfCached = true, bool raise404 = false)
        {
            // Attempt to get cached thread
            ChanSharpThread cachedThread = ThreadCache.ContainsKey(threadID) ? ThreadCache[threadID] : null;

            // Thread is not cached
            if (cachedThread is null)
            {
                // Make a request to http(s)://a.4cdn.org/{board}/thread/{threadID}.json
                HttpResponseMessage resp = RequestsClient.GetAsync(UrlGenerator.ThreadApiUrl(threadID)).Result;

                // Check if the request was ok
                if (raise404)
                {
                    resp.EnsureSuccessStatusCode();
                }
                else if (!resp.IsSuccessStatusCode)
                {
                    return null;
                }

                // Get the thread from the request and insert it into the thread cache
                ChanSharpThread newThread = ChanSharpThread.FromRequest(Name, resp, threadID);
                ThreadCache.Add(threadID, newThread);

                // Dispose of the request and return the thread
                resp.Dispose();
                return newThread;
            }
            // Thread is cached

            // Update the thread and reinsert the updated thread into the thread cache
            if (updateIfCached)
            {
                cachedThread.Update();
                ThreadCache.Add(threadID, cachedThread);
            }

            return cachedThread;
        }


        public ChanSharpThread[] GetThreads(int page = 1)
        {
            string url = UrlGenerator.PageUrls(page);
            return RequestThreads(url);
        }


        public ChanSharpThread[] GetAllThreads(bool expand = false)
        {
            if (!expand) { return RequestThreads(UrlGenerator.Catalog()); }

            // Itterate over all the thread IDs and call this.GetThread() for each of them
            List<ChanSharpThread> threads = new List<ChanSharpThread>();
            foreach (int id in GetAllThreadIDs())
            {
                threads.Add(GetThread(id));
            }

            return threads.ToArray();
        }


        public int[] GetAllThreadIDs()
        {
            JToken json = GetJson(UrlGenerator.ThreadList());

            List<int> threadIDsList = new List<int> { };
            foreach (JToken page in json)
            {
                foreach (JToken thread in page["threads"])
                {
                    threadIDsList.Add((int)thread["no"]);
                }
            }

            return threadIDsList.ToArray();
        }


        public bool ThreadExists(int threadID)
        {
            string threadAPIURL = UrlGenerator.ThreadApiUrl(threadID);
            return RequestsClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, threadAPIURL)).Result.IsSuccessStatusCode;
        }


        public void RefreshCache(bool ifWantUpdate = false)
        {
            foreach (ChanSharpThread thread in ThreadCache.Values)
            {
                if (ifWantUpdate)
                {
                    if (thread.WantUpdate) { thread.Update(); }
                }
            }
        }


        public void ClearCache()
        {
            foreach (int threadID in ThreadCache.Keys)
            {
                ThreadCache.Remove(threadID);
            }
        }



        /////////////////////////////////
        ///   Property get; Methods   ///
        /////////////////////////////////

        private string Title_get()
        {
            return GetMetaData("title").ToObject<string>();
        }

        private bool IsWorksafe_get()
        {
            return GetMetaData("ws_board").ToObject<int>() == 1;
        }

        private int PageCount_get()
        {
            return GetMetaData("pages").ToObject<int>();
        }

        private int ThreadsPerPage_get()
        {
            return GetMetaData("per_page").ToObject<int>();
        }
    }
}
