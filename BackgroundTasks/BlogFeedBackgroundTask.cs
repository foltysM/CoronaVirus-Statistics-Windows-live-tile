using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.Web.Syndication;
using Newtonsoft.Json;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.Media.Playback;
using Microsoft.CognitiveServices.Speech;
using Windows.Storage;
using Windows.Media.Core;
using System.Runtime.InteropServices;

namespace BackgroundTasks
{
    public sealed class BlogFeedBackgroundTask : IBackgroundTask
    {
        
        private MediaPlayer mediaPlayer;
        private static string text2Speech = "";

        public BlogFeedBackgroundTask()
        {
            this.mediaPlayer = new MediaPlayer();
        }

        private async void CoronaSpeakAsync()
        {
            // Creates an instance of a speech config with specified subscription key and service region.
            // Replace with your own subscription key and service region (e.g., "westus").
           // var config = SpeechConfig.FromSubscription("b1323ad73dda43038546ea1a82594e8a", "northeurope");
            var config = SpeechConfig.FromSubscription("b1323ad73dda43038546ea1a82594e8a", "northeurope");


            try
            {
                // Creates a speech synthesizer.
                using (var synthesizer = new SpeechSynthesizer(config, null))
                {
                    
                    using (var result = await synthesizer.SpeakTextAsync(text2Speech).ConfigureAwait(false))
                    {
                        // Checks result.
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            Console.WriteLine("Speech Synthesis Succeeded.");
                            Console.WriteLine(NotifyType.StatusMessage);

                            // Since native playback is not yet supported on UWP (currently only supported on Windows/Linux Desktop),
                            // use the WinRT API to play audio here as a short term solution.
                            using (var audioStream = AudioDataStream.FromResult(result))
                            {
                                // Save synthesized audio data as a wave file and use MediaPlayer to play it
                                var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "outputaudio.wav");
                                await audioStream.SaveToWaveFileAsync(filePath);
                                mediaPlayer.Source = MediaSource.CreateFromStorageFile(await StorageFile.GetFileFromPathAsync(filePath));
                                mediaPlayer.Play();
                            }
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);

                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine($"CANCELED: Reason={cancellation.Reason}");
                            sb.AppendLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                            sb.AppendLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");

                            Console.WriteLine(sb.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(NotifyType.ErrorMessage);
            }
        }
      
        private enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        internal static class NativeMethods //nowa klasa z metodami
        {
            // Declares a managed prototype for unmanaged function.
            [DllImport("E:\\LiveTile\\AsmTest.dll")]
            internal static extern int CPUSpeed();
            [DllImport("E:\\LiveTile\\AsmTest.dll")]
            internal static extern int getCPUFamily();
            [DllImport("E:\\LiveTile\\AsmTest.dll")]
            internal static extern int getCPUModel();
        }

        class InformationCorona
        {
            public string Total_cases { get; set; }
            public string Total_recovered { get; set; }
            public string Total_unresolved { get; set; }
            public string Total_deaths { get; set; }
            public string Total_new_cases_today { get; set; }
            public string Total_new_deaths_today { get; set; }
            public string Total_active_cases { get; set; }
            public string Total_serious_cases { get; set; }          
            public string Total_affected_countries { get; set; }
        }
        class InfoExport
        {
            public int Total_cases { get; set; }
            public int Total_deaths { get; set; }
            public int New_cases_today { get; set; }
            public int New_deaths_today { get; set; }
            public int Countries { get; set; }
        }

        class ReceivedInfo
        {
            public List<InformationCorona> Results { get; set; }

            public string Stat { get; set; }
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            int CPU_Speed = NativeMethods.CPUSpeed(); 
            int cpumodel = NativeMethods.getCPUModel();
            int family = NativeMethods.getCPUFamily();

            // Get a deferral, to prevent the task from closing prematurely
            // while asynchronous code is still running.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            // Download the feed.
            InfoExport info = GetCoronaInfo();
            //var feed = await GetCoronaData();

            // Update the live tile with the feed items.
            await UpdateAsync(info);
            CoronaSpeakAsync();
            // Inform the system that the task is finished.
            deferral.Complete();
        }

        private static InfoExport GetCoronaInfo()
        {
            string infoJSON = "";
            WebRequest request = WebRequest.Create(feedUrl);
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

            InfoExport export = new InfoExport
            {
                Countries = int.Parse(infObj.Results[0].Total_affected_countries),
                New_cases_today = int.Parse(infObj.Results[0].Total_new_cases_today),
                New_deaths_today = int.Parse(infObj.Results[0].Total_new_deaths_today),
                Total_cases = int.Parse(infObj.Results[0].Total_cases),
                Total_deaths = int.Parse(infObj.Results[0].Total_deaths)
            };

            return export;
        }

        private static async Task UpdateAsync(InfoExport info)
        {
            string number_of_gc = info.Total_cases.ToString();
            string number_of_gd = info.Total_deaths.ToString();
            string number_of_tc = info.New_cases_today.ToString();
            string number_of_td = info.Total_deaths.ToString();

            string global_c = "Global infections: " + number_of_gc;
            string global_d = "Global deaths: " + number_of_gd;
            string todays_c = "Today's infections: " + number_of_tc;
            string todays_d = "Today's deaths: " + number_of_td;

            text2Speech = global_c + global_d + todays_c + todays_d;
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

            
            //Task returnedTask = GetTaskAsync();
        }



        // Although most HTTP servers do not require User-Agent header, others will reject the request or return
        // a different response if this header is missing. Use SetRequestHeader() to add custom headers.
        //static string customHeaderName = "User-Agent";
        //static string customHeaderValue = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";

        private static readonly string feedUrl = @"https://thevirustracker.com/free-api?global=stats";
    }
}
