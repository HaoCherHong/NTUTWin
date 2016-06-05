using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class SchedulePage : Page
    {
        Schedule schedule;
        public SchedulePage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("SchedulePage");

            //Send GA Event
            App.Current.GATracker.SendEvent("Schedule", "Get Schedule", null, 0);

            GetSchedule();
        }

        private async void GetSchedule()
        {
            var result = await NPAPI.GetSchedule();
            if(result.Success)
            {
                schedule = result.Data;
                calendar.DisplayDateStart = new DateTime(2016, 1, 1);
                calendar.DisplayDateEnd = new DateTime(2016, 12, 31);
                calendar.SelectionMode = WinRTXamlToolkit.Controls.CalendarSelectionMode.SingleDate;
                if (schedule.monthSchedules.ContainsKey(calendar.DisplayDate.Month))
                    listView.ItemsSource = schedule.monthSchedules[calendar.DisplayDate.Month];
                else
                {
                    listView.ItemsSource = null;
                    listView.Items.Clear();
                }
            }
            else
            {
                listView.Items.Clear();
                listView.Items.Add("讀取失敗，請稍後再試。");
                listView.Items.Add(result.Message);
            }
        }

        private void calendar_DisplayDateChanged(object sender, WinRTXamlToolkit.Controls.CalendarDateChangedEventArgs e)
        {
            if (schedule != null)
                if (schedule.monthSchedules.ContainsKey(calendar.DisplayDate.Month))
                    listView.ItemsSource = schedule.monthSchedules[calendar.DisplayDate.Month];
                else
                {
                    listView.ItemsSource = null;
                    listView.Items.Clear();
                }
        }

        private void calendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView.ItemsSource == null)
                return;

            var date = calendar.SelectedDate;
            var monthSchedule = (List<Schedule.SchoolEvent>)listView.ItemsSource;
            foreach(Schedule.SchoolEvent schoolEvent in monthSchedule)
            {
                if(schoolEvent.date == date)
                {
                    listView.SelectedItem = schoolEvent;
                    break;
                }
            }
        }

        private void listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var schoolEvent = listView.SelectedItem as Schedule.SchoolEvent;
            if (schoolEvent != null)
            {
                calendar.SelectedDate = schoolEvent.date;
                detailsTextBlock.Text = string.Format("時間：{0}\n{1}", schoolEvent.timeString, schoolEvent.description);
            }
        }
    }
}
