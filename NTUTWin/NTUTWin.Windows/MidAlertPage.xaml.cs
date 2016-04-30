using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        public MidAlertPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("MidAlertPage");

            await GetMidAlert();
        }

        private async Task GetMidAlert()
        {
            var request = await NPAPI.GetMidAlerts();
            if(request.Success)
            {
                titleTextBlock.Text = request.Data.Semester + " 期中預警";
                listView.ItemsSource = request.Data.Alerts;
            }
            else
            {
                if (request.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Try background login
                    var result = await NPAPI.BackgroundLogin();
                    if (result.Success)
                        await GetMidAlert();
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                {
                    await new MessageDialog(request.Message).ShowAsync();
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
