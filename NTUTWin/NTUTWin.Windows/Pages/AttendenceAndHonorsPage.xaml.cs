using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AttendenceAndHonorsPage : Page
    {
        List<UIElement> honorsGridHeaders;
        List<UIElement> attendenceGridHeaders;

#if DEBUG && DEBUG_DOC
        private int skip = 0;
#endif

        public AttendenceAndHonorsPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("AttendenceAndHonorsPage");

            //Save grid headers
            honorsGridHeaders = new List<UIElement>(honorsGrid.Children);
            attendenceGridHeaders = new List<UIElement>(attendenceGrid.Children);
            honorsGrid.Children.Clear();
            attendenceGrid.Children.Clear();
            await GetAttendenceAndHonors();
        }

        private async Task GetAttendenceAndHonors()
        {
            semestersComboBox.ItemsSource = null;
            semestersComboBox.Items.Clear();
            semestersComboBox.Items.Add("讀取中...");
            semestersComboBox.SelectedIndex = 0;
            try
            {
                var result = await NPAPI.GetAttendenceAndHonors();
                semestersComboBox.ItemsSource = result.Semesters;
                if (semestersComboBox.Items.Count > 0)
                    semestersComboBox.SelectedIndex = 0;
            }
            catch(NPAPI.SessionExpiredException)
            {
                //Send GA Event
                App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                //Try background login
                try
                {
                    await NPAPI.BackgroundLogin();
                }
                catch
                {
                    Frame.Navigate(typeof(LoginPage));
                }
            }
            catch(Exception e)
            {
                semestersComboBox.ItemsSource = null;
                semestersComboBox.Items.Clear();
                semestersComboBox.Items.Add(e.Message);
                semestersComboBox.SelectedIndex = 0;
            }
        }

        private void semestersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(semestersComboBox.SelectedItem is AttendenceAndHonors.Semester))
                return;
            var semester = semestersComboBox.SelectedItem as AttendenceAndHonors.Semester;

            if (semester.HonorsStatistics.Count > 0)
            {
                string honorsText = "";
                foreach (string key in semester.HonorsStatistics.Keys)
                    honorsText += key + ":\t" + semester.HonorsStatistics[key] + "\n";
                honorsTextBlock.Text = honorsText.TrimEnd();
            }
            else
                honorsTextBlock.Text = "無";

            honorsGrid.Children.Clear();
            honorsGrid.RowDefinitions.Clear();

            if (semester.HonorDetails.Count > 0)
            {

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                honorsGrid.RowDefinitions.Add(rowDefinition);

                foreach (UIElement header in honorsGridHeaders)
                    honorsGrid.Children.Add(header);
            }

            for (int i = 0, count = semester.HonorDetails.Count; i < count; i++)
            {
                var honor = semester.HonorDetails[i];

                TextBlock timeTextBlock = new TextBlock();
                TextBlock typeTextBlock = new TextBlock();
                TextBlock countTextBlock = new TextBlock();
                TextBlock detailTextBlock = new TextBlock();

                timeTextBlock.Text = honor.Time;
                typeTextBlock.Text = honor.Type;
                countTextBlock.Text = honor.Count.ToString();
                detailTextBlock.Text = honor.Detail;

                timeTextBlock.FontSize = typeTextBlock.FontSize = countTextBlock.FontSize = detailTextBlock.FontSize = 16;
                typeTextBlock.Margin = countTextBlock.Margin = new Thickness(10);
                timeTextBlock.Margin = new Thickness(0, 10, 10, 10);
                detailTextBlock.Margin = new Thickness(10, 10, 0, 10);

                Grid.SetColumn(timeTextBlock, 0);
                Grid.SetColumn(typeTextBlock, 1);
                Grid.SetColumn(countTextBlock, 2);
                Grid.SetColumn(detailTextBlock, 3);

                Grid.SetRow(timeTextBlock, i + 1);
                Grid.SetRow(typeTextBlock, i + 1);
                Grid.SetRow(countTextBlock, i + 1);
                Grid.SetRow(detailTextBlock, i + 1);

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                honorsGrid.RowDefinitions.Add(rowDefinition);

                honorsGrid.Children.Add(timeTextBlock);
                honorsGrid.Children.Add(typeTextBlock);
                honorsGrid.Children.Add(countTextBlock);
                honorsGrid.Children.Add(detailTextBlock);
            }

            if (semester.AttendenceStatistics.Count > 0)
            {
                string attendenceText = "";
                foreach (string key in semester.AttendenceStatistics.Keys)
                    attendenceText += key + ":\t" + semester.AttendenceStatistics[key] + "\n";
                attendenceTextBlock.Text = attendenceText.TrimEnd();
            }
            else
                attendenceTextBlock.Text = "無";

            attendenceGrid.Children.Clear();
            attendenceGrid.RowDefinitions.Clear();

            if (semester.AttendenceDetails.Count > 0)
            {
                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                attendenceGrid.RowDefinitions.Add(rowDefinition);

                foreach (UIElement header in attendenceGridHeaders)
                    attendenceGrid.Children.Add(header);
            }

            for (int i = 0, count = semester.AttendenceDetails.Count; i < count; i++)
            {
                var attendence = semester.AttendenceDetails[i];

                TextBlock timeTextBlock = new TextBlock();
                TextBlock weekTextBlock = new TextBlock();
                TextBlock sessionTextBlock = new TextBlock();
                TextBlock rollcallSheetNumberTextBlock = new TextBlock();
                TextBlock typeTextBlock = new TextBlock();
                TextBlock detailTextBlock = new TextBlock();

                timeTextBlock.Text = attendence.Time;
                weekTextBlock.Text = attendence.Week.ToString();
                sessionTextBlock.Text = attendence.Session;
                rollcallSheetNumberTextBlock.Text = attendence.RollcallSheetNumber;
                typeTextBlock.Text = attendence.Type;
                detailTextBlock.Text = attendence.Detail;

                timeTextBlock.FontSize = weekTextBlock.FontSize = sessionTextBlock.FontSize = rollcallSheetNumberTextBlock.FontSize = typeTextBlock.FontSize = detailTextBlock.FontSize = 16;
                weekTextBlock.Margin = sessionTextBlock.Margin = rollcallSheetNumberTextBlock.Margin = typeTextBlock.Margin = new Thickness(10);
                timeTextBlock.Margin = new Thickness(0, 10, 10, 10);
                detailTextBlock.Margin = new Thickness(10, 10, 0, 10);

                Grid.SetColumn(timeTextBlock, 0);
                Grid.SetColumn(weekTextBlock, 1);
                Grid.SetColumn(sessionTextBlock, 2);
                Grid.SetColumn(rollcallSheetNumberTextBlock, 3);
                Grid.SetColumn(typeTextBlock, 4);
                Grid.SetColumn(detailTextBlock, 5);

                Grid.SetRow(timeTextBlock, i + 1);
                Grid.SetRow(weekTextBlock, i + 1);
                Grid.SetRow(sessionTextBlock, i + 1);
                Grid.SetRow(rollcallSheetNumberTextBlock, i + 1);
                Grid.SetRow(typeTextBlock, i + 1);
                Grid.SetRow(detailTextBlock, i + 1);

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                attendenceGrid.RowDefinitions.Add(rowDefinition);

                attendenceGrid.Children.Add(timeTextBlock);
                attendenceGrid.Children.Add(weekTextBlock);
                attendenceGrid.Children.Add(sessionTextBlock);
                attendenceGrid.Children.Add(rollcallSheetNumberTextBlock);
                attendenceGrid.Children.Add(typeTextBlock);
                attendenceGrid.Children.Add(detailTextBlock);
            }
        }
    }
}
