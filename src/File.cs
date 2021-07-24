using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ChanSharp
{
    public class File
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private HttpClient   RequestsClient   { get; set; }
        private JObject      Data             { get; set; }
        private UrlGenerator UrlGenerator     { get; set; }

        public  Board        Board            { get; set; }
        public  Thread       Thread           { get; set; }
        public  Post         Post             { get; set; }

        public byte[]        FileMD5          { get => FileMD5_get();          }
        public string        FileMD5Hex       { get => FileMD5Hex_get();       }
        public string        FileName         { get => FileName_get();         }
        public string        FileNameOriginal { get => FileNameOriginal_get(); }
        public string        FileURL          { get => FileURL_get();          }
        public string        FileExtension    { get => FileExtension_get();    }
        public int           FileSize         { get => FileSize_get();         }
        public int           FileWidth        { get => FileWidth_get();        }
        public int           FileHeight       { get => FileHeight_get();       }
        public bool          FileDeleted      { get => FileDeleted_get();      }
        public int           ThumbnailWidth   { get => ThumbnailWidth_get();   }
        public int           ThumbnailHeight  { get => ThumbnailHeight_get();  }
        public string        ThumbnailFName   { get => ThumbnailFName_get();   }
        public string        ThumbnailURL     { get => ThumbnailURL_get();     }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public File(Post post, JObject data)
        {
            this.Post   = post;
            this.Thread = post.Thread;
            this.Board  = post.Thread.Board;

            this.RequestsClient = new HttpClient();
            this.Data   = data;
            this.UrlGenerator = new UrlGenerator(Board.Name, Board.Https);
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return string.Format("<File {0} from Post /{1}/{2}#{3}>",
                this.FileName,
                this.Board.Name,
                this.Thread.ID,
                this.Post.ID);
        }



        ////////////////////////////
        ///   Instance methods   ///
        ////////////////////////////

        public HttpResponseMessage FileRequest()
        {
            HttpClient requestsClient = new HttpClient();
            return this.RequestsClient.GetAsync(FileURL).Result; 
        }


        public HttpResponseMessage ThumbnailRequest()
        {
            return this.RequestsClient.GetAsync(ThumbnailURL).Result;
        }



        /////////////////////////////////
        ///   Property get; methods   ///
        /////////////////////////////////

        private byte[] FileMD5_get()
        {
            string md5Base64String = Data.Value<string>("MD5"); ;
            return Util.Base64Decode(md5Base64String);
        }


        private string FileMD5Hex_get()
        {
            return BitConverter.ToString(FileMD5);
        }


        private string FileName_get()
        {
            return $"{ Data["tim"] }{ Data["ext"] }";
        }


        private string FileNameOriginal_get()
        {
            return $"{ Data["filename"] }{ Data["ext"] }";
        }


        private string FileURL_get()
        {
            return UrlGenerator.FileUrls(Data.Value<string>("tim"), Data.Value<string>("ext"));
        }


        private string FileExtension_get()
        {
            return Data.Value<string>("ext");
        }


        private int FileSize_get()
        {
            return Data.Value<int>("fsize");
        }


        private int FileWidth_get()
        {
            return Data.Value<int>("h");
        }


        private int FileHeight_get()
        {
            return Data.Value<int>("w");
        }


        private bool FileDeleted_get()
        {
            return Data.Value<int>("filedeleted") == 1;
        }


        private int ThumbnailWidth_get()
        {
            return Data.Value<int>("tn_w");
        }


        private int ThumbnailHeight_get()
        {
            return Data.Value<int>("tn_h");
        }


        private string ThumbnailFName_get()
        {
            return $"{ Data["tim"] }s.jpg";
        }


        private string ThumbnailURL_get()
        {
            return UrlGenerator.ThumbUrls( Data.Value<string>("tim") );
        }
    }
}
