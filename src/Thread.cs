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

        private DateTimeOffset? LastModified { get; set; }

        public Board Board { get; }

        public int ID { get; internal set; }
        public bool Closed { get => Closed_get(); }
        public bool Sticky { get => Sticky_get(); }
        public bool Archived { get => Archived_get(); }
        public bool BumpLimit { get => BumpLimit_get(); }
        public bool ImageLimit { get => ImageLimit_get(); }
        public bool Is404 { get; internal set; }
        public int ReplyCount { get; internal set; }
        public int ImageCount { get; internal set; }
        public int OmittedPosts { get; internal set; }
        public int OmittedImages { get; internal set; }
        public Post Topic { get; internal set; }
        public Post[] Replies { get; internal set; }
        public Post[] Posts { get => Posts_get(); }
        public Post[] AllPosts { get => AllPosts_get(); }
        public File[] Files { get => Files_get(); }
        public string[] ThumbnailUrls { get => ThumbnailUrls_get(); }
        public string Url { get => Url_get(); }
        public string SemanticSlug { get => SemanticSlug_get(); }
        public string SemanticUrl { get => SemanticUrl_get(); }
        public int CustomSpoilers { get => CustomSpoilers_get(); }
        public bool WantUpdate { get; internal set; }
        public int LastReplyID { get; internal set; }

        internal HttpClient RequestsClient { get; }
        internal UrlGenerator UrlGenerator { get; }


        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        // Do not use constructor to make Thread objects, use Board.GetThread() and similar methods
        public Thread(Board board, int threadID)
        {
            LastModified = null;

            Board = board;

            ID = threadID;
            Is404 = false;
            ReplyCount = 0;
            ImageCount = 0;
            OmittedPosts = 0;
            OmittedImages = 0;
            Topic = null;
            Replies = null;
            WantUpdate = false;
            LastReplyID = 0;

            RequestsClient = board.RequestsClient;
            UrlGenerator = new(board.Name, board.Https);
        }


        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return string.Format("<Thread /{0}/{1}>",
                                 Board.Name,
                                 ID);
        }



        ////////////////////////
        ///   Type methods   ///
        ////////////////////////

        // From a request to http(s)://a.4cdn.org/{board}/thread/{threadId}.json
        public static Thread FromRequest(Board board, HttpResponseMessage resp, int threadId = 0)
        {
            // Parse the response content into a JObject and get the Last-Modified value from the content headers
            JObject jsonContent = JObject.Parse(resp.Content.ReadAsString());
            DateTimeOffset? lastModified = resp.Content.Headers.LastModified.Value;

            return FromJson(board, jsonContent, threadId, lastModified);
        }

        //TODO, maybe: The last_replies property should be turned into Posts at the end not the start of replies
        public static Thread FromJson(Board board, JObject threadJson, int threadID = 0, DateTimeOffset? lastModified = null)
        {
            Thread newThread = new(board, threadID);

            JObject[] postsJson = threadJson["posts"].ToObject<JObject[]>();
            JObject firstPostJson = postsJson[0];
            JObject[] repliesJson = Util.SliceArray(postsJson, 1, postsJson.Length - 1);

            // Generate the list of replies 
            List<Post> replies = null;
            if (postsJson.Length > 1)
            {
                replies = new();
                foreach (JObject replyJson in repliesJson)
                {
                    replies.Add(new Post(newThread, replyJson));
                }
            }

            // Fill in the thread information from the thread's Json data
            newThread.ID = firstPostJson.Value<int?>("no") ?? threadID;
            newThread.Topic = new Post(newThread, firstPostJson);
            newThread.Replies = replies?.ToArray();
            newThread.ReplyCount = firstPostJson.Value<int>("replies");
            newThread.ImageCount = firstPostJson.Value<int>("images");
            newThread.OmittedImages = firstPostJson.Value<int>("omitted_images");
            newThread.OmittedPosts = firstPostJson.Value<int>("omitted_posts");
            newThread.LastModified = lastModified;

            // If for some reason the thread ID wasnt passed in, set want update to true
            if (threadID == 0)
            {
                newThread.WantUpdate = true;
            }
            else
            {
                newThread.LastReplyID = newThread.Replies?.Last().ID ?? newThread.Topic.ID;
            }

            return newThread;
        }


        ///////////////////////////////////
        ///   Public Instance Methods   ///
        ///////////////////////////////////

        // Updates this thread, fetching new posts
        public int Update(bool force = false)
        {
            if (Is404 && !force) { return 0; }

            // Hit http(s)://a.4cdn.org/{board}/{threadID}.json for the thread's current data
            RequestsClient.DefaultRequestHeaders.IfModifiedSince = LastModified;
            HttpResponseMessage resp = RequestsClient.Get(UrlGenerator.ThreadApiUrl(ID));

            switch (resp.StatusCode)
            {
                // 404 - Not Found: Thread died
                case HttpStatusCode.NotFound:
                    // Set Is404 to true and remove post from cache because it's gone
                    Is404 = true;
                    Board.ThreadCache.Remove(ID);

                    return 0;

                // 304 - Not Modified: No new posts
                case HttpStatusCode.NotModified:
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

                    JArray postsJson = JObject.Parse(resp.Content.ReadAsString()).Value<JArray>("posts");

                    Topic = new Post(this, postsJson[0]);
                    WantUpdate = false;
                    OmittedImages = 0;
                    OmittedPosts = 0;

                    // Update the LastModified value from the response headers
                    LastModified = resp.Content.Headers.LastModified.Value;

                    // Add all the posts to a list
                    List<Post> newReplies = new();
                    foreach (JObject postJson in postsJson)
                    {
                        newReplies.Add(new Post(this, postJson));
                    }

                    // Remove the OP, as it is a seperate property
                    newReplies.RemoveAt(0);
                    Replies = newReplies.ToArray();
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
            // If for whatever reason there is no topic, return null
            if (Topic is null)
            {
                return null;
            }

            // If there are no replies, return the topic as a single element array
            if (Replies is null)
            {
                return new Post[] { Topic };
            }

            // Else, add the Topic and Replies to a list of posts and return it as an array
            List<Post> retVal = new() { Topic };
            retVal.AddRange(Replies);
            return retVal.ToArray();
        }


        private Post[] AllPosts_get()
        {
            Expand();
            return Posts;
        }


        private File[] Files_get()
        {
            //If for whatever reason the thread has no topic, return null
            if (Topic is null)
            {
                return null;
            }

            // If the thread only contains the op, return its' file as a single element array if present
            // Else return null
            if (Replies is null)
            {
                if (Topic.HasFile) { return new File[] { Topic.File }; }
                return null;
            }

            // Else, the thread has an OP and 1 or more replies, itterate over each of them 
            // And if they have a file, add it to the list
            List<File> retVal = new();
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
            List<string> retVal = new();
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
