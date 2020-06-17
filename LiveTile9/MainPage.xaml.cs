using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Syndication;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using System.Runtime.InteropServices;

namespace LiveTile9
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
            this.mediaPlayer = new MediaPlayer();
            int CPU_Speed = NativeMethods.CPUSpeed();
            int cpumodel = NativeMethods.getCPUModel();
            int familyINT = NativeMethods.getCPUFamily();
            string models = cpumodel.ToString();
            string speeds = CPU_Speed.ToString();
            string familys = familyINT.ToString();
            model.Text = "CPU Model: " + models;
            family.Text = "CPU Family: "+familys;
            speed.Text = "CPU Speed: " + speeds;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.RegisterBackgroundTask();
        }


        private async void RegisterBackgroundTask()
        {
            var backgroundAccessStatus = await BackgroundExecutionManager.RequestAccessAsync();
            if (backgroundAccessStatus == BackgroundAccessStatus.AllowedSubjectToSystemPolicy ||
                backgroundAccessStatus == BackgroundAccessStatus.AlwaysAllowed)
            {
                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == taskName)
                    {
                        task.Value.Unregister(true);
                    }
                }
                
                BackgroundTaskBuilder taskBuilder = new BackgroundTaskBuilder();
                taskBuilder.Name = taskName;
                taskBuilder.TaskEntryPoint = taskEntryPoint;
                uint rRate = Convert.ToUInt32(refreshRate); 
                taskBuilder.SetTrigger(new TimeTrigger(rRate, false));
                var registration = taskBuilder.Register();
            }
        }
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

        private const string taskName = "BlogFeedBackgroundTask";
        private const string taskEntryPoint = "BackgroundTasks.BlogFeedBackgroundTask";

        //TextBlock speed = new TextBlock();
        //TextBlock model = new TextBlock();
        //TextBlock family = new TextBlock();
        private MediaPlayer mediaPlayer;

        public double refreshRate { get; private set; }

        private void TextBlock_SelectionChanged(object sender, RoutedEventArgs e)
        {

        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            double newValueMy = e.NewValue;
            refreshRate = newValueMy;
            Console.WriteLine(newValueMy);
        }

        private void TextBlock_SelectionChanged_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
