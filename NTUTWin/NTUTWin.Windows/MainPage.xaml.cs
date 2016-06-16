//#define RESET_ASK_FOR_STAT

using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("MainPage");

            //Default Page
            listView.SelectedItem = CurriculumListViewItem;

            frame.Navigated += Frame_Navigated;

            var roamingSettings = ApplicationData.Current.RoamingSettings;

#if DEBUG && RESET_ASK_FOR_STAT
            roamingSettings.Values.Remove("AskedForCreditStat");
            roamingSettings.Values.Remove("AskedForAttendenceAndRewardsStat");
#endif

            //Check has logged in
            if(roamingSettings.Values.ContainsKey("id") && roamingSettings.Values.ContainsKey("password"))
            {
                //Ask for credit stat
                if (!(roamingSettings.Values.ContainsKey("AskedForCreditStat") && (bool)roamingSettings.Values["AskedForCreditStat"]))
                {
                    var result = await AskForCreditStat();
                    roamingSettings.Values["AskedForCreditStat"] = result;
                }
                //Ask for attendence & rewards stat
                else if (!(roamingSettings.Values.ContainsKey("AskedForAttendenceAndRewardsStat") && (bool)roamingSettings.Values["AskedForAttendenceAndRewardsStat"]))
                {
                    var result = await AskForAttendenceAndRewardsStat();
                    roamingSettings.Values["AskedForAttendenceAndRewardsStat"] = result;
                }
            }
        }

        private void Frame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            if (e.SourcePageType == typeof(CurriculumPage))
                listView.SelectedItem = CurriculumListViewItem;
            else if (e.SourcePageType == typeof(SchedulePage))
                listView.SelectedItem = ScheduleListViewItem;
            else if (e.SourcePageType == typeof(MidAlertPage))
                listView.SelectedItem = MidAlertListViewItem;
            else if(e.SourcePageType == typeof(AttendenceAndHonorsPage))
                listView.SelectedItem = AttendenceAndHonorsListViewItem;
            else
                listView.SelectedItem = null;
        }

        private async Task<bool> AskForCreditStat()
        {
            MessageDialog dialog = new MessageDialog("為了新增學分計算功能，我們需要學分資料做測試，你願意匿名提供您的學分資料嗎？", "學分計算");
            dialog.Commands.Add(new UICommand("願意", null, true));
            dialog.Commands.Add(new UICommand("不願意", null, false));
            dialog.DefaultCommandIndex = 0;
            var operation = await dialog.ShowAsync();
            if ((bool)operation.Id)
            {
                var result = await SendCreditStat();
                if (result)
                {
                    await new MessageDialog("感謝您！學分資料已送出。", "學分計算").ShowAsync();
                    return true;
                }
                else
                {
                    await new MessageDialog("感謝您！但很抱歉，發生了一些問題，資料無法送出。", "學分計算").ShowAsync();
                    return false;
                }
            }
            else
                return true;
        }

        private async Task<bool> AskForAttendenceAndRewardsStat()
        {
            MessageDialog dialog = new MessageDialog("即將新增查詢缺曠獎懲功能，我們需要資料做測試，你願意匿名提供您的資料嗎？", "缺曠獎懲");
            dialog.Commands.Add(new UICommand("願意", null, true));
            dialog.Commands.Add(new UICommand("不願意", null, false));
            dialog.DefaultCommandIndex = 0;
            var operation = await dialog.ShowAsync();
            if ((bool)operation.Id)
            {
                var result = await SendAttendenceAndRewardsStat();
                if (result)
                {
                    await new MessageDialog("感謝您！資料已送出。", "缺曠獎懲").ShowAsync();
                    return true;
                }
                else
                {
                    await new MessageDialog("感謝您！但很抱歉，發生了一些問題，資料無法送出。", "缺曠獎懲").ShowAsync();
                    return false;
                }
            }
            else
                return true;
        }

        private async Task<bool> SendCreditStat()
        {
            var result = await NPAPI.SendCreditStat();
            if(result.Success)
                return true;
            else
            {
                if (result.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Try background login
                    var loginResult = await NPAPI.BackgroundLogin();
                    if (loginResult.Success)
                        return await SendCreditStat();
                    else
                        return false;
                }
                else
                    return false;
            }
        }

        private async Task<bool> SendAttendenceAndRewardsStat()
        {
            var result = await NPAPI.SendAttendenceAndHonorsStat();
            if (result.Success)
                return true;
            else
            {
                if (result.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Try background login
                    var loginResult = await NPAPI.BackgroundLogin();
                    if (loginResult.Success)
                        return await SendAttendenceAndRewardsStat();
                    else
                        return false;
                }
                else
                    return false;
            }
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
            else if (item == AttendenceAndHonorsListViewItem)
                frame.Navigate(typeof(AttendenceAndHonorsPage));
        }

        private async void logoutButton_Click(object sender, RoutedEventArgs e)
        {
            logoutButton.IsEnabled = false;
            var result = await NPAPI.LogoutNPortal();

            if (result.Success)
            {
                frame.Navigate(typeof(LoginPage));

                //Send GA Event
                string id = ApplicationData.Current.RoamingSettings.Values.ContainsKey("id") ? ApplicationData.Current.RoamingSettings.Values["id"] as string : "N/A";
                App.Current.GATracker.SendEvent("Session", "Logout", id, 0);
            }
            else
                await new MessageDialog(result.Message, "錯誤").ShowAsync();

            logoutButton.IsEnabled = true;
        }

        private async void rateAndReviewButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:reviewapp?appid=5c805945-21cb-4160-9a45-1de3ec408a9d"));

            //Send GA Event
            App.Current.GATracker.SendEvent("Other", "Go Rating Page", null, 0);
        }
    }
}
