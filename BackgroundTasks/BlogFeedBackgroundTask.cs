using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

// Added during quickstart
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.Web.Syndication;
using Newtonsoft.Json;
using Microsoft.Toolkit.Uwp.Notifications;

namespace BackgroundTasks
{
    public sealed class BlogFeedBackgroundTask : IBackgroundTask
    {
        class InformationCorona
        {
            public string total_cases { get; set; }
            public string total_recovered { get; set; }
            public string total_unresolved { get; set; }
            public string total_deaths { get; set; }
            public string total_new_cases_today { get; set; }
            public string total_new_deaths_today { get; set; }
            public string total_active_cases { get; set; }
            public string total_serious_cases { get; set; }          
            public string total_affected_countries { get; set; }
        }
        class InfoExport
        {
            public int total_cases { get; set; }
            public int total_deaths { get; set; }
            public int new_cases_today { get; set; }
            public int new_deaths_today { get; set; }
            public int countries { get; set; }
        }

        class ReceivedInfo
        {
            public List<InformationCorona> results { get; set; }

            public string stat { get; set; }
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            // Download the feed.
            InfoExport info = getCoronaInfo();
            //var feed = await GetCoronaData();

            // Update the live tile with the feed items.
            //UpdateTile(feed); //TODO 
            update(info);
            // Inform the system that the task is finished.
            deferral.Complete();
        }

        private static InfoExport getCoronaInfo()
        {
            string infoJSON = "";
            WebRequest request = WebRequest.Create(
              "https://thevirustracker.com/free-api?global=stats");
            WebResponse response = request.GetResponse();
            Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

            using (Stream dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                infoJSON = responseFromServer;
                // Display the content.
                Debug.WriteLine(responseFromServer);
            }
             response.Close();
            
            ReceivedInfo infObj = JsonConvert.DeserializeObject<ReceivedInfo>(infoJSON);

            InfoExport export = new InfoExport();
            export.countries = int.Parse(infObj.results[0].total_affected_countries);
            export.new_cases_today = int.Parse(infObj.results[0].total_new_cases_today);
            export.new_deaths_today = int.Parse(infObj.results[0].total_new_deaths_today);
            export.total_cases = int.Parse(infObj.results[0].total_cases);
            export.total_deaths = int.Parse(infObj.results[0].total_deaths);




            return export;
        }

        private static void update(InfoExport info)
        {
            string number_of_gc = info.total_cases.ToString();
            string number_of_gd = info.total_deaths.ToString();
            string number_of_tc = info.new_cases_today.ToString();
            string number_of_td = info.total_deaths.ToString();

            string global_c = "Global infections: " + number_of_gc;
            string global_d = "Global deaths: " + number_of_gd;
            string todays_c = "Today's infections: " + number_of_tc;
            string todays_d = "Today's deaths: " + number_of_td;

            // Construct the tile content            
            TileContent content = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = global_c

                                },

                                new AdaptiveText()
                                {
                                    Text = global_d,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = todays_c,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    },

                    TileWide = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = global_c,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = global_d,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = todays_c,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                },

                                new AdaptiveText()
                                {
                                    Text = todays_d,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    }
                }
            };
           
            var notification = new TileNotification(content.GetXml());

            // And send the notification
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
            
            ///TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }
        private static async Task<SyndicationFeed> GetCoronaData()
        {
            SyndicationFeed feed = null;

            try
            {
                // Create a syndication client that downloads the feed.  
                SyndicationClient client = new SyndicationClient();
                client.BypassCacheOnRetrieve = true;
                client.SetRequestHeader(customHeaderName, customHeaderValue);

                // Download the feed.
                feed = await client.RetrieveFeedAsync(new Uri(feedUrl)); // TODO błąd
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return feed;
        }


        // Although most HTTP servers do not require User-Agent header, others will reject the request or return
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers.
        static string customHeaderName = "User-Agent";
        static string customHeaderValue = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        static string textElementName = "text";
        static string feedUrl = @"https://thevirustracker.com/free-api?global=stats";
    }
}
