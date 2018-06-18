using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// 空白頁項目範本已記錄在 http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// 可以在本身使用或巡覽至框架內的空白頁面。
    /// </summary>
    public sealed partial class SchedulePage : Page
    {
        int CurrentAcademicYear {
            get
            {
                return currentAcademicYear;
            }
            set
            {
                currentAcademicYear = value;
                OnAcademicYearChanged?.Invoke(this, null);
            }
        }

        event EventHandler OnAcademicYearChanged; 

        const int MinAcademicYear = 100;
        int currentAcademicYear;
        int maxAcademicYear;

        Schedule schedule;

        public SchedulePage()
        {
            this.InitializeComponent();

            var today = DateTime.Today;

            if(today.Month > 7)
                maxAcademicYear = today.Year - 1911;
            else
                maxAcademicYear = today.Year - 1 - 1911;

            OnAcademicYearChanged += SchedulePage_OnAcademicYearChanged;
        }

        private void SchedulePage_OnAcademicYearChanged(object sender, EventArgs e)
        {
            nextYearButton.IsEnabled = currentAcademicYear < maxAcademicYear;
            previousYearButton.IsEnabled = currentAcademicYear > MinAcademicYear;
            titleTextBlock.Text = string.Format("{0}學年度行事曆", currentAcademicYear);
            previousYearButton.Content = string.Format("{0}學年", currentAcademicYear - 1);
            nextYearButton.Content = string.Format("{0}學年", currentAcademicYear + 1);
            GetSchedule(currentAcademicYear);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("SchedulePage");

            //Send GA Event
            App.Current.GATracker.SendEvent("Schedule", "Get Schedule", null, 0);

            CurrentAcademicYear = maxAcademicYear;
        }

        private async void GetSchedule(int academicYear)
        {
            try
            {
                var schedule = await NPAPI.GetSchedule(academicYear);
                this.schedule = schedule;
                calendar.DisplayDateStart = new DateTime(academicYear + 1911, 8, 1);
                calendar.DisplayDateEnd = new DateTime(academicYear + 1 + 1911, 7, 31);
                calendar.SelectionMode = WinRTXamlToolkit.Controls.CalendarSelectionMode.SingleDate;
                if (this.schedule.monthSchedules.ContainsKey(calendar.DisplayDate.Month))
                    listView.ItemsSource = this.schedule.monthSchedules[calendar.DisplayDate.Month];
                else
                {
                    listView.ItemsSource = null;
                    listView.Items.Clear();
                }
            }
            catch (Exception e)
            {
                listView.ItemsSource = null;
                listView.Items.Clear();
                listView.Items.Add("讀取失敗，請稍後再試。");
                listView.Items.Add(e.Message);
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

        private void previousYearButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentAcademicYear--;
        }

        private void nextYearButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentAcademicYear++;
        }

        private async void openInWebButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(string.Format("http://www.cc.ntut.edu.tw/~wwwoaa/oaa-nwww/oaa-cal/oaa-cal_{0}.html", currentAcademicYear)));

            //Send GA Event
            App.Current.GATracker.SendEvent("Other", "Go to Schedule Web " + currentAcademicYear, null, 0);
        }
    }
}
