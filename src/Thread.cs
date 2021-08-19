using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace ChanSharp
{
    using Extensions;

    public class Thread
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private HttpClient RequestsClient { get; set; }
        private UrlGenerator UrlGenerator { get; set; }

        public Board Board { get; set; }

        public int ID { get; set; }
        public bool Closed { get => Closed_get(); }
        public bool Sticky { get => Sticky_get(); }
        public bool Archived { get => Archived_get(); }
        public bool BumpLimit { get => BumpLimit_get(); }
        public bool ImageLimit { get => ImageLimit_get(); }
        public bool Is404 { get; set; }
        public int ReplyCount { get; set; }
        public int ImageCount { get; set; }
        public int OmittedPosts { get; set; }
        public int OmittedImages { get; set; }
        public Post Topic { get; set; }
        public Post[] Replies { get; set; }
        public Post[] Posts { get => Posts_get(); }
        public Post[] AllPosts { get => AllPosts_get(); }
        public File[] Files { get => Files_get(); }
        public string[] ThumbnailUrls { get => ThumbnailUrls_get(); }
        public string Url { get => Url_get(); }
        public string SemanticSlug { get => SemanticSlug_get(); }
        public string SemanticUrl { get => SemanticUrl_get(); }
        public int CustomSpoilers { get => CustomSpoilers_get(); }
        public bool WantUpdate { get; set; }
        public int LastReplyID { get; set; }
        public DateTimeOffset LastModified { get; set; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public Thread(Board board, int threadID)
        {
            Board = board;

            ID = threadID;
            Is404 = false;
            OmittedPosts = 0;
            OmittedImages = 0;
            Topic = null;
            Replies = null;
            LastReplyID = 0;
            WantUpdate = false;
            LastModified = DateTimeOffset.FromUnixTimeSeconds(0);

            RequestsClient = new HttpClient();
            UrlGenerator = new UrlGenerator(board.Name, board.Https);
        }



        ////////////////////////
        ///   Type methods   ///
        ////////////////////////

        // These are the methods used to properly fill out the object

        // From a request to http(s)://a.4cdn.org/{board}/thread/{threadId}.json
        public static Thread FromRequest(string boardName, HttpResponseMessage resp, int threadId = 0)
        {
            // Ensure the request is ok
            if (resp.StatusCode == HttpStatusCode.NotFound) { return null; }
            resp.EnsureSuccessStatusCode();

            // Parse the response content into a JObject and get the Last-Modified value from the content headers
            JObject jsonContent = JObject.Parse(resp.Content.ReadAsString());
            DateTimeOffset lastModified = resp.Content.Headers.LastModified.Value;

            return FromJson(jsonContent, new Board(boardName), threadId, lastModified);
        }


        public static Thread FromJson(JObject threadJson, Board board, int threadID = 0, DateTimeOffset? lastModified = null)
        {
            Thread newThread = new Thread(board, threadID);

            JToken[] postsJson = threadJson["posts"].ToObject<JToken[]>();
            JToken firstPostJson = postsJson[0];
            JToken[] repliesJson;

            // If the postsJson length greater than 1, get rid of the first element of postsJson to make repliesJson,
            // Itterate over each reply Json in repliesJson and add a new Post object to replies
            List<Post> replies = new List<Post>();
            if (postsJson.Length > 1)
            {
                repliesJson = Util.SliceArray(postsJson, 1);
                foreach (JObject reply in repliesJson)
                {
                    replies.Add(new Post(newThread, reply));
                }
            }
            // Else, only the OP is present, and there are no replies
            else
            {
                replies = null;
            }

            // Fill in the thread information from the OP's post Json
            newThread.ID = firstPostJson.Value<int?>("no") ?? threadID;
            newThread.Topic = new Post(newThread, firstPostJson);
            newThread.Replies = replies?.ToArray();
            newThread.ReplyCount = firstPostJson.Value<int>("replies");
            newThread.ImageCount = firstPostJson.Value<int>("images");
            newThread.OmittedImages = firstPostJson.Value<int?>("omitted_images") ?? 0;
            newThread.OmittedPosts = firstPostJson.Value<int?>("omitted_posts") ?? 0;
            newThread.LastModified = lastModified ?? DateTimeOffset.FromUnixTimeSeconds(0);

            // If we couldnt get the threadID, set WantUpdate to true, else set the LastReplyID
            if (threadID == 0)
            {
                newThread.WantUpdate = true;
            }
            else
            {
                newThread.LastReplyID = newThread.Replies == null ? newThread.Topic.ID : newThread.Replies.Last().ID;
            }
            return newThread;
        }


        //Overload for instances where threadJson is a JToken
        public static Thread FromJson(JToken threadJson, Board board, int threadID = 0, DateTimeOffset? lastModified = null)
        {
            Thread newThread = new Thread(board, threadID);

            JToken[] postsJson = threadJson["posts"].ToObject<JToken[]>();
            JToken firstPostJson = postsJson[0];
            JToken[] repliesJson;

            // If the postsJson length greater than 1, get rid of the first element of postsJson to make repliesJson,
            // Itterate over each reply Json in repliesJson and add a new Post object to replies
            List<Post> replies = new List<Post>();
            if (postsJson.Length > 1)
            {
                repliesJson = Util.SliceArray(postsJson, 1);
                foreach (JObject reply in repliesJson)
                {
                    replies.Add(new Post(newThread, reply));
                }
            }
            // Else, only the OP is present, and there are no replies
            else
            {
                replies = null;
            }

            // Fill in the thread information from the OP's post Json
            newThread.ID = firstPostJson.Value<int?>("no") ?? threadID;
            newThread.Topic = new Post(newThread, firstPostJson);
            newThread.Replies = replies?.ToArray();
            newThread.ReplyCount = firstPostJson.Value<int>("replies");
            newThread.ImageCount = firstPostJson.Value<int>("images");
            newThread.OmittedImages = firstPostJson.Value<int?>("omitted_images") ?? 0;
            newThread.OmittedPosts = firstPostJson.Value<int?>("omitted_posts") ?? 0;
            newThread.LastModified = lastModified ?? DateTimeOffset.FromUnixTimeSeconds(0);

            // If we couldnt get the threadID, set WantUpdate to true, else set the LastReplyID
            if (threadID == 0)
            {
                newThread.WantUpdate = true;
            }
            else
            {
                newThread.LastReplyID = newThread.Replies == null ? newThread.Topic.ID : newThread.Replies.Last().ID;
            }
            return newThread;
        }


        ///////////////////////////////////
        ///   Public Instance Methods   ///
        ///////////////////////////////////

        // Updates this thread
        public int Update(bool force = false)
        {
            if (Is404 && !force) { return 0; }

            if (LastModified != null)
            {
                RequestsClient.DefaultRequestHeaders.IfModifiedSince = LastModified;
            }

            // Random connection errors, return 0 and try again later
            HttpResponseMessage resp;
            try
            {
                resp = RequestsClient.GetAsync(UrlGenerator.ThreadApiUrl(ID)).Result;
            }
            catch
            {
                return 0;
            }

            switch (resp.StatusCode)
            {
                // 304 - Not Modified: No new posts
                case HttpStatusCode.NotModified:
                    return 0;

                // 404 - Not Found: Thread died
                case HttpStatusCode.NotFound:
                    Is404 = true;

                    // Remove post from cache because it's gone
                    Board.ThreadCache.Remove(ID);
                    return 0;

                // 200 - OK: Thread is alive
                case HttpStatusCode.OK:
                    // If we somehow 404'ed, put ourself back in the cache
                    if (Is404)
                    {
                        Is404 = false;
                        Board.ThreadCache.Add(ID, this);
                    }

                    int originalPostCount = Replies.Length;

                    JToken[] posts = JObject.Parse(resp.Content.ReadAsStringAsync().Result).Value<JArray>("posts").ToObject<JToken[]>();

                    Topic = new Post(this, posts[0]);
                    WantUpdate = false;
                    OmittedImages = 0;
                    OmittedPosts = 0;
                    LastModified = resp.Content.Headers.LastModified.Value;

                    if (LastReplyID > 0 && !force)
                    {
                        // Add the new replies to a list
                        List<Post> newReplies = new List<Post> { };
                        foreach (JToken post in posts)
                        {
                            if (post.Value<int>("no") > LastReplyID) { newReplies.Add(new Post(this, post)); }
                        }

                        // Insert the old replies before the new replies
                        newReplies.InsertRange(0, Replies);

                        Replies = newReplies.ToArray();
                    }
                    else
                    {
                        // Add all the posts to a list
                        List<Post> newReplies = new List<Post>();
                        foreach (JToken post in posts)
                        {
                            newReplies.Add(new Post(this, post));
                        }

                        // Remove the OP and set the Replies property
                        newReplies.RemoveAt(0);

                        Replies = newReplies.ToArray();
                    }


                    LastReplyID = Replies.Last().ID;

                    return Replies.Length - originalPostCount;

                // Incase of anything else, raise for status and return 0
                default:
                    resp.EnsureSuccessStatusCode();
                    return 0;
            }
        }


        // Updates the thread to include all posts
        public void Expand()
        {
            if (OmittedPosts > 0) { Update(); }
        }


        /////////////////////////////////
        ///   Property get; Methods   ///
        /////////////////////////////////   

        private bool Closed_get()
        {
            return Topic.Data.Value<int>("closed") == 1;
        }


        private bool Sticky_get()
        {
            return Topic.Data.Value<int>("sticky") == 1;
        }


        private bool Archived_get()
        {
            return Topic.Data.Value<int>("archived") == 1;
        }


        private bool BumpLimit_get()
        {
            return Topic.Data.Value<int>("bumplimit") == 1;
        }


        private bool ImageLimit_get()
        {
            return Topic.Data.Value<int>("imagelimit") == 1;
        }


        private int CustomSpoilers_get()
        {
            return Topic.Data.Value<int>("custom_spoiler");
        }


        private Post[] Posts_get()
        {
            // If there are no replies, return the topic as a single element array
            if (Replies == null)
            {
                return new Post[] { Topic };
            }
            // Else add the Topic and Replies to a list of posts and return it as an array
            else
            {
                List<Post> retVal = new List<Post>();
                retVal.Add(Topic);
                retVal.AddRange(Replies);
                return retVal.ToArray();
            }
        }


        private Post[] AllPosts_get()
        {
            Expand();
            return Posts;
        }


        private File[] Files_get()
        {
            // If the thread only has the OP, check if it has a file, if it does, return a single element array 
            // Containing that file, if it doesnt, return null
            if (Posts.Length == 1)
            {
                if (Posts[0].HasFile) { return new File[] { Posts[0].File }; }
                return null;
            }

            // Else, the thread has an OP and 1 or more replies, itterate over each of them 
            // And if they have a file, add it to the list
            List<File> retVal = new List<File>();
            foreach (Post post in Posts)
            {
                if (post.HasFile) { retVal.Add(post.File); }
            }

            // If no files could be found, return null, else return the files as an array
            if (retVal.Count == 0) { return null; }
            return retVal.ToArray();
        }


        private string[] ThumbnailUrls_get()
        {
            List<string> retVal = new List<string>();
            foreach (File file in Files)
            {
                retVal.Add(file.ThumbnailUrl);
            }

            return retVal.ToArray();
        }


        private string Url_get()
        {
            return UrlGenerator.ThreadUrl(ID);
        }

        private string SemanticSlug_get()
        {
            return Topic.SemanticSlug;
        }


        private string SemanticUrl_get()
        {
            return $"{Url}/{SemanticSlug}";
        }
    }
}
