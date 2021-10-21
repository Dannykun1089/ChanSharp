using Newtonsoft.Json.Linq;

namespace ChanSharp
{
    internal class UrlGenerator
	{
		//////////////////////
		///   Properties   ///
		//////////////////////

		private string BoardName { get; }
		private string Protocol { get; }
		
		public JObject Urls { get; }



		////////////////////////
		///   Constructors   ///
		////////////////////////

		internal UrlGenerator(string boardName, bool https = true)
		{
			BoardName = boardName;
			Protocol = https ? "https://" : "http://";


			// Fill in the Urls property
			JToken domain = JToken.Parse($@"
										 {{
											 'api':             '{ Protocol }a.4cdn.org',
											 'boards':          '{ Protocol }boards.4chan.org' ,
											 'boards_4channel': '{ Protocol }boards.4channel.org',
											 'file':            '{ Protocol }i.4cdn.org',
											 'thumbs':          '{ Protocol }i.4cdn.org',
											 'static':          '{ Protocol }s.4cdn.org'
										 }}
										 ");

			JToken listing = JToken.Parse($@"
										  {{
											  'board_list':           '{ domain["api"] }/boards.json',
											  'thread_list':          '{ domain["api"] }/{{0}}/threads.json',
											  'archived_thread_list': '{ domain["api"] }/{{0}}/archive.json',
											  'catalog':              '{ domain["api"] }/{{0}}/catalog.json'
										  }}
										  ");

			JToken template = JToken.Parse($@"
										   {{
											   'api':  {{
														   'board':  '{ domain["api"] }/{{0}}/{{1}}.json',
														   'thread': '{ domain["api"] }/{{0}}/thread/{{1}}.json'
												}},
											   'http': {{
														   'board':  '{ domain["api"] }/{{0}}/{{1}}',
														   'thread': '{ domain["api"] }/{{0}}/thread/{{1}}'
												}},
											   'data': {{
														   'file':   '{ domain["file"] }/{{0}}/{{1}}{{2}}',
														   'thumbs': '{ domain["thumbs"] }/{{0}}/{{1}}s.jpg',
														   'static': '{ domain["static"] }/image/{{0}}'
												}}
										   }}
										");

			Urls = JObject.FromObject(template);
			Urls.Add("domain", domain);
			Urls.Add("listing", listing);
		}



		///////////////////////////////////
		///   Public Instance Methods   ///
		///////////////////////////////////

		public string BoardList()
		{
			return Urls["listing"].Value<string>("board_list");
		}


		public string PageUrls(int page)
		{
			return string.Format(Urls["api"].Value<string>("board"),
								 BoardName,
								 page);
		}


		public string Catalog()
		{
			return string.Format(Urls["listing"].Value<string>("catalog"),
								 BoardName);
		}


		public string ThreadList()
		{
			return string.Format(Urls["listing"].Value<string>("thread_list"),
								 BoardName);
		}


		public string ThreadApiUrl(int threadID)
		{
			return string.Format(Urls["api"].Value<string>("thread"),
								 BoardName,
								 threadID);
		}


		public string ThreadUrl(int threadID)
		{
			return string.Format(Urls["http"].Value<string>("thread"),
								 BoardName,
								 threadID);
		}


		public string FileUrls(string tim, string ext)
		{
			return string.Format(Urls["data"].Value<string>("file"),
								 BoardName,
								 tim,
								 ext);
		}


		public string ThumbUrls(string tim)
		{
			return string.Format(Urls["data"].Value<string>("thumbs"),
								 BoardName,
								 tim);
		}
	}
}

//////////////////////////////////////////////////////////
///   Misc notes from the original basc-py4chan code   ///
//////////////////////////////////////////////////////////

/*
 4chan Static Data (Unique to 4chan, needs implementation)

 STATIC = {        
			   'flags': DOMAIN['static'] + '/image/country/{country}.gif',
			   'pol_flags': DOMAIN['static'] + '/image/country/troll/{country}.gif',
			   'spoiler': { # all known custom spoiler images, just fyi
			   'default': DOMAIN['static'] + '/image/spoiler.png',
			   'a': DOMAIN['static'] + '/image/spoiler-a.png',
			   'co': DOMAIN['static'] + '/image/spoiler-co.png',
			   'mlp': DOMAIN['static'] + '/image/spoiler-mlp.png',
			   'tg': DOMAIN['static'] + '/image/spoiler-tg.png',
			   'tg-alt': DOMAIN['static'] + '/image/spoiler-tg2.png',
			   'v': DOMAIN['static'] + '/image/spoiler-v.png',
			   'vp': DOMAIN['static'] + '/image/spoiler-vp.png',
			   'vr': DOMAIN['static'] + '/image/spoiler-vr.png'
		  }

 Maybe i should implement this
 */
