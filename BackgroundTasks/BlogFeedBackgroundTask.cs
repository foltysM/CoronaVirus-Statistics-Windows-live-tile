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

        private static void UpdateTile(SyndicationFeed feed)
        {
            // Create a tile update manager for the specified syndication feed.
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();
            updater.EnableNotificationQueue(true);
            updater.Clear();

            // Keep track of the number feed items that get tile notifications.
            int itemCount = 0;

            // Create a tile notification for each feed item.
            foreach (var item in feed.Items)
            {
                XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Text03);

                var title = item.Title;
                string titleText = title.Text == null ? String.Empty : title.Text;
                tileXml.GetElementsByTagName(textElementName)[0].InnerText = titleText;

                // Create a new tile notification.
                updater.Update(new TileNotification(tileXml));

                // Don't create more than 5 notifications.
                if (itemCount++ > 5) break;
            }
        }

        // Although most HTTP servers do not require User-Agent header, others will reject the request or return
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers.
        static string customHeaderName = "User-Agent";
        static string customHeaderValue = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        static string textElementName = "text";
        static string feedUrl = @"https://thevirustracker.com/free-api?global=stats";
    }
}
