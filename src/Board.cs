using System;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace BascSharp4Chan
{
    public class Board
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private JObject                    MetaData        { get; set; }
        private HttpClient                 RequestsClient  { get; set; }
        private UrlGenerator               UrlGenerator    { get; set; }

        public string                  Name           { get; set; }
        public string                  Title          { get => Title_get();          }
        public bool                    IsWorksafe     { get => IsWorksafe_get();     }
        public int                     PageCount      { get => PageCount_get();      }
        public int                     ThreadsPerPage { get => ThreadsPerPage_get(); }
        public bool                    Https          { get; set; }
        public string                  Protocol       { get; set; }
        public Dictionary<int, Thread> ThreadCache    { get; set; }




        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public Board(string boardName, bool https = true, HttpClient session = null)
        {
            Name = boardName;
            Https = https;
            Protocol = https ? "https://" : "http://";
            ThreadCache = new Dictionary<int, Thread>();

            MetaData = new JObject();
            RequestsClient = session ?? new HttpClient();
            RequestsClient.DefaultRequestHeaders.Add("User-Agent", "ChanSharp");
            UrlGenerator = new UrlGenerator(boardName, https);
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return String.Format( "<Board /{0}/>",
                Name );
        }



        ////////////////////////
        ///   Type Methods   ///
        ////////////////////////

        public static Dictionary<string, Board> GetAllBoards(bool https = true, HttpClient session = null)
        {
            // Request board list Json
            HttpClient requestsClient = session ?? new HttpClient();
            HttpResponseMessage resp = requestsClient.GetAsync( new UrlGenerator(null).BoardList() ).Result;

            // Turn the Json string into a JObject in the Board.MetaData format
            JObject metaData = Util.MetaDataFromRequest(resp);

            // Itterate over each key value pair in the metadata and add the key (board name, E.G: "a", "aco", "d") to a list
            List<string> allBoards = new List<string>();
            foreach (KeyValuePair<string, JToken> metadataKVP in metaData)
            {
                allBoards.Add(metadataKVP.Key);
            }

            // pass the list of all the boards into the GetBoards method
            return GetBoards(allBoards.ToArray(), https, session);
        }


        public static Dictionary<string, Board> GetBoards(string[] boardNameList, bool https = true, HttpClient session = null)
        {
            Dictionary<string, Board> retVal = new Dictionary<string, Board>();

            // Itterate over each board name, add dictionary entry {boardName: BoardObject}
            foreach (string newBoardName in boardNameList)
            {
                retVal.Add(newBoardName, new Board(newBoardName, https, session));
            }

            // Return the dictionary
            return retVal;
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

            // Deserialize and reconstruct the boards.json response data, then asign it to this instance's MetaData property
            this.MetaData = Util.MetaDataFromRequest(resp);

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


        private JObject GetJSON(string url)
        {
            HttpResponseMessage resp = RequestsClient.GetAsync(url).Result;
            resp.EnsureSuccessStatusCode();
            string responseContent = resp.Content.ReadAsStringAsync().Result;

            return JObject.Parse(responseContent);
        }


        private JArray CatalogToThreads(JArray catalogJson)
        {
            JArray threadsJson = new JArray();
            JArray threadsList = new JArray();

            // Reconstruct the catalog.json into [INSERT FORMAT DEFINITION HERE]
            foreach (JToken page in catalogJson)
            {
                foreach (JToken thread in page["threads"])
                {
                    threadsJson.Add(thread);
                }
            }

            foreach (JToken thread in threadsJson)
            {
                threadsList.Add(JToken.Parse($"{{'posts': { thread } }}"));
            }

            foreach (JToken thread in threadsList)
            {
                thread["posts"][0]["last_replies"].Remove();
            }

            return threadsList;
        }


        private Thread[] RequestThreads(string url)
        {
            JToken json = GetJSONToken(url);

            Thread[] threadList;
            if (url == UrlGenerator.Catalog())
            {
                threadList = CatalogToThreads(JArray.FromObject(json));
                Console.WriteLine(threadList.Length);
            }
            else
            {
                threadList = BoardPageToThreads(JObject.FromObject(json));
            }

            List<Thread> threads = new List<Thread>();
            foreach (Thread thread in threadList)
            {
                Thread newThread;
                if (ThreadCache.ContainsKey(thread.ID))
                {
                    newThread = ThreadCache[thread.ID];
                    newThread.WantUpdate = true;
                }
                else
                {
                    newThread = thread;
                    ThreadCache.Add(thread.ID, thread);
                }

                threads.Add(thread);
            }
            return threads.ToArray();
        }



        ///////////////////////////////////
        ///   Public Instance Methods   ///
        ///////////////////////////////////

        public Thread GetThread(int threadID, bool updateIfCached = true, bool raise404 = false)
        {
            // Attempt to get cached thread
            Thread cachedThread = ThreadCache.ContainsKey(threadID) ? ThreadCache[threadID] : null;

            // Thread is not cached
            if (cachedThread is null)
            {
                // Make a request to http(s)://a.4cdn.org/{board}/thread/{threadID}.json
                HttpResponseMessage resp = RequestsClient.GetAsync(UrlGenerator.ThreadAPIUrls(threadID)).Result;

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
                Thread newThread = Thread.FromRequest(this.Name, resp, threadID);
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


        public Thread[] GetThreads(int page = 1)
        {
            string url = UrlGenerator.PageUrls(page);
            return RequestThreads(url);
        }


        public Thread[] GetAllThreads(bool expand = false)
        {
            if (!expand) { return RequestThreads(UrlGenerator.Catalog()); }

            int[] threadIDs = GetAllThreadIDs();

            List<Thread> threads = new List<Thread>();
            foreach (int id in threadIDs)
            {
                threads.Add(GetThread(id));
            }

            return threads.ToArray();
        }


        public int[] GetAllThreadIDs()
        {
            JArray json = GetJSONArray(UrlGenerator.ThreadList());

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
            string threadAPIURL = UrlGenerator.ThreadAPIUrls(threadID);
            return RequestsClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, threadAPIURL)).Result.IsSuccessStatusCode;
        }


        public void RefreshCache(bool ifWantUpdate = false)
        {
            foreach (Thread thread in ThreadCache.Values)
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
            return (string)GetMetaData("title");
        }

        private bool IsWorksafe_get()
        {
            return GetMetaData("ws_board") != null;
        }

        private int PageCount_get()
        {
            return (int)GetMetaData("pages");
        }

        private int ThreadsPerPage_get()
        {
            return (int)GetMetaData("per_page");
        }
    }
}
