using System;
using Newtonsoft.Json.Linq;

namespace BascSharp4Chan
{
    public class UrlGenerator
    {
        //////////////////////
        ///   Properties   ///
        //////////////////////

        private static string  BoardName        { get; set; }
        private static string  Protocol         { get; set; }

        private static JObject Urls             { get; set; }

        private static JToken  Domain           { get; set; }
        private static JToken  Template         { get; set; }
        private static JToken  Listing          { get; set; }



        ////////////////////////
        ///   Constructors   ///
        ////////////////////////

        public UrlGenerator(string boardName, bool https = true)
        {
            BoardName = boardName;
            Protocol = https ? "https://" : "http://";

            Domain = JToken.Parse($@"{{
                                        'api':             '{ Protocol }a.4cdn.org',
                                        'boards':          '{ Protocol }boards.4chan.org' ,
                                        'boards_4channel': '{ Protocol }boards.4channel.org',
                                        'file':            '{ Protocol }i.4cdn.org',
                                        'thumbs':          '{ Protocol }i.4cdn.org',
                                        'static':          '{ Protocol }s.4cdn.org'
                                    }}");

            Template = JToken.Parse($@"{{
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

            Listing = JToken.Parse($@"{{
                                           'board_list':           '{ Domain["api"] }/boards.json',
                                           'thread_list':          '{ Domain["api"] }/{{0}}/threads.json',
                                           'archived_thread_list': '{ Domain["api"] }/{{0}}/archive.json',
                                           'catalog':              '{ Domain["api"] }/{{0}}/catalog.json'
                                      }}");


            Urls = JObject.FromObject(Template);
            Urls.Add("domain", Domain);
            Urls.Add("listing", Listing);
        }



        ///////////////////////////////////
        ///   Public Instance Methods   ///
        ///////////////////////////////////

        public string BoardList()
        {
            return (string) Urls["listing"]["board_list"];
        }


        public string PageUrls(int page)
        {
            return string.Format((string) Urls["api"]["board"], BoardName, page);
        }


        public string Catalog()
        {
            return string.Format((string) Urls["listing"]["catalog"], BoardName);
        }


        public string ThreadList()
        {
            return string.Format((string)Urls["listing"]["thread_list"], BoardName);
        }


        public string ThreadAPIUrls(int threadID)
        {
            return string.Format((string)Urls["api"]["thread"], BoardName, threadID);
        }


        public string ThreadUrls(string threadID)
        {
           return string.Format((string)Urls["http"]["thread"], BoardName, threadID);
        }


        public string FileUrls(string tim, string ext)
        {
           return string.Format((string)Urls["data"]["file"], BoardName, tim, ext);
        }


        public string ThumbUrls(string tim)
        {
            return string.Format((string)Urls["data"]["thumbs"], tim);
        }


        public static JObject SiteUrls()
        {
            return Urls;
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
