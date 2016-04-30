using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("MainPage");

            listView.SelectedItem = CurriculumListViewItem;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = listView.SelectedItem;
            if (item == CurriculumListViewItem)
                frame.Navigate(typeof(CurriculumPage));
            else if (item == ScheduleListViewItem)
                frame.Navigate(typeof(SchedulePage));
            else if (item == MidAlertListViewItem)
                frame.Navigate(typeof(MidAlertPage));
        }

        private async void logoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = await NPAPI.LogoutNPortal();

            //Send GA Event
            App.Current.GATracker.SendEvent("Session", "Logout", null, 0);

            if (result.Success)
                frame.Navigate(typeof(LoginPage));
            else
                await new MessageDialog(result.Message).ShowAsync();
        }
    }
}
