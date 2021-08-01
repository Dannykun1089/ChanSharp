using Newtonsoft.Json.Linq;

namespace ChanSharp
{
    internal class UrlGenerator
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private string BoardName { get; set; }
        private string Protocol { get; set; }

        private JToken Domain { get => Domain_get(); }
        private JToken Template { get => Template_get(); }
        private JToken Listing { get => Listing_get(); }

        private JObject Urls { get => Urls_get(); }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public UrlGenerator(string boardName, bool https = true)
        {
            BoardName = boardName;
            Protocol = https ? "https://" : "http://";
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
                                  tim);
        }


        public JObject SiteUrls()
        {
            return Urls;
        }



        /////////////////////////////////
        ///   Property get; Methods   ///
        /////////////////////////////////

        private JToken Domain_get()
        {
            return JToken.Parse($@"{{
                                        'api':             '{ Protocol }a.4cdn.org',
                                        'boards':          '{ Protocol }boards.4chan.org' ,
                                        'boards_4channel': '{ Protocol }boards.4channel.org',
                                        'file':            '{ Protocol }i.4cdn.org',
                                        'thumbs':          '{ Protocol }i.4cdn.org',
                                        'static':          '{ Protocol }s.4cdn.org'
                                   }}");
        }


        private JToken Template_get()
        {
            return JToken.Parse($@"{{
                                        'api':  {{
                                                     'board':  '{ Domain["api"] }/{{0}}/{{1}}.json',
                                                     'thread': '{ Domain["api"] }/{{0}}/thread/{{1}}.json'
                                                }},
                                        'http': {{
                                                     'board':  '{ Domain["api"] }/{{0}}/{{1}}',
                                                     'thread': '{ Domain["api"] }/{{0}}/thread/{{1}}'
                                                }},
                                        'data': {{
                                                     'file':   '{ Domain["file"] }/{{0}}/{{1}}{{2}}',
                                                     'thumbs': '{ Domain["thunbs"] }/{{0}}/{{1}}s.jpg',
                                                     'static': '{ Domain["static"] }/image/{{0}}'
                                                }}
                                   }}");
        }


        private JToken Listing_get()
        {
            return JToken.Parse($@"{{
                                        'board_list':           '{ Domain["api"] }/boards.json',
                                        'thread_list':          '{ Domain["api"] }/{{0}}/threads.json',
                                        'archived_thread_list': '{ Domain["api"] }/{{0}}/archive.json',
                                        'catalog':              '{ Domain["api"] }/{{0}}/catalog.json'
                                   }}");
        }


        private JObject Urls_get()
        {
            JObject retVal = JObject.FromObject(Template);
            retVal.Add("domain", Domain);
            retVal.Add("listing", Listing);

            return retVal;
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
*/
