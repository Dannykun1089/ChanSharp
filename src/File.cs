using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChanSharp
{
    using Extensions;

    public class File
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private JObject Data { get; }
        private HttpClient RequestsClient { get; }
        private UrlGenerator UrlGenerator { get; }

        public Board Board { get; }
        public Thread Thread { get; }
        public Post Post { get; }

        public string FileName { get => FileName_get(); }
        public string FileNameFull { get => FileNameFull_get(); }
        public string FileNameOriginal { get => FileNameOriginal_get(); }
        public string FileNameOriginalFull { get => FileNameOriginalFull_get(); }
        public string Extension { get => Extension_get(); }
        public string Url { get => Url_get(); }
        public int Size { get => Size_get(); }
        public int Width { get => FileWidth_get(); }
        public int Height { get => FileHeight_get(); }
        public byte[] Content { get => FileContent_get(); }
        public bool Deleted { get => FileDeleted_get(); }
        public byte[] MD5 { get => FileMD5_get(); }
        public string MD5Hex { get => FileMD5Hex_get(); }
        public string ThumbnailFileName { get => ThumbnailFileName_get(); }
        public string ThumbnailFileNameFull { get => ThumbnailFileNameFull_get(); }
        public string ThumbnailUrl { get => ThumbnailUrl_get(); }
        public int ThumbnailWidth { get => ThumbnailWidth_get(); }
        public int ThumbnailHeight { get => ThumbnailHeight_get(); }
        public byte[] ThumbnailContent { get => ThumbnailContent_get(); }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public File(Post post)
        {
            Post = post;
            Thread = post.Thread;
            Board = post.Board;

            Data = post.Data;
            RequestsClient = post.Board.RequestsClient;
            UrlGenerator = post.Board.UrlGenerator;
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return string.Format("<File /{0}/{1}#{2}, name: {3}>",
                                 Board.Name,
                                 Thread.ID,
                                 Post.ID,
                                 FileNameOriginalFull);
        }



        ////////////////////////////
        ///   Instance methods   ///
        ////////////////////////////

        public HttpResponseMessage FileRequest()
        {
            return RequestsClient.Get(Url);
        }


        public HttpResponseMessage ThumbnailRequest()
        {
            return RequestsClient.Get(ThumbnailUrl);
        }


        public Task<HttpResponseMessage> FileRequestAsync()
        {
            return RequestsClient.GetAsync(Url);
        }


        public Task<HttpResponseMessage> ThumbnailRequestAsync()
        {
            return RequestsClient.GetAsync(ThumbnailUrl);
        }



        /////////////////////////////////
        ///   Property get; methods   ///
        /////////////////////////////////

        private string FileName_get()
        {
            return $"{ Data["tim"] }";
        }


        private string FileNameFull_get()
        {
            return $"{ Data["tim"] }{ Data["ext"] }";
        }


        private string FileNameOriginal_get()
        {
            return $"{ Data["filename"] }";
        }


        private string FileNameOriginalFull_get()
        {
            return $"{ Data["filename"] }{ Data["ext"] }";
        }


        private string Extension_get()
        {
            return Data.Value<string>("ext");
        }


        private string Url_get()
        {
            return UrlGenerator.FileUrls(Data.Value<string>("tim"), Data.Value<string>("ext"));
        }


        private int Size_get()
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


        private byte[] FileContent_get()
        {
            // Return null if data couldn't be obtained for whatever reason
            HttpResponseMessage resp = RequestsClient.Get(Url);
            return resp.IsSuccessStatusCode ? resp.Content.ReadAsByteArray() : null;
        }


        private bool FileDeleted_get()
        {
            return Data.Value<int>("filedeleted") == 1;
        }


        private byte[] FileMD5_get()
        {
            string md5Base64String = Data.Value<string>("MD5");
            return Convert.FromBase64String(md5Base64String);
        }


        private string FileMD5Hex_get()
        {
            return BitConverter.ToString(MD5);
        }


        private string ThumbnailFileName_get()
        {
            return $"{ Data["tim"] }";
        }


        private string ThumbnailFileNameFull_get()
        {
            return $"{ Data["tim"] }s.jpg";
        }


        private string ThumbnailUrl_get()
        {
            return UrlGenerator.ThumbUrls(Data.Value<string>("tim"));
        }


        private int ThumbnailWidth_get()
        {
            return Data.Value<int>("tn_w");
        }


        private int ThumbnailHeight_get()
        {
            return Data.Value<int>("tn_h");
        }


        private byte[] ThumbnailContent_get()
        {
            // Return null if data couldn't be obtained for whatever reason
            HttpResponseMessage resp = RequestsClient.Get(ThumbnailUrl);
            return resp.IsSuccessStatusCode ? resp.Content.ReadAsByteArray() : null;
        }
    }
}
