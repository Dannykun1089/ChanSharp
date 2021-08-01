using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ChanSharp
{
    public class ChanSharpPost
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private UrlGenerator UrlGenerator { get; set; }

        public ChanSharpBoard  Board        { get; set; }
        public ChanSharpThread Thread       { get; set; }

        public  int            ID           { get => ID_get();           }
        public  int            PosterID     { get => PosterID_get();     }
        public  string         Name         { get => Name_get();         }
        public  string         EMail        { get => Email_get();        }
        public  string         Tripcode     { get => Tripcode_get();     }
        public  string         Subject      { get => Subject_get();      }
        public  string         HTMLComment  { get => HTMLComment_get();  }
        public  string         TextComment  { get => TextComment_get();  }
        public  string         Comment      { get => Comment_get();      }
        public  bool           IsOp         { get => IsOp_get();         }
        public  bool           Spoiler      { get => Spoiler_get();      }
        public  int            Timestamp    { get => Timestamp_get();    }
        public  DateTime       DateTime     { get => DateTime_get();     }
        public  ChanSharpFile  File         { get => File_get();         }
        public  bool           HasFile      { get => HasFile_get();      }
        public  string         Url          { get => Url_get();          }
        public  string         SemanticSlug { get => SemanticSlug_get(); }
        public  string         SemanticUrl  { get => SemanticUrl_get();  }

        public  JObject        Data         { get; set; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public ChanSharpPost(ChanSharpThread thread, JObject data)
        {
            this.Thread = thread;

            this.Data = data;
            this.UrlGenerator = new UrlGenerator(thread.Board.Name, thread.Board.Https);
        }


        public ChanSharpPost(string boardName, int threadID, JObject data)
        {
            this.Thread = new ChanSharpThread(boardName, threadID);

            this.Data = data;
            this.UrlGenerator = new UrlGenerator(this.Thread.Board.Name, this.Thread.Board.Https);
        }


        public ChanSharpPost(ChanSharpThread thread, JToken data)
        {
            this.Thread = thread;

            this.Data = JObject.FromObject(data);
            this.UrlGenerator = new UrlGenerator(thread.Board.Name, thread.Board.Https);
        }


        public ChanSharpPost(string boardName, int threadID, JToken data)
        {
            this.Thread = new ChanSharpThread(boardName, threadID);

            this.Data = JObject.FromObject(data);
            this.UrlGenerator = new UrlGenerator(this.Thread.Board.Name, this.Thread.Board.Https);
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return String.Format("<Post /{0}/{1}#{2}, has_file: {3}>",
                this.Thread.Board.Name,
                this.Thread.ID,
                this.ID,
                this.HasFile ? "true" : "false");
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
            return this.ID == Thread.Topic.ID;
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


        private ChanSharpFile File_get()
        {
            return this.HasFile ? new ChanSharpFile(this) : null; 
        }


        private bool HasFile_get()
        {
            return Data.ContainsKey("filename");
        }


        private string Url_get() 
        {
            return $"{ this.Thread.Url }#p{ this.ID }";
        }


        private string SemanticSlug_get()
        {
            return Data.Value<string>("semantic_url");
        }


        private string SemanticUrl_get()
        {
            return $"{ this.Thread.SemanticUrl }#p{ this.ID }";
        }
    }
}
