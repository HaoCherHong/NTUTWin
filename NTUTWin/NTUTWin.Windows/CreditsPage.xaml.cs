using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreditsPage : Page
    {

        List<UIElement> creditsHeaders;

        public CreditsPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("CreditsPage");

            //save credits headers
            creditsHeaders = new List<UIElement>(creditsGrid.Children);
            creditsGrid.Children.Clear();

            //Add loading text to combobox
            semestersComboBox.Items.Add("讀取中...");
            semestersComboBox.SelectedIndex = 0;

            await GetCredits();
        }

        private async Task GetCredits()
        {
            var result = await NPAPI.GetCredits();
            if (result.Success)
                ApplyCredits(result.Data);
            else
            {
                if (result.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Send GA Event
                    App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                    //Try background login
                    var loginResult = await NPAPI.BackgroundLogin();
                    if (loginResult.Success)
                        await GetCredits();
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                    summaryTextBlock.Text = result.Message;
            }
        }

        private void ApplyCredits(Credits credits)
        {
            //Fill summary
            summaryTextBlock.Text = string.Format("總實得學分:\t{0}\n\n", credits.TotalCreditsGot);

            foreach(string key in credits.TotalTypeCredits.Keys)
                summaryTextBlock.Text += string.Format("{0}修:\t\t{1}\n", key, credits.TotalTypeCredits[key]);

            summaryTextBlock.Text += "\n";

            foreach (string key in credits.TotalDetailTypeCredits.Keys)
                summaryTextBlock.Text += string.Format("{0}:\t{1}\n", key, credits.TotalDetailTypeCredits[key]);

            //Setup combobox
            semestersComboBox.ItemsSource = credits.Semesters;
            if (credits.Semesters.Count > 0)
                semestersComboBox.SelectedIndex = 0;
            else
            {
                semestersComboBox.ItemsSource = null;
                semestersComboBox.Items.Clear();
                semestersComboBox.Items.Add("查無資料");
                semestersComboBox.SelectedIndex = 0;
            }
        }

        private void ApplySemester(Credits.Semester semester)
        {
            //Fill summary
            semesterSummaryTextBlock.Text = string.Format(
                "總平均:\t\t{0}\n" +
                "操行成績:\t{1}\n" +
                "修習總學分數:\t{2}\n" +
                "實得學分數:\t{3}",
                semester.TotalAverage,
                semester.ConductGrade,
                semester.CreditsWanted,
                semester.CreditsGot);

            //Fill credits grid
            creditsGrid.Children.Clear();
            creditsGrid.RowDefinitions.Clear();

            //Add headers
            foreach (UIElement header in creditsHeaders)
                creditsGrid.Children.Add(header);

            RowDefinition rowDefinition = new RowDefinition();
            rowDefinition.Height = GridLength.Auto;
            creditsGrid.RowDefinitions.Add(rowDefinition);

            for (int i = 0; i < semester.Credits.Count; i++)
            {
                var credit = semester.Credits[i];

                TextBlock courseIdTextBlock = new TextBlock();
                TextBlock typeTextBlock = new TextBlock();
                TextBlock nameTextBlock = new TextBlock();
                TextBlock creditsTextBlock = new TextBlock();
                TextBlock gradeTextBlock = new TextBlock();
                TextBlock noteTextBlock = new TextBlock();
                Button courseDetailButton = new Button();

                courseIdTextBlock.Text = credit.CourseId;
                typeTextBlock.Text = credit.Type;
                nameTextBlock.Text = credit.Name;
                creditsTextBlock.Text = credit.Credits.ToString();
                gradeTextBlock.Text = credit.Grade.ToString();
                noteTextBlock.Text = credit.Note;
                courseDetailButton.Content = "課程資料";

                courseDetailButton.Click += (sender, e) => {
                    Frame.Navigate(typeof(CourseDetailPage), credit.Detail);
                };

                typeTextBlock.Margin = nameTextBlock.Margin = creditsTextBlock.Margin = gradeTextBlock.Margin = noteTextBlock.Margin = new Thickness(10);
                courseIdTextBlock.Margin = new Thickness(0, 10, 10, 10);
                courseIdTextBlock.FontSize = typeTextBlock.FontSize = nameTextBlock.FontSize = creditsTextBlock.FontSize = gradeTextBlock.FontSize = noteTextBlock.FontSize = 16;

                if(credit.Grade < 60)
                    courseIdTextBlock.Opacity = typeTextBlock.Opacity = nameTextBlock.Opacity = creditsTextBlock.Opacity = gradeTextBlock.Opacity = noteTextBlock.Opacity = 0.5;

                Grid.SetColumn(courseIdTextBlock, 0);
                Grid.SetColumn(typeTextBlock, 1);
                Grid.SetColumn(nameTextBlock, 2);
                Grid.SetColumn(creditsTextBlock, 3);
                Grid.SetColumn(gradeTextBlock, 4);
                Grid.SetColumn(noteTextBlock, 5);
                Grid.SetColumn(courseDetailButton, 6);

                Grid.SetRow(courseIdTextBlock, i + 1);
                Grid.SetRow(typeTextBlock, i + 1);
                Grid.SetRow(nameTextBlock, i + 1);
                Grid.SetRow(creditsTextBlock, i + 1);
                Grid.SetRow(gradeTextBlock, i + 1);
                Grid.SetRow(noteTextBlock, i + 1);
                Grid.SetRow(courseDetailButton, i + 1);

                rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                creditsGrid.RowDefinitions.Add(rowDefinition);

                creditsGrid.Children.Add(courseIdTextBlock);
                creditsGrid.Children.Add(typeTextBlock);
                creditsGrid.Children.Add(nameTextBlock);
                creditsGrid.Children.Add(creditsTextBlock);
                creditsGrid.Children.Add(gradeTextBlock);
                creditsGrid.Children.Add(noteTextBlock);
                creditsGrid.Children.Add(courseDetailButton);
            }
        }

        private void semestersComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(semestersComboBox.SelectedItem is Credits.Semester))
                return;
            var semester = semestersComboBox.SelectedItem as Credits.Semester;
            ApplySemester(semester);
        }
    }
}
