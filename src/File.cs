using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ChanSharp
{
	public class ChanSharpFile
	{
		//////////////////////
		///   Properties   ///
		//////////////////////
		
		private HttpClient     RequestsClient   { get; }
		private JObject        Data             { get; }
		private UrlGenerator   UrlGenerator     { get; }

		public ChanSharpBoard  Board                 { get; set; }
		public ChanSharpThread Thread                { get; set; }
		public ChanSharpPost   Post                  { get; set; }

		public string          FileName              { get => FileName_get();              }
		public string          FileNameFull          { get => FileNameFull_get();          }
		public string          FileNameOriginal      { get => FileNameOriginal_get();      }
		public string          FileNameOriginalFull  { get => FileNameOriginalFull_get();  }
		public string          FileExtension         { get => FileExtension_get();         }
		public string          FileUrl               { get => FileUrl_get();               }
		public int             FileSize              { get => FileSize_get();              }
		public int             FileWidth             { get => FileWidth_get();             }
		public int             FileHeight            { get => FileHeight_get();            }
		public byte[]          FileContent           { get => FileContent_get();           }
		public bool            FileDeleted           { get => FileDeleted_get();           }
		public byte[]          FileMD5               { get => FileMD5_get();               }
		public string          FileMD5Hex            { get => FileMD5Hex_get();            }
		public string          ThumbnailFileName     { get => ThumbnailFileName_get();     }
		public string          ThumbnailFileNameFull { get => ThumbnailFileNameFull_get(); }
		public string          ThumbnailUrl          { get => ThumbnailUrl_get();          }
		public int             ThumbnailWidth        { get => ThumbnailWidth_get();        }
		public int             ThumbnailHeight       { get => ThumbnailHeight_get();       }
		public byte[]          ThumbnailContent      { get => ThumbnailContent_get();      }



		////////////////////////
		///   Constructors   ///
		////////////////////////

		public ChanSharpFile(ChanSharpPost post)
		{
			this.Post   = post;
			this.Thread = post.Thread;
			this.Board  = post.Thread.Board;

			this.RequestsClient = new HttpClient();
			this.Data           = post.Data;
			this.UrlGenerator   = new UrlGenerator(Board.Name, Board.Https);
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
			return this.RequestsClient.GetAsync(FileUrl).Result; 
		}


		public HttpResponseMessage ThumbnailRequest()
		{
			return this.RequestsClient.GetAsync(ThumbnailUrl).Result;
		}



		/////////////////////////////////
		///   Property get; methods   ///
		/////////////////////////////////

		private string FileName_get()
		{
			return $"{ this.Data["tim"] }";
		}


		private string FileNameFull_get()
		{
			return $"{ this.Data["tim"] }{ this.Data["ext"] }";
		}


		private string FileNameOriginal_get()
		{
			return $"{ this.Data["filename"] }";
		}


		private string FileNameOriginalFull_get()
		{
			return $"{ this.Data["filename"] }{ this.Data["ext"] }";
		}


		private string FileExtension_get()
		{
			return this.Data.Value<string>("ext");
		}


		private string FileUrl_get()
		{
			return this.UrlGenerator.FileUrls(this.Data.Value<string>("tim"), this.Data.Value<string>("ext"));
		}


		private int FileSize_get()
		{
			return this.Data.Value<int>("fsize");
		}


		private int FileWidth_get()
		{
			return this.Data.Value<int>("h");
		}


		private int FileHeight_get()
		{
			return this.Data.Value<int>("w");
		}


		private byte[] FileContent_get()
		{
			// Return null if data couldn't be obtained for whatever reason
			HttpResponseMessage resp = this.RequestsClient.GetAsync(this.FileUrl).Result;
			return resp.IsSuccessStatusCode ? resp.Content.ReadAsByteArrayAsync().Result : null;
		}


		private bool FileDeleted_get()
		{
			return this.Data.Value<int>("filedeleted") == 1;
		}


		private byte[] FileMD5_get()
		{
			string md5Base64String = Data.Value<string>("MD5");
			return Util.Base64Decode(md5Base64String);
		}


		private string FileMD5Hex_get()
		{
			return BitConverter.ToString(this.FileMD5);
		}


		private string ThumbnailFileName_get()
		{
			return $"{ this.Data["tim"] }";
		}


		private string ThumbnailFileNameFull_get()
		{
			return $"{ this.Data["tim"] }s.jpg";
		}


		private string ThumbnailUrl_get()
		{
			return this.UrlGenerator.ThumbUrls(this.Data.Value<string>("tim"));
		}


		private int ThumbnailWidth_get()
		{
			return this.Data.Value<int>("tn_w");
		}


		private int ThumbnailHeight_get()
		{
			return this.Data.Value<int>("tn_h");
		}		
		

		private byte[] ThumbnailContent_get()
		{
			// Return null if data couldn't be obtained for whatever reason
			HttpResponseMessage resp = this.RequestsClient.GetAsync(this.ThumbnailUrl).Result;
			return resp.IsSuccessStatusCode ? resp.Content.ReadAsByteArrayAsync().Result : null;
		}
	}
}
