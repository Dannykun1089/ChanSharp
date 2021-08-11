using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;

namespace ChanSharp
{
    public class File
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private HttpClient RequestsClient { get; }
        private JObject Data { get; }
        private UrlGenerator UrlGenerator { get; }

        public Board Board { get; set; }
        public Thread Thread { get; set; }
        public Post Post { get; set; }

        public string FileName { get => FileName_get(); }
        public string FileNameFull { get => FileNameFull_get(); }
        public string FileNameOriginal { get => FileNameOriginal_get(); }
        public string FileNameOriginalFull { get => FileNameOriginalFull_get(); }
        public string FileExtension { get => FileExtension_get(); }
        public string FileUrl { get => FileUrl_get(); }
        public int FileSize { get => FileSize_get(); }
        public int FileWidth { get => FileWidth_get(); }
        public int FileHeight { get => FileHeight_get(); }
        public byte[] FileContent { get => FileContent_get(); }
        public bool FileDeleted { get => FileDeleted_get(); }
        public byte[] FileMD5 { get => FileMD5_get(); }
        public string FileMD5Hex { get => FileMD5Hex_get(); }
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
            Board = post.Thread.Board;

            RequestsClient = new HttpClient();
            Data = post.Data;
            UrlGenerator = new UrlGenerator(Board.Name, Board.Https);
        }



        /////////////////////
        ///   Overrides   ///
        /////////////////////

        public override string ToString()
        {
            return string.Format("<File {0} from Post /{1}/{2}#{3}>",
                FileName,
                Board.Name,
                Thread.ID,
                Post.ID);
        }



        ////////////////////////////
        ///   Instance methods   ///
        ////////////////////////////

        public HttpResponseMessage FileRequest()
        {
            return RequestsClient.GetAsync(FileUrl).Result;
        }


        public HttpResponseMessage ThumbnailRequest()
        {
            return RequestsClient.GetAsync(ThumbnailUrl).Result;
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


        private string FileExtension_get()
        {
            return Data.Value<string>("ext");
        }


        private string FileUrl_get()
        {
            return UrlGenerator.FileUrls(Data.Value<string>("tim"), Data.Value<string>("ext"));
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


        private byte[] FileContent_get()
        {
            // Return null if data couldn't be obtained for whatever reason
            HttpResponseMessage resp = RequestsClient.GetAsync(FileUrl).Result;
            return resp.IsSuccessStatusCode ? resp.Content.ReadAsByteArrayAsync().Result : null;
        }


        private bool FileDeleted_get()
        {
            return Data.Value<int>("filedeleted") == 1;
        }


        private byte[] FileMD5_get()
        {
            string md5Base64String = Data.Value<string>("MD5");
            return Util.Base64Decode(md5Base64String);
        }


        private string FileMD5Hex_get()
        {
            return BitConverter.ToString(FileMD5);
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
            HttpResponseMessage resp = RequestsClient.GetAsync(ThumbnailUrl).Result;
            return resp.IsSuccessStatusCode ? resp.Content.ReadAsByteArrayAsync().Result : null;
        }
    }
}
