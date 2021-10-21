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
        private DateTimeOffset? ThreadsLastModified { get; set; }

        public string Name { get; }
        public string Title { get => Title_get(); }
        public bool IsWorksafe { get => IsWorksafe_get(); }
        public int PageCount { get => PageCount_get(); }
        public int ThreadsPerPage { get => ThreadsPerPage_get(); }
        public bool Https { get; }

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

            ThreadsLastModified = null;

            RequestsClient = session ?? Util.NewCSHttpClient();
            UrlGenerator = new(boardName, https);
            ThreadCache = new();
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return string.Format("<Board /{0}/>",
                                 Name);
        }



        ////////////////////////
        ///   Type Methods   ///
        ////////////////////////

        public static Dictionary<string, Board> GetBoards(string[] boardNames, bool https = true, HttpClient session = null, JObject boardsMetadata = null)
        {
            // If no boardsMetadata has been provided, hit http(s)://a.4cdn.org/boards.json for it
            if (boardsMetadata is null)
            {
                // Create new Http client and Url generator for static method
                HttpClient requestsClient = session ?? Util.NewCSHttpClient();
                UrlGenerator urlGenerator = new(null);

                // Request the boards.json api data and ensure success
                HttpResponseMessage resp = requestsClient.Get(urlGenerator.BoardList());
                resp.EnsureSuccessStatusCode();

                // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
                boardsMetadata = Util.BoardsMetaDataFromRequest(resp);

                // Dispose of response
                resp.Dispose();
            }

            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            Dictionary<string, Board> boards = new();
            foreach (string boardName in boardNames)
            {
                Board newBoard = new(boardName, https, session)
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
            return GetBoards(boardNames.ToArray(), https, session, boardsMetadata);
        }


        public static Dictionary<string, Board> GetAllBoards(bool https = true, HttpClient session = null)
        {
            // Initialize Http Client and Url Generator for static method
            HttpClient requestsClient = session ?? Util.NewCSHttpClient();
            UrlGenerator urlGenerator = new(null);

            // Hit http(s)://a.4cdn.org/boards.json for a response
            HttpResponseMessage resp = requestsClient.Get(urlGenerator.BoardList());
            resp.EnsureSuccessStatusCode();

            // Parse the response content into the BoardsMetaData format
            JObject boardsMetadata = Util.BoardsMetaDataFromRequest(resp);

            // Itterate over each board Json and add it's name to the list
            List<string> allBoards = new();
            foreach (KeyValuePair<string, JToken> boardJson in boardsMetadata)
            {
                allBoards.Add(boardJson.Key);
            }

            // pass the list of all the boards into the GetBoards method
            return GetBoards(allBoards, https, session, boardsMetadata);
        }



        ///////////////////////////////////
        ///   Public Instance Methods   ///
        ///////////////////////////////////

        public Thread GetThread(int threadID, bool updateIfCached = true, bool raise404 = false)
        {
            // Attempt to get cached thread
            Thread returnValue;
            if (ThreadCache.TryGetValue(threadID, out returnValue))
            {
                // Thread is cached
                if (updateIfCached)
                {
                    // Update the thread and reinsert the updated thread into the thread cache
                    returnValue.Update();
                    ThreadCache.Add(threadID, returnValue);
                }

                // Return the cached thread
                return returnValue;
            }

            // Thread is not cached
            // Make a request to http(s)://a.4cdn.org/{board}/thread/{threadID}.json
            HttpResponseMessage resp = RequestsClient.Get(UrlGenerator.ThreadApiUrl(threadID));

            // Check if the request was ok
            if (raise404) { resp.EnsureSuccessStatusCode(); }
            else if (!resp.IsSuccessStatusCode) { return null; }

            // Get the thread from the request and insert it into the thread cache
            returnValue = Thread.FromRequest(this, resp, threadID);
            ThreadCache.Add(threadID, returnValue);

            // Dispose of the request and return the thread
            resp.Dispose();
            return returnValue;
        }


        public Thread[] GetThreads(int page)
        {
            string url = UrlGenerator.PageUrls(page);
            return RequestThreads(url);
        }


        public Thread[] GetAllThreads(bool expand = false)
        {
            if (!expand) { return RequestThreads(UrlGenerator.Catalog()); }

            // Itterate over all the thread IDs and call GetThread() for each of them
            List<Thread> threads = new();
            foreach (int id in GetAllThreadIds())
            {
                threads.Add(GetThread(id));
            }

            return threads.ToArray();
        }


        public int[] GetAllThreadIds()
        {
            // Hit http(s)://a.4cdn/{board}/threads.json for a response
            RequestsClient.DefaultRequestHeaders.IfModifiedSince = ThreadsLastModified;
            HttpResponseMessage resp = RequestsClient.Get(UrlGenerator.ThreadList());

            // Check the status code (NOTE: if a 200-299 code is thrown, no idea what to do with that) 
            switch (resp.StatusCode)
            {
                // Threads.json has changed since last requested, update the last-modified and cached data
                case HttpStatusCode.OK:
                    ThreadsLastModified = resp.Content.Headers.LastModified.Value;
                    ThreadsMetadata = JArray.Parse(resp.Content.ReadAsString());
                    break;

                // Threads.json has not changed, ThreadsMetaData stays the same
                case HttpStatusCode.NotModified:
                    break;

                // Another code has been thrown, throw if not successful
                default:
                    resp.EnsureSuccessStatusCode();
                    break;
            }


            // Itterate over each page in the cached data
            List<int> threadIdsList = new();
            foreach (JObject page in ThreadsMetadata)
            {
                // Itterate over each thread in the page and add the thread id to the list
                foreach (JToken thread in page["threads"])
                {
                    threadIdsList.Add(thread.Value<int>("no"));
                }
            }

            // Dispose of request and return the list of IDs as an array
            resp.Dispose();
            return threadIdsList.ToArray();
        }


        public bool ThreadExists(int threadID)
        {
            // Send a HEAD method Http request to the thread api Url to see if it goes through
            string threadApiUrl = UrlGenerator.ThreadApiUrl(threadID);
            HttpRequestMessage request = new(HttpMethod.Head, threadApiUrl);
            return RequestsClient.Send(request).IsSuccessStatusCode;
        }


        public void RefreshCache()
        {
            foreach (Thread thread in ThreadCache.Values)
            {
                if (thread.WantUpdate) { thread.Update(); }
            }
        }


        public void ClearCache()
        {
            foreach (int threadID in ThreadCache.Keys)
            {
                ThreadCache.Remove(threadID);
            }
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
            BoardsMetadata = Util.BoardsMetaDataFromRequest(resp);

            // Dispose of request
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
        private static JArray CatalogToThreads(JArray catalogJson)
        {
            JArray threadsList = new();
            foreach (JObject pageJson in catalogJson)
            {
                foreach (JObject threadJson in pageJson.Value<JArray>("threads"))
                {
                    JArray posts = threadJson.Value<JArray>("last_replies");
                    
                    if (posts is null)
                    {
                        posts = threadJson.ToObject<JArray>();
                    }
                    else
                    {
                        threadJson.Remove("last_replies");
                        posts.Insert(0, threadJson);
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

            JArray threadList;
            if (url == UrlGenerator.Catalog())
            {
                // The Url is a catalog Url, call the CatalogToThreads() method to get the threadList
                threadList = CatalogToThreads(JArray.Parse(responseContent));
            }
            else
            {
                // The Url is a page Url, get the threadList from the 'threads' token
                threadList = JObject.Parse(responseContent).Value<JArray>("threads");
            }


            // Go over each thread Json object
            List<Thread> threads = new();
            foreach (JObject threadJson in threadList)
            {
                // Get the thread ID and Last-Modified values
                int id = threadJson["posts"][0].Value<int>("no");
                long lastModifiedUnix = threadJson.Value<long>("last_modified");
                DateTimeOffset? lastModified = lastModifiedUnix == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(lastModifiedUnix);

                // Check the cache for the thread
                Thread newThread;
                if (ThreadCache.TryGetValue(id, out newThread))
                {
                    // The thread ID is in the cache, retrieve it from the cache and set WantUpdate to true
                    newThread.WantUpdate = true;
                }
                else
                {
                    // Create a new thread object from the Json data and add it to the cache
                    newThread = Thread.FromJson(this, threadJson, id, lastModified);
                    ThreadCache.Add(id, newThread);
                }
                // Add the new thread to the list
                threads.Add(newThread);
            }
            // Return the list as an array
            return threads.ToArray();
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
