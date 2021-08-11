using Newtonsoft.Json.Linq;
using System;

namespace ChanSharp
{
    public class Post
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private UrlGenerator UrlGenerator { get; set; }

        public Board Board { get; set; }
        public Thread Thread { get; set; }

        public int ID { get => ID_get(); }
        public int PosterID { get => PosterID_get(); }
        public string Name { get => Name_get(); }
        public string EMail { get => Email_get(); }
        public string Tripcode { get => Tripcode_get(); }
        public string Subject { get => Subject_get(); }
        public string HTMLComment { get => HTMLComment_get(); }
        public string TextComment { get => TextComment_get(); }
        public string Comment { get => Comment_get(); }
        public bool IsOp { get => IsOp_get(); }
        public bool Spoiler { get => Spoiler_get(); }
        public int Timestamp { get => Timestamp_get(); }
        public DateTime DateTime { get => DateTime_get(); }
        public File File { get => File_get(); }
        public bool HasFile { get => HasFile_get(); }
        public string Url { get => Url_get(); }
        public string SemanticSlug { get => SemanticSlug_get(); }
        public string SemanticUrl { get => SemanticUrl_get(); }

        public JObject Data { get; set; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public Post(Thread thread, JObject data)
        {
            Thread = thread;

            Data = data;
            UrlGenerator = new UrlGenerator(thread.Board.Name, thread.Board.Https);
        }


        public Post(string boardName, int threadID, JObject data)
        {
            Thread = new Thread(boardName, threadID);

            Data = data;
            UrlGenerator = new UrlGenerator(Thread.Board.Name, Thread.Board.Https);
        }


        public Post(Thread thread, JToken data)
        {
            Thread = thread;

            Data = JObject.FromObject(data);
            UrlGenerator = new UrlGenerator(thread.Board.Name, thread.Board.Https);
        }


        public Post(string boardName, int threadID, JToken data)
        {
            Thread = new Thread(boardName, threadID);

            Data = JObject.FromObject(data);
            UrlGenerator = new UrlGenerator(Thread.Board.Name, Thread.Board.Https);
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return String.Format("<Post /{0}/{1}#{2}, has_file: {3}>",
                Thread.Board.Name,
                Thread.ID,
                ID,
                HasFile ? "true" : "false");
        }



        /////////////////////////////////
        ///   Property get; methods   ///
        /////////////////////////////////

        private int ID_get()
        {
            return Data.Value<int>("no");
        }


        private int PosterID_get()
        {
            return Data.Value<int>("id");
        }


        private string Name_get()
        {
            return Data.Value<string>("name");
        }


        private string Email_get()
        {
            return Data.Value<string>("email");
        }


        private string Tripcode_get()
        {
            return Data.Value<string>("trip");
        }


        private string Subject_get()
        {
            return Data.Value<string>("sub");
        }


        private string HTMLComment_get()
        {
            return Data.Value<string>("com");
        }


        //todo: clean comment body util func
        private string TextComment_get()
        {
            return Util.CleanCommentBody(HTMLComment);
        }


        private string Comment_get()
        {
            return HTMLComment.Replace("<wbr>", "");
        }


        private bool IsOp_get()
        {
            return ID == Thread.Topic.ID;
        }


        private bool Spoiler_get()
        {
            return Data.Value<int>("spoiler") == 1;
        }


        private int Timestamp_get()
        {
            return Data.Value<int>("time");
        }


        private DateTime DateTime_get()
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            date.AddSeconds(Timestamp);
            return date;
        }


        private File File_get()
        {
            return HasFile ? new File(this) : null;
        }


        private bool HasFile_get()
        {
            return Data.ContainsKey("filename");
        }


        private string Url_get()
        {
            return $"{ Thread.Url }#p{ ID }";
        }


        private string SemanticSlug_get()
        {
            return Data.Value<string>("semantic_url");
        }


        private string SemanticUrl_get()
        {
            return $"{ Thread.SemanticUrl }#p{ ID }";
        }
    }
}
