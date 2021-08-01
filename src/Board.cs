using System;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ChanSharp
{
    public class ChanSharpBoard
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private JObject                           MetaData       { get; set; }
        private HttpClient                        RequestsClient { get; }
        private UrlGenerator                      UrlGenerator   { get; }

        public   string                           Name           { get; }
        public   string                           Title          { get => Title_get();          }
        public   bool                             IsWorksafe     { get => IsWorksafe_get();     }
        public   int                              PageCount      { get => PageCount_get();      }
        public   int                              ThreadsPerPage { get => ThreadsPerPage_get(); }
        public   bool                             Https          { get; }
        public   string                           Protocol       { get; }

        internal Dictionary<int, ChanSharpThread> ThreadCache    { get; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public ChanSharpBoard(string boardName, bool https = true, HttpClient session = null)
        {
            this.Name        = boardName;
            this.Https       = https;
            this.Protocol    = https ? "https://" : "http://";

            this.RequestsClient = session ?? new HttpClient();
            this.UrlGenerator   = new UrlGenerator(boardName, https);

            this.ThreadCache = new Dictionary<int, ChanSharpThread>();

            this.RequestsClient.DefaultRequestHeaders.Add("User-Agent", "ChanSharp");
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return String.Format( "<Board /{0}/>",
                this.Name );
        }



        ////////////////////////
        ///   Type Methods   ///
        ////////////////////////

        public static Dictionary<string, ChanSharpBoard> GetBoards(string[] boardNameList, bool https = true, HttpClient session = null)
        {
            Dictionary<string, ChanSharpBoard> retVal = new Dictionary<string, ChanSharpBoard>();

            // Itterate over each board name, add dictionary entry {boardName: new BoardObject}
            foreach (string newBoardName in boardNameList)
            {
                retVal.Add(newBoardName, new ChanSharpBoard(newBoardName, https, session));
            }

            // Return the dictionary
            return retVal;
        }


        // System.Collections.Generic.List<string> overload variant
        public static Dictionary<string, ChanSharpBoard> GetBoards(List<string> boardNameList, bool https = true, HttpClient session = null)
        {
            Dictionary<string, ChanSharpBoard> retVal = new Dictionary<string, ChanSharpBoard>();

            // Itterate over each board name, add dictionary entry {boardName: new BoardObject}
            foreach (string newBoardName in boardNameList)
            {
                retVal.Add(newBoardName, new ChanSharpBoard(newBoardName, https, session));
            }

            // Return the dictionary
            return retVal;
        }


        public static Dictionary<string, ChanSharpBoard> GetAllBoards(bool https = true, HttpClient session = null)
        {
            // Request a list of all boards from 4Chan
            HttpClient requestsClient = session ?? new HttpClient();
            HttpResponseMessage resp = requestsClient.GetAsync( new UrlGenerator(null).BoardList() ).Result;

            // Parse the response content into a JObject
            string responseContent = resp.Content.ReadAsStringAsync().Result;
            JObject boardsJson = JObject.Parse( responseContent );

            // Itterate over each board and add its name to the list
            List<string> allBoards = new List<string>();
            foreach (JToken boardJson in boardsJson.Value<JArray>("boards"))
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
            if (this.MetaData != null) { return; }

            // Request the boards.json api data
            HttpResponseMessage resp = RequestsClient.GetAsync(urlGenerator.BoardList()).Result;
            resp.EnsureSuccessStatusCode();

            // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
            this.MetaData = Util.MetaDataFromRequest(resp);

            // Finish up
            resp.Dispose();
        }


        private JToken GetBoardMetadata(UrlGenerator urlGenerator, string board, string key)
        {
            FetchBoardsMetadata(urlGenerator);
            return MetaData[board][key];
        }


        private JToken GetMetaData(string key)
        {
            return GetBoardMetadata(UrlGenerator, Name, key);
        }


        private JToken GetJson(string url)
        {
            // Send request to the url, ensure successfull status code
            HttpResponseMessage resp = RequestsClient.GetAsync(url).Result;
            resp.EnsureSuccessStatusCode();

            // return the Json data as a JToken (JTokens can handle arrays and regular Json)
            string responseContent = resp.Content.ReadAsStringAsync().Result;
            return JToken.Parse(responseContent);
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
                    if (thread["last_replies"] == null)
                    {
                        posts = new JArray(thread);
                    }
                    else
                    {
                        posts = thread.Value<JArray>("last_replies");
                        thread.Remove("last_replies");
                        posts.Insert(0, thread);
                    }
                    threadsList.Add(JToken.Parse($"{{'posts': { posts } }}"));
                }
            }

            return threadsList;
        }


        private ChanSharpThread[] RequestThreads(string url)
        {
            // Request the url and turn the Json response into a JToken
            JToken Json = this.GetJson(url);

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
                    newThread = ChanSharpThread.FromJson(threadJson, this, lastModified: threadJson.Value<string>("last_modified"));
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
                ChanSharpThread newThread = ChanSharpThread.FromRequest(this.Name, resp, threadID);
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

            int[] threadIDs = GetAllThreadIDs();

            List<ChanSharpThread> threads = new List<ChanSharpThread>();
            foreach (int id in threadIDs)
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
