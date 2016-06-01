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
    public sealed partial class CurriculumPage : Page
    {
        private string name;

        //private StatusBarProgressIndicator progressbar = StatusBar.GetForCurrentView().ProgressIndicator;

        private ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

        public CurriculumPage()
        {
            this.InitializeComponent();

            //For debugging

            //var sessionId = roamingSettings.Values["JSESSIONID"].ToString();
            //roamingSettings.Values.Clear();
            //roamingSettings.Values.Add("JSESSIONID", sessionId);
            //roamingSettings.Values.Remove("JSESSIONID");
        }

        private async Task GetSchedule(Semester semester)
        {
            var coursesRequest = await NPAPI.GetCourses(searchForIdTextBox.Text, semester.Year, semester.SemesterNumber);

            if (coursesRequest.Success)
            {
                //Fill scheduleGrid
                FillCoursesIntoGrid(coursesRequest.Data);

                //Update result label
                searchResultLabelTextBlock.Text = name + " " + semester;

                //Show searchAppBarToggleButton
                //searchAppBarToggleButton.Visibility = Visibility.Visible;

                //Save to roaming settings
                var coursesJson = JsonConvert.SerializeObject(coursesRequest.Data);
                var semesterJson = JsonConvert.SerializeObject(semester);
                PutRoamingSetting("courses", coursesJson.ToString());
                PutRoamingSetting("semester", semesterJson.ToString());

                //Send GA Event
                bool searchSelf = roamingSettings.Values.ContainsKey("id") && roamingSettings.Values["id"] as string == searchForIdTextBox.Text;
                App.Current.GATracker.SendEvent("Get Curriculum", semester.ToString(), searchForIdTextBox.Text, searchSelf ? 0 : 1);
            }
            else
            {
                if (coursesRequest.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Send GA Event
                    App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                    //Try background login
                    var result = await NPAPI.BackgroundLogin();
                    if (result.Success)
                        await GetSchedule(semester);
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                    await new MessageDialog(coursesRequest.Message, "錯誤").ShowAsync();
            }
        }

        private void FillCoursesIntoGrid(List<Course> courses)
        {
            //Clear previous result
            scheduleGrid.Children.Clear();
            scheduleGrid.RowDefinitions.Clear();
            unscheduledCoursesGrid.Items.Clear();

            //Prepare scheduleGrid header columns
            var dayChars = "一二三四五六日";
            for (int d = 0; d < 7; d++)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = dayChars[d].ToString();
                textBlock.TextAlignment = TextAlignment.Center;
                Grid.SetRow(textBlock, 0);
                Grid.SetColumn(textBlock, d + 1);
                scheduleGrid.Children.Add(textBlock);
            }
            var timeChars = "123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (int t = 0; t < timeChars.Length; t++)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = timeChars[t].ToString();
                textBlock.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetRow(textBlock, t + 1);
                Grid.SetColumn(textBlock, 0);

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                scheduleGrid.RowDefinitions.Add(rowDefinition);

                scheduleGrid.Children.Add(textBlock);
            }

            int maxDay = 0;
            int maxTime = 0;

            foreach (Course course in courses)
            {
                if (course.Schedule.Count != 0)
                {
                    foreach (int day in course.Schedule.Keys)
                    {
                        maxDay = Math.Max(maxDay, day);

                        var times = course.Schedule[day];
                        foreach (int time in times)
                        {
                            maxTime = Math.Max(maxTime, time);

                            var border = GetCourseElement(course, time, day);

                            Grid.SetColumn(border, day + 1);
                            Grid.SetRow(border, time);
                            scheduleGrid.Children.Add(border);
                        }
                    }
                }
                else
                {
                    //show courses that dosne't have any schedule data
                    var border = GetCourseElement(course, -1);
                    unscheduledCoursesGrid.Items.Add(border);
                }
            }

            unscheduledCoursesTextBlock.Visibility = unscheduledCoursesGrid.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            //Determine should Saturday and Sunday should be shown
            saturdayColumnDefinition.Width = maxDay >= 5 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
            sundayColumnDifinition.Width = maxDay == 6 ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

            //Hide unnecessary rows
            for (int r = maxTime + 1; r < scheduleGrid.RowDefinitions.Count; r++)
                scheduleGrid.RowDefinitions[r].Height = new GridLength(0);
        }

        private FrameworkElement GetCourseElement(Course course, int time, int day = -1)
        {
            var textBlock = new TextBlock();

            textBlock.Text = course.Name;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.FontSize = 16;
            textBlock.VerticalAlignment = VerticalAlignment.Center;

            Border border = new Border();
            border.MinHeight = 50;
            border.Child = textBlock;
            border.Tapped += async (object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e) =>
            {

                string content = "";

                if (course.ClassRooms.Count > 0)
                {
                    foreach (string classRoom in course.ClassRooms)
                        content += classRoom + " ";
                    content += "\n";
                }

                if (course.Teachers.Count > 0)
                {
                    foreach (string teacher in course.Teachers)
                        content += teacher + " ";
                    content += "\n";
                }

                string timeString = Course.GetTimeString(time);

                if (timeString != null)
                    content += timeString + "\n";

                if (!string.IsNullOrWhiteSpace(course.Note))
                    content += course.Note;

                await new MessageDialog(content, course.Name).ShowAsync();

                //Send GA Event
                App.Current.GATracker.SendEvent("Other", "Tap on Course", null, 0);
            };

            Brush backColor;
            if (DateTime.Today.DayOfWeek == (DayOfWeek)(day + 1 % 7))
                backColor = new SolidColorBrush(Color.FromArgb(255, 209, 52, 56));
            else
                backColor = new SolidColorBrush(Color.FromArgb(128, 128, 128, 128));

            border.Background = backColor;
            border.Padding = new Thickness(5);
            border.Margin = new Thickness(1);

            return border;
        }

        private async Task SearchForId(string id)
        {
            //Disable user input
            searchForIdTextBox.IsEnabled = semesterComboBox.IsEnabled = getSemestersButton.IsEnabled = false;

            var semestersRequest = await NPAPI.GetSemesters(id);
            bool saveSearchId = true;
            if (semestersRequest.Success)
            {
                //Update label
                name = semestersRequest.Name;
                searchResultLabelTextBlock.Text = name;

                //Update comoboBox
                semesterComboBox.ItemsSource = semestersRequest.Semesters;
                if (semesterComboBox.Items.Count > 0)
                    semesterComboBox.SelectedIndex = 0;
                semesterComboBox.Visibility = semesterComboBox.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                //Save request to roaming settings
                PutRoamingSetting("name", name);
                var semestersJson = JsonConvert.SerializeObject(semestersRequest.Semesters);
                PutRoamingSetting("semesters", semestersJson.ToString());

                //Send GA Event
                bool searchSelf = roamingSettings.Values.ContainsKey("id") && roamingSettings.Values["id"] as string == id;
                App.Current.GATracker.SendEvent("Get Semesters", null, id, searchSelf ? 0 : 1);
            }
            else
            {
                if (semestersRequest.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Send GA Event
                    App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                    //Try background login
                    var result = await NPAPI.BackgroundLogin();
                    if (result.Success)
                        await SearchForId(id);
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                {
                    await new MessageDialog(semestersRequest.Message, "錯誤").ShowAsync();
                    saveSearchId = false;
                }

            }

            if (saveSearchId)
                PutRoamingSetting("searchId", id);

            //Enableuser input
            searchForIdTextBox.IsEnabled = semesterComboBox.IsEnabled = getSemestersButton.IsEnabled = true;

            semesterComboBox.Focus(FocusState.Programmatic);
        }

        private void PutRoamingSetting(string key, object value)
        {
            if (!roamingSettings.Values.ContainsKey(key))
                roamingSettings.Values.Add(key, value);
            else
                roamingSettings.Values[key] = value;
        }

        private async void semesterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            scheduleGrid.Children.Clear();
            searchResultLabelTextBlock.Text = name;
            if (semesterComboBox.SelectedItem is Semester)
            {
                //Disable user input
                searchForIdTextBox.IsEnabled = semesterComboBox.IsEnabled = getSemestersButton.IsEnabled = false;
                await GetSchedule(semesterComboBox.SelectedItem as Semester);
                //Enable user input
                searchForIdTextBox.IsEnabled = semesterComboBox.IsEnabled = getSemestersButton.IsEnabled = true;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("CurriculumPage");

            if (roamingSettings.Values.ContainsKey("searchId"))
                searchForIdTextBox.Text = roamingSettings.Values["searchId"].ToString();
            else if (roamingSettings.Values.ContainsKey("id"))
            {
                searchForIdTextBox.Text = roamingSettings.Values["id"].ToString();
                await SearchForId(roamingSettings.Values["id"].ToString());
            }

            if (roamingSettings.Values.ContainsKey("name"))
            {
                name = roamingSettings.Values["name"].ToString();
                if (roamingSettings.Values.ContainsKey("semester"))
                    searchResultLabelTextBlock.Text = name + " " + JsonConvert.DeserializeObject<Semester>(roamingSettings.Values["semester"].ToString());
                else
                    searchResultLabelTextBlock.Text = name;
            }

            if (roamingSettings.Values.ContainsKey("semesters"))
                semesterComboBox.ItemsSource = JsonConvert.DeserializeObject<List<Semester>>(roamingSettings.Values["semesters"].ToString());
            semesterComboBox.Visibility = semesterComboBox.Items.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (roamingSettings.Values.ContainsKey("courses"))
            {
                try
                {
                    var courses = JsonConvert.DeserializeObject<List<Course>>(roamingSettings.Values["courses"].ToString());
                    FillCoursesIntoGrid(courses);
                }
                catch(Exception exception)
                {
                    
                }
                
            }

            //if (!roamingSettings.Values.ContainsKey("JSESSIONID"))
            //    Frame.Navigate(typeof(LoginPage));

            //searchAppBarToggleButton.IsChecked = !roamingSettings.Values.ContainsKey("courses");
            //searchAppBarToggleButton.Visibility = roamingSettings.Values.ContainsKey("courses") ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void searchForIdTextBox_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                await SearchForId(searchForIdTextBox.Text);
            }
        }

        private async void getSemestersButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchForId(searchForIdTextBox.Text);
        }

        private void schoolEventScheduleButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SchedulePage));
        }
    }
}
