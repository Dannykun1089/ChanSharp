using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace ChanSharp
{
    using Extensions;

    public class Board
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private JObject BoardsMetadata { get; set; }
        private JArray ThreadsMetadata { get; set; }
        private DateTimeOffset ThreadsLastModified { get; set; }

        public string Name { get; }
        public string Title { get => Title_get(); }
        public bool IsWorksafe { get => IsWorksafe_get(); }
        public int PageCount { get => PageCount_get(); }
        public int ThreadsPerPage { get => ThreadsPerPage_get(); }
        public int ThreadCount { get => ThreadCount_get(); }
        public bool Https { get; }
        public string Protocol { get; }

        internal HttpClient RequestsClient { get; }
        internal UrlGenerator UrlGenerator { get; }
        internal Dictionary<int, Thread> ThreadCache { get; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public Board(string boardName, bool https = true, HttpClient session = null)
        {
            Name = boardName;
            Https = https;
            Protocol = https ? "https://" : "http://";

            ThreadsLastModified = DateTimeOffset.MinValue;

            RequestsClient = session ?? Util.newCSHttpClient();
            UrlGenerator = new UrlGenerator(boardName, https);
            ThreadCache = new Dictionary<int, Thread>();
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

        public static Dictionary<string, Board> GetBoards(string[] boardNames, bool https = true, HttpClient session = null, JObject boardsMetadata = null)
        {
            // If no boardsMetadata has been provided, hit http(s)://a.4cdn.org/boards.json for it
            if (boardsMetadata is null)
            {
                // Initialize HttpClient and UrlGenerator for static method
                HttpClient requestsClient = session ?? Util.newCSHttpClient();
                UrlGenerator urlGenerator = new UrlGenerator(null);

                // Request the boards.json api data and ensure success
                HttpResponseMessage resp = requestsClient.Get(urlGenerator.BoardList());
                resp.EnsureSuccessStatusCode();

                // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
                boardsMetadata = Util.BoardsMetadataFromRequest(resp);

                // Dispose of response
                resp.Dispose();
            }

            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            Dictionary<string, Board> boards = new Dictionary<string, Board>();
            foreach (string boardName in boardNames)
            {
                Board newBoard = new Board(boardName, https, session)
                {
                    BoardsMetadata = boardsMetadata
                };
                boards.Add(boardName, newBoard);
            }

            // Return the dictionary
            return boards;
        }


        // List<string> overload
        public static Dictionary<string, Board> GetBoards(List<string> boardNames, bool https = true, HttpClient session = null, JObject boardsMetadata = null)
        {
            // If no boardsMetadata has been provided, hit http(s)://a.4cdn.org/boards.json for it
            if (boardsMetadata is null)
            {
                // Initialize HttpClient and UrlGenerator for static method
                HttpClient requestsClient = session ?? Util.newCSHttpClient();
                UrlGenerator urlGenerator = new UrlGenerator(null);

                // Request the boards.json api data and ensure success
                HttpResponseMessage resp = requestsClient.Get(urlGenerator.BoardList());
                resp.EnsureSuccessStatusCode();

                // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
                boardsMetadata = Util.BoardsMetadataFromRequest(resp);

                // Dispose of response
                resp.Dispose();
            }

            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            Dictionary<string, Board> boards = new Dictionary<string, Board>();
            foreach (string boardName in boardNames)
            {
                Board newBoard = new Board(boardName, https, session)
                {
                    BoardsMetadata = boardsMetadata
                };
                boards.Add(boardName, newBoard);
            }

            // Return the dictionary
            return boards;
        }


        public static Dictionary<string, Board> GetAllBoards(bool https = true, HttpClient session = null)
        {
            // Initialize HttpClient and UrlGenerator for static method
            HttpClient requestsClient = session ?? Util.newCSHttpClient();
            UrlGenerator urlGenerator = new UrlGenerator(null);

            // Hit http(s)://a.4cdn.org/boards.json for a response
            HttpResponseMessage resp = requestsClient.Get(urlGenerator.BoardList());
            resp.EnsureSuccessStatusCode();

            // Parse the response content into the BoardsMetaData format
            JObject boardsMetadata = Util.BoardsMetadataFromRequest(resp);

            // Itterate over each board Json and add it's name to the list
            List<string> allBoards = new List<string>();
            foreach (KeyValuePair<string, JToken> boardJson in boardsMetadata)
            {
                allBoards.Add(boardJson.Key);
            }

            // pass the list of all the boards into the GetBoards method
            return GetBoards(allBoards, https, session, boardsMetadata);
        }



        ////////////////////////////////////
        ///   Private Instance Methods   ///
        ////////////////////////////////////

        // Hits 'http(s)://a.4cdn.org/boards.json' for boards Json data
        // Boards.json is never subject to change
        // Therefore we only need to check If the BoardsMetadata is null
        private void FetchBoardsMetadata(UrlGenerator urlGenerator)
        {
            // Return if there is already metadata
            if (BoardsMetadata != null) { return; }

            // Request the boards.json api data and ensure success
            HttpResponseMessage resp = RequestsClient.Get(urlGenerator.BoardList());
            resp.EnsureSuccessStatusCode();

            // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
            BoardsMetadata = Util.BoardsMetadataFromRequest(resp);

            // Dispose of IDisposables 
            resp.Dispose();
        }


        // Get boards data and return the metadata specified as a JToken, null if not present
        private JToken GetMetaData(string key)
        {
            FetchBoardsMetadata(UrlGenerator);
            return BoardsMetadata[Name].Value<JToken>(key);
        }


        // Takes in the /{board}/catalog.json Json data as a JArray and reconstructs it
        // Into a format more similar to /{board}/threads.json [NOTE: INSERT FORMAT DEFINITION]
        private JArray CatalogToThreads(JArray catalogJson)
        {
            JArray threadsList = new JArray();
            foreach (JObject pageJson in catalogJson)
            {
                foreach (JObject threadJson in pageJson.Value<JArray>("threads"))
                {
                    JArray posts;
                    if (threadJson.ContainsKey("last_replies"))
                    {
                        posts = threadJson.Value<JArray>("last_replies");
                        threadJson.Remove("last_replies");
                        posts.Insert(0, threadJson);
                    }
                    else
                    {
                        posts = new JArray(threadJson);
                    }
                    threadsList["posts"] = posts;
                }
            }
            return threadsList;
        }


        // Attempts to obtain an array of ChanSharp.Thread objects from a valid Api Url
        // ( {board}/catalog.json OR {board}/{pageNum}.json )
        private Thread[] RequestThreads(string url)
        {
            // Hit the Url for a response and ensure successful status code
            HttpResponseMessage resp = RequestsClient.Get(url);
            resp.EnsureSuccessStatusCode();
            string responseContent = resp.Content.ReadAsString();

            // If the Url is a catalog Url, call the CatalogToThreads() method to get the threadList
            JArray threadList;
            if (url == UrlGenerator.Catalog())
            {
                threadList = CatalogToThreads(JArray.Parse(responseContent));
            }
            // Else, the Url is a page Url, get the threadList from the 'threads' token
            else
            {
                threadList = JObject.Parse(responseContent).Value<JArray>("threads");
            }

            // Go over each thread Json object
            List<Thread> threads = new List<Thread>();
            foreach (JObject threadJson in threadList)
            {
                // Get the thread ID and Last-Modified values
                int id = threadJson["posts"][0].Value<int>("no");
                DateTimeOffset lastModified = DateTimeOffset.MinValue;
                if (threadJson.ContainsKey("last_modified"))
                {
                    lastModified = DateTimeOffset.FromUnixTimeSeconds(threadJson.Value<long>("last_modified"));
                }

                // If the thread ID is in the cache, retrieve it from the cache and set WantUpdate to true
                // Else, create a new thread object from the Json data and add it to the cache
                Thread newThread;
                if (ThreadCache.ContainsKey(id))
                {
                    newThread = ThreadCache[id];
                    newThread.WantUpdate = true;
                }
                else
                {
                    newThread = Thread.FromJson(threadJson, this, id, lastModified);
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

        public Thread GetThread(int threadID, bool updateIfCached = true, bool raise404 = false)
        {
            // Attempt to get cached thread
            Thread cachedThread = ThreadCache.ContainsKey(threadID) ? ThreadCache[threadID] : null;

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
                Thread newThread = Thread.FromRequest(Name, resp, threadID);
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

            // Itterate over all the thread IDs and call this.GetThread() for each of them
            List<Thread> threads = new List<Thread>();
            foreach (int id in GetAllThreadIDs())
            {
                threads.Add(GetThread(id));
            }

            return threads.ToArray();
        }


        public int[] GetAllThreadIDs()
        {
            // Hit http(s)://a.4cdn/{board}/threads.json for a response
            RequestsClient.DefaultRequestHeaders.IfModifiedSince = ThreadsLastModified;
            HttpResponseMessage resp = RequestsClient.Get(UrlGenerator.ThreadList());

            // Check the status code
            switch (resp.StatusCode)
            {
                // DEBUG
                case HttpStatusCode.NotModified:
                    Console.WriteLine("Nothing changed");
                    break;

                // Threads.json has changed since last requested, update the last-modified and cached data
                case HttpStatusCode.OK:
                    ThreadsLastModified = resp.Content.Headers.LastModified.Value;
                    ThreadsMetadata = JArray.Parse(resp.Content.ReadAsString());
                    break;

                // Somthing else has happened, raise for unsuccessful status code, then return empty array
                default:
                    resp.EnsureSuccessStatusCode();
                    return Array.Empty<int>();
            }

            // Itterate over each page in the cached data
            List<int> threadIDsList = new List<int>();
            foreach (JObject page in ThreadsMetadata)
            {
                // Itterate over each thread in the page and add the thread id to the list
                foreach (JToken thread in page["threads"])
                {
                    threadIDsList.Add(thread.Value<int>("no"));
                }
            }

            // Dispose of request and return the list of IDs as an array
            resp.Dispose();
            return threadIDsList.ToArray();
        }


        public bool ThreadExists(int threadID)
        {
            // Send a HEAD method Http request to the thread api Url to see if it goes through
            string threadAPIURL = UrlGenerator.ThreadApiUrl(threadID);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Head, threadAPIURL);
            return RequestsClient.Send(req).IsSuccessStatusCode;
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

        private int ThreadCount_get()
        {
            return GetAllThreadIDs().Length;
        }
    }
}
