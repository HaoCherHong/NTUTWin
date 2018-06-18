using System;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

            frame.Navigated += Frame_Navigated;

            //Default Page
            listView.SelectedItem = CurriculumListViewItem;
        }

        private void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            //Set listView selection and prevent event benn triggered
            listView.SelectionChanged -= ListView_SelectionChanged;
            if (e.SourcePageType == typeof(CurriculumPage))
                listView.SelectedItem = CurriculumListViewItem;
            else if (e.SourcePageType == typeof(SchedulePage))
                listView.SelectedItem = ScheduleListViewItem;
            else if (e.SourcePageType == typeof(MidAlertPage))
                listView.SelectedItem = MidAlertListViewItem;
            else if(e.SourcePageType == typeof(AttendenceAndHonorsPage))
                listView.SelectedItem = AttendenceAndHonorsListViewItem;
            else if (e.SourcePageType == typeof(CreditsPage))
                listView.SelectedItem = CreditsListViewItem;
			else if(e.SourcePageType == typeof(PortalPage))
				listView.SelectedItem = PortalListViewItem;
            else
                listView.SelectedItem = null;
            listView.SelectionChanged += ListView_SelectionChanged;

            //Set navigate back visibility

            foreach (PageStackEntry entry in frame.BackStack)
                if (entry.SourcePageType == typeof(LoginPage))
                    frame.BackStack.Remove(entry);

            navigateBackButton.Visibility = frame.CanGoBack ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = listView.SelectedItem;

            if (item == null)
                return;

            if (item == CurriculumListViewItem)
                frame.Navigate(typeof(CurriculumPage));
            else if (item == ScheduleListViewItem)
                frame.Navigate(typeof(SchedulePage));
            else if (item == MidAlertListViewItem)
                frame.Navigate(typeof(MidAlertPage));
            else if (item == AttendenceAndHonorsListViewItem)
                frame.Navigate(typeof(AttendenceAndHonorsPage));
            else if (item == CreditsListViewItem)
                frame.Navigate(typeof(CreditsPage));
			else if (item == PortalListViewItem)
				frame.Navigate(typeof(PortalPage));

			frame.BackStack.Clear();
            navigateBackButton.Visibility = Visibility.Collapsed;
        }

        private async void logoutButton_Click(object sender, RoutedEventArgs args)
        {
            logoutButton.IsEnabled = false;
            try
            {
                await NPAPI.LogoutNPortal();
                frame.Navigate(typeof(LoginPage));

                //Send GA Event
                string id = ApplicationData.Current.RoamingSettings.Values.ContainsKey("id") ? ApplicationData.Current.RoamingSettings.Values["id"] as string : "N/A";
                App.Current.GATracker.SendEvent("Session", "Logout", id, 0);
            }
            catch (Exception e)
            {
                await new MessageDialog(e.Message, "錯誤").ShowAsync();
            }

            logoutButton.IsEnabled = true;
        }

        private void rateAndReviewButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(FeedbackPage));
        }

        private void navigateBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (frame.CanGoBack)
                frame.GoBack();
        }

        private void donateButton_Click(object sender, RoutedEventArgs e)
        {
            frame.Navigate(typeof(DonatePage));
        }
    }
}
