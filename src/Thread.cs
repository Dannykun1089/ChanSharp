using System;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ChanSharp
{
	public class ChanSharpThread
	{
		//////////////////////
		///   Properties   ///
		//////////////////////

		private HttpClient            RequestsClient { get; set; }
		private string                ApiUrl         { get; set; }
		private ChanSharpUrlGenerator UrlGenerator   { get; set; }

		public ChanSharpBoard  Board          { get; set; }

		public int             ID             { get; set; }
		public bool            Closed         { get => Closed_get();         }
		public bool            Sticky         { get => Sticky_get();         }
		public bool            Archived       { get => Archived_get();       }
		public bool            BumpLimit      { get => BumpLimit_get();      }
		public bool            ImageLimit     { get => ImageLimit_get();     }
		public bool            Is404          { get; set; }
		public int             ReplyCount     { get; set; }
		public int             ImageCount     { get; set; }
		public int             OmittedPosts   { get; set; }
		public int             OmittedImages  { get; set; }
		public ChanSharpPost   Topic          { get; set; }
		public ChanSharpPost[] Replies        { get; set; }
		public ChanSharpPost[] Posts          { get => Posts_get();          }
		public ChanSharpPost[] AllPosts       { get => AllPosts_get();       }
		public ChanSharpFile[] Files          { get => Files_get();          }
		public string[]        ThumbnailUrls  { get => ThumbnailUrls_get();  }
		public string          Url            { get => Url_get();            }
		public string          SemanticSlug   { get => SemanticSlug_get();   }
		public string          SemanticUrl    { get => SemanticUrl_get();    }
		public int             CustomSpoilers { get => CustomSpoilers_get(); }
		public bool            WantUpdate     { get; set; }
		public int             LastReplyID    { get; set; }
		public DateTime        LastModified   { get; set; }



		////////////////////////
		///   Constructors   ///
		////////////////////////

		public ChanSharpThread(ChanSharpBoard board, int threadID)
		{
			this.Board          = board;

			this.ID             = threadID;
			this.Is404          = false;
			this.OmittedPosts   = 0;
			this.OmittedImages  = 0;
			this.Topic          = null;
			this.Replies        = null;
			this.LastReplyID    = 0;
			this.WantUpdate     = false;
			this.LastModified   = DateTime.MinValue;

			this.RequestsClient = new HttpClient();
			this.UrlGenerator   = new ChanSharpUrlGenerator(board.Name, board.Https);
			this.ApiUrl         = UrlGenerator.ThreadAPIUrls(ID);
		}


		public ChanSharpThread(string boardName, int threadID)
		{
			this.Board         = new ChanSharpBoard(boardName);

			this.ID            = threadID;
			this.Is404         = false;
			this.OmittedPosts  = 0;
			this.OmittedImages = 0;
			this.Topic         = null;
			this.Replies       = null;
			this.WantUpdate    = false;
			this.LastReplyID   = 0;
			this.LastModified  = DateTime.MinValue;

			this.RequestsClient = new HttpClient();
			this.UrlGenerator = new ChanSharpUrlGenerator(boardName, this.Board.Https);
			this.ApiUrl        = UrlGenerator.ThreadAPIUrls(ID);
		}



		////////////////////////
		///   Type methods   ///
		////////////////////////

		public static ChanSharpThread FromRequest(string boardName, HttpResponseMessage resp, int threadID = 0)
		{
			if (resp.StatusCode == HttpStatusCode.NotFound) { return null; }
			resp.EnsureSuccessStatusCode();

			JObject        jsonContent  = JObject.Parse( resp.Content.ReadAsStringAsync().Result );
			string         lastModified = resp.Content.Headers.GetValues("Last-Modified").FirstOrDefault();
			ChanSharpBoard board        = new ChanSharpBoard(boardName);

			return FromJson(jsonContent, board, threadID, lastModified);
		}


		public static ChanSharpThread FromJson(JObject threadJson, ChanSharpBoard board, int threadID = 0, string lastModified = null)
		{
			ChanSharpThread retVal = new ChanSharpThread(board, threadID);

			JToken[] postsJson     = threadJson["posts"].ToObject<JToken[]>();
			JObject  firstPostJson = JObject.FromObject(postsJson[0]);
			JObject[] repliesJson  = Util.JTokenArrayToJObjectArray(
																new ArraySegment<JToken>(postsJson, 1, postsJson.Length - 1).Array
																);

			List<ChanSharpPost> replies = new List<ChanSharpPost>();
			foreach (JObject reply in repliesJson)
			{
				replies.Add( new ChanSharpPost(retVal, reply) );
			}

			retVal.ID            = firstPostJson["no"] == null ? threadID : firstPostJson.Value<int>("no");
			retVal.Topic         = new ChanSharpPost(retVal, firstPostJson);
			retVal.Replies       = replies.ToArray();
			retVal.ReplyCount    = firstPostJson.Value<int>("replies");
			retVal.ImageCount    = firstPostJson.Value<int>("images");
			retVal.OmittedImages = firstPostJson["omitted_images"] == null ? 0 : firstPostJson.Value<int>("omitted_images");
			retVal.OmittedPosts  = firstPostJson["omitted_posts"] == null  ? 0 : firstPostJson.Value<int>("omitted_posts");
			retVal.LastModified  = DateTime.Parse(lastModified);

			// If we couldnt get the threadID, set WantUpdate to true, else set the LastReplyID
			if (threadID == 0)
			{
				retVal.WantUpdate = true;
			}
			else
			{
				retVal.LastReplyID = retVal.Replies == null ? retVal.Topic.ID : retVal.Replies.Last().ID;
			}

			return retVal;
		}


		//Overload for instances where threadJson is a JToken
		public static ChanSharpThread FromJson(JToken threadJson, ChanSharpBoard board, int threadID = 0, string lastModified = null)
		{
			ChanSharpThread retVal = new ChanSharpThread(board, threadID);

			JToken[] postsJson = threadJson["posts"].ToObject<JToken[]>();
			JObject firstPostJson = JObject.FromObject(postsJson[0]);
			JObject[] repliesJson = Util.JTokenArrayToJObjectArray(
																new ArraySegment<JToken>(postsJson, 1, postsJson.Length - 1).Array
																);

			List<ChanSharpPost> replies = new List<ChanSharpPost>();
			foreach (JObject reply in repliesJson)
			{
				replies.Add(new ChanSharpPost(retVal, reply));
			}

			retVal.ID            = firstPostJson["no"] == null ? threadID : firstPostJson.Value<int>("no");
			retVal.Topic         = new ChanSharpPost(retVal, firstPostJson);
			retVal.Replies       = replies.ToArray();
			retVal.ReplyCount    = firstPostJson.Value<int>("replies");
			retVal.ImageCount    = firstPostJson.Value<int>("images");
			retVal.OmittedImages = firstPostJson["omitted_images"] == null ? 0 : firstPostJson.Value<int>("omitted_images");
			retVal.OmittedPosts  = firstPostJson["omitted_posts"] == null ? 0 : firstPostJson.Value<int>("omitted_posts");
			retVal.LastModified  = lastModified == null ? DateTime.MinValue : DateTime.Parse(lastModified);

			// If we couldnt get the threadID, set WantUpdate to true, else set the LastReplyID
			if (threadID == 0)
			{
				retVal.WantUpdate = true;
			}
			else
			{
				retVal.LastReplyID = retVal.Replies == null ? retVal.Topic.ID : retVal.Replies.Last().ID;
			}

			return retVal;
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
				string httpHeaderPattern = "ddd, dd, MMM, yyyy HH:mm:ss 'GMT'";
				RequestsClient.DefaultRequestHeaders.Add("If-Modified-Since", LastModified.ToString(httpHeaderPattern));
			}

			// Random connection errors, return 0 and try again later
			HttpResponseMessage resp;
			try
			{
				resp = RequestsClient.GetAsync( UrlGenerator.ThreadAPIUrls(ID) ).Result;
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
					this.Is404 = true;

					// Remove post from cache because it's gone
					this.Board.ThreadCache.Remove(ID);
					return 0;

				// 200 - OK: Thread is alive
				case HttpStatusCode.OK:
					// If we somehow 404'ed, put ourself back in the cache
					if (Is404)
					{
						this.Is404 = false;
						this.Board.ThreadCache.Add(ID, this);
					}

					int originalPostCount = Replies.Length;

					JToken[] posts = JObject.Parse( resp.Content.ReadAsStringAsync().Result ).Value<JToken[]>("posts");

					this.Topic         = new ChanSharpPost(this, posts[0]);
					this.WantUpdate    = false;
					this.OmittedImages = 0;
					this.OmittedPosts  = 0;
					this.LastModified  = DateTime.Parse( resp.Headers.GetValues("Last-Modified").First() );

					if (this.LastReplyID > 0 && !force)
					{
						// Add the new replies to a list
						List<ChanSharpPost> newReplies = new List<ChanSharpPost> { };
						foreach (JToken post in posts)
						{
							if (post.Value<int>("no") > LastReplyID) { newReplies.Add( new ChanSharpPost(this, post) );  }
						}

						// Insert the old replies before the new replies
						newReplies.InsertRange(0, this.Replies);

						this.Replies = newReplies.ToArray();
					}
					else
					{
						// Add all the posts to a list
						List<ChanSharpPost> newReplies = new List<ChanSharpPost>();
						foreach (JToken post in posts)
						{
							newReplies.Add(new ChanSharpPost(this, post));
						}

						// Remove the OP and set the Replies property
						newReplies.RemoveAt(0);

						this.Replies = newReplies.ToArray();
					}


					this.LastReplyID = this.Replies.Last().ID;

					return Replies.Length - originalPostCount;

				// Incase of anything else, raise for status and return 0
				default:
					resp.EnsureSuccessStatusCode();
					return 0;
			}
		}


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


		private ChanSharpPost[] Posts_get()
		{
			List<ChanSharpPost> retVal = new List<ChanSharpPost>();
			retVal.Add(Topic);
			retVal.AddRange(Replies);

			return retVal.ToArray();
		}


		private ChanSharpPost[] AllPosts_get()
		{
			Expand();
			return Posts;
		}


		private ChanSharpFile[] Files_get()
		{
			List<ChanSharpFile> retVal = new List<ChanSharpFile>();

			foreach (ChanSharpPost post in this.Posts)
			{
				if (post.HasFile) { retVal.Add(post.File); }
			}

			return retVal.ToArray();
		}


		private string[] ThumbnailUrls_get()
		{
			List<string> retVal = new List<string>();

			foreach (ChanSharpFile file in this.Files)
			{
				retVal.Add(file.ThumbnailURL);
			}

			return retVal.ToArray();
		}


		private string Url_get()
		{
			return UrlGenerator.ThreadAPIUrls(ID);
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
