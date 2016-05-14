#define DEBUG_DOC

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白頁項目範本已記錄在 http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class MidAlertPage : Page
    {
#if DEBUG_DOC
        private int skip = 0;
#endif
        public MidAlertPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("MidAlertPage");

#if DEBUG && DEBUG_DOC
            Windows.UI.Core.CoreWindow.GetForCurrentThread().KeyDown += async (Windows.UI.Core.CoreWindow keySender, Windows.UI.Core.KeyEventArgs keyEvent) =>
            {
                if (keyEvent.VirtualKey == Windows.System.VirtualKey.Right)
                    await DebugMidAlert();
            };
#endif
            await GetMidAlert();

        }

#if DEBUG && DEBUG_DOC
        private async Task DebugMidAlert()
        {
            courseNameTextBlock.Text = "";
            var request = await NPAPI.DebugMidAlerts(skip++);
            if (request.Success)
            {
                courseNameTextBlock.Text = "(請選擇)";
                titleTextBlock.Text = request.Data.Semester + " 期中預警";
                listView.ItemsSource = request.Data.Alerts;

                //Send GA Event
                string id = ApplicationData.Current.RoamingSettings.Values.ContainsKey("id") ? ApplicationData.Current.RoamingSettings.Values["id"] as string : "N/A";
                App.Current.GATracker.SendEvent("Mid Alert", "Get Mid Alert", id, 0);

                if (request.Data.Alerts.Count == 0)
                {
                    Debugger.Break();
                    return;
                }
            }
            else
            {
                Debugger.Break();

                if (request.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Send GA Event
                    App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                    //Try background login
                    var result = await NPAPI.BackgroundLogin();
                    if (result.Success)
                        await GetMidAlert();
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                {
                    listView.Items.Clear();
                    listView.Items.Add("讀取失敗，請稍後再試。");
                    listView.Items.Add(request.Message);
                }

                return;
            }
            await DebugMidAlert();
        }
#endif

        private async Task GetMidAlert()
        {
            courseNameTextBlock.Text = "";
            var request = await NPAPI.GetMidAlerts();
            if(request.Success)
            {
                courseNameTextBlock.Text = "(請選擇)";
                titleTextBlock.Text = request.Data.Semester + " 期中預警";
                listView.ItemsSource = request.Data.Alerts;

                //Send GA Event
                string id = ApplicationData.Current.RoamingSettings.Values.ContainsKey("id") ? ApplicationData.Current.RoamingSettings.Values["id"] as string : "N/A";
                App.Current.GATracker.SendEvent("Mid Alert", "Get Mid Alert", id, 0);

            }
            else
            {
                if (request.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Send GA Event
                    App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                    //Try background login
                    var result = await NPAPI.BackgroundLogin();
                    if (result.Success)
                        await GetMidAlert();
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                {
                    listView.Items.Clear();
                    listView.Items.Add("讀取失敗，請稍後再試。");
                    listView.Items.Add(request.Message);
                }
            }
        }

        private async void listView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is MidAlerts.MidAlert)
            {
                var alertItem = e.ClickedItem as MidAlerts.MidAlert;
                var message = string.Format("預警:{3}\n{0} {1} {2}學分\n{4}",
                    alertItem.CourseNumber,
                    alertItem.Type,
                    alertItem.Credit,
                    alertItem.AlertSubmitted ? ((alertItem.Alerted ? "是" : "否") + " (" + alertItem.Ratio.Alerted + "/" + alertItem.Ratio.All  +")") : "尚未送出",
                    alertItem.Note);
                await new MessageDialog(message, alertItem.CourseName).ShowAsync();
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(listView.SelectedItem is MidAlerts.MidAlert))
                return;
            var alertItem = listView.SelectedItem as MidAlerts.MidAlert;
            courseNameTextBlock.Text = alertItem.CourseName;
            var message = string.Format("預警:\t{3}\n課號:\t{0}\n類型:\t{1}\n學分:\t{2}\n\n{4}",
                alertItem.CourseNumber,
                alertItem.Type,
                alertItem.Credit,
                alertItem.AlertSubmitted ? ((alertItem.Alerted ? "是" : "否") + " (" + alertItem.Ratio.Alerted + "/" + alertItem.Ratio.All + ")") : "尚未送出",
                alertItem.Note);
            detailTextBlock.Text = message;
        }

        private void logoutButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
