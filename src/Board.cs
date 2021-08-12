﻿using Newtonsoft.Json.Linq;
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
        internal Dictionary<int, Thread> ThreadCache { get; }



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
            ThreadCache = new Dictionary<int, Thread>();

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

        public static Dictionary<string, Board> GetBoards(string[] boardNames, bool https = true, HttpClient session = null)
        {
            Dictionary<string, Board> boards = new Dictionary<string, Board>();
            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            foreach (string boardName in boardNames)
            {
                boards.Add( boardName, new Board(boardName, https, session) );
            }

            // Return the dictionary
            return boards;
        }


        // System.Collections.Generic.List<string> overload
        public static Dictionary<string, Board> GetBoards(List<string> boardNames, bool https = true, HttpClient session = null)
        {
            Dictionary<string, Board> boards = new Dictionary<string, Board>();
            // Itterate over each board name, add dictionary entry 'boardName': new Board()
            foreach (string boardName in boardNames)
            {
                boards.Add( boardName, new Board(boardName, https, session) );
            }

            // Return the dictionary
            return boards;
        }


        public static Dictionary<string, Board> GetAllBoards(bool https = true, HttpClient session = null)
        {
            // Request a list of all boards from 4Chan
            HttpClient requestsClient = session ?? new HttpClient();
            UrlGenerator urlGenerator = new UrlGenerator(null);
            requestsClient.DefaultRequestHeaders.Add("User-Agent", "ChanSharp");

            HttpResponseMessage resp  = requestsClient.Get( urlGenerator.BoardList() );

            // Parse the response content into a JObject
            string responseContent = resp.Content.ReadAsString();
            JObject boardsJson     = JObject.Parse( responseContent );

            // Itterate over each board Json and add it's name to the list
            List<string> allBoards = new List<string>();
            foreach (JObject boardJson in boardsJson.Value<JArray>("boards"))
            {
                allBoards.Add( boardJson.Value<string>("board") );
            }

            // pass the list of all the boards into the GetBoards method
            return GetBoards(allBoards, https, session);
        }



        ////////////////////////////////////
        ///   Private Instance Methods   ///
        ////////////////////////////////////

        // Hits 'http(s)://a.4cdn.org/boards.json' for boards Json data
        private void FetchBoardsMetadata(UrlGenerator urlGenerator)
        {
            // Return if there is already metadata
            if (MetaData != null) { return; }

            // Request the boards.json api data and ensure success
            HttpResponseMessage resp = RequestsClient.Get( urlGenerator.BoardList() );
            resp.EnsureSuccessStatusCode();

            // Parse the response data, reconstruct it and return it in the ChanSharpBoard.MetaData format
            MetaData = Util.MetaDataFromRequest(resp);

            // Finish up
            resp.Dispose();
        }


        // Get boards data and return the metadata specified as a JToken, null if not present
        private JToken GetMetaData(string key)
        {
            FetchBoardsMetadata(UrlGenerator);
            return MetaData[Name].Value<JToken>(key);
        }


        // Hits the url specified for a Json response, return it as a JToken
        private JToken GetJson(string url)
        {
            // Send request to the url, ensure successfull status code
            HttpResponseMessage resp = RequestsClient.Get( url );
            resp.EnsureSuccessStatusCode();

            // return the Json data as a JToken (JTokens can handle arrays and regular Json)
            string responseContent = resp.Content.ReadAsString();
            return JToken.Parse( responseContent );
        }


        // Takes in the /{board}/catalog.json Json data as a JArray and reconstructs it
        // Into a format more similar to /{board}/threads.json [NOTE: INSERT FORMAT DEFINITION]
        private JArray CatalogToThreads(JArray catalogJson)
        {
            JArray threadsList = new JArray();
            foreach (JToken pageJson in catalogJson)
            {
                foreach (JObject threadJson in pageJson.Value<JArray>("threads"))
                { 
                    JArray posts;
                    if ( threadJson.ContainsKey("last_replies") )
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
            // Request the url and turn the Json response into a JToken
            JToken Json = GetJson(url);

            // If the Url is a catalog url, call the CatalogToThreads() method
            // Else, the url is a page url, obtain it from the 'threads' token
            JArray threadList;
            if (url == UrlGenerator.Catalog())
            {
                threadList = CatalogToThreads( JArray.FromObject(Json) );
            }
            else
            {
                threadList = JArray.FromObject( Json["threads"] );
            }

            // Go over each thread Json object
            List<Thread> threads = new List<Thread>();
            foreach (JObject threadJson in threadList)
            {
                // Get the thread ID and lastModified values
                int id = threadJson["posts"][0].Value<int>("no");
                DateTimeOffset? lastModified = null;
                if (threadJson.ContainsKey("last_modified"))
                {
                    lastModified = DateTimeOffset.FromUnixTimeSeconds( threadJson.Value<long>("last_modified") );
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
    }
}
