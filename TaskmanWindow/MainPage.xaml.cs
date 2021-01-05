
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace TaskmanWindow
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public string LocalFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\";


		public MainPage()
        {
            this.InitializeComponent();

            ApplicationView.PreferredLaunchViewSize = new Size(800, 1200);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;

            ListView_Processes.ItemsSource = Global.ProcessList;

            if (ApiInformation.IsApiContractPresent(
                             "Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }


            //SendToWinForms();
            /*ValueSet request = new ValueSet();
            request.Add("KEY", "key");
            App.SendToWinForms(request);*/

            // Hook Window Close

            SystemNavigationManagerPreview mgr =
                SystemNavigationManagerPreview.GetForCurrentView();
            mgr.CloseRequested += SystemNavigationManager_CloseRequested;

            // Hook Minimize
            Window.Current.VisibilityChanged += new WindowVisibilityChangedEventHandler(Minimize_Hook);
        }


      

		private async void Minimize_Hook(object sender, VisibilityChangedEventArgs e)
		{
            Debug.WriteLine(e.Visible);
            if (!e.Visible)
            {
                await CloseWindowOpenTray();
            }
        }

        private async Task CloseWindowOpenTray()
		{
            await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();
        }

		protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private async void SystemNavigationManager_CloseRequested(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)
        {
            Deferral deferral = e.GetDeferral();
            if (ApiInformation.IsApiContractPresent(
                             "Windows.ApplicationModel.FullTrustAppContract", 1, 0))
            {
                await FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
            }
            e.Handled = false;
            deferral.Complete();
        }

        ObservableCollection<OpenProcess> ProcessList_short_temp = new ObservableCollection<OpenProcess>();
        ObservableCollection<OpenProcess> ProcessList_temp = new ObservableCollection<OpenProcess>();

        private void TextBox_Filter_TextChanged(object sender, TextChangedEventArgs e)
		{
            ProcessList_short_temp = (ObservableCollection<OpenProcess>)ListView_Processes.ItemsSource;
            ProcessList_temp = new ObservableCollection<OpenProcess>();
            foreach (var process in Global.ProcessList)
            {
                string[] filter_list = TextBox_Filter.Text.ToLower().Split(' ');

                if (filter_list.All(process.FilePath.ToLower().Contains))
                {
                    ProcessList_temp.Add(process);
                }
            }
            var test = ProcessList_temp.SequenceEqual(ProcessList_short_temp);

            if (!test)
            {
                ListView_Processes.ItemsSource = ProcessList_temp;
            }
        }

        private void Button_ListViewItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            OpenProcess item = ((sender as Button).DataContext as OpenProcess);
            ValueSet request = new ValueSet();
            request.Add("item", item.FilePath);
            App.SendToWinForms(request);
            Global.ProcessList.Remove(item);
            ProcessList_temp.Remove(item);
            ProcessList_short_temp.Remove(item);
        }
    }
}
