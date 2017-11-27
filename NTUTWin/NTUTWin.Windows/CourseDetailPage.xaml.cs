using System;
using System.Collections.Generic;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CourseDetailPage : Page
    {
        public CourseDetailPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string)
                await GetCourseDetail(e.Parameter as string);
            else if (e.Parameter is CourseDetail)
                ApplyCourseDetail(e.Parameter as CourseDetail);
        }

        private async Task GetCourseDetail(string courseId)
        {
            var result = await NPAPI.GetCourseDetail(courseId);

            if (result.Success)
                ApplyCourseDetail(result.Data);
            else
            {
                if (result.Error == NPAPI.RequestResult.ErrorType.Unauthorized)
                {
                    //Send GA Event
                    App.Current.GATracker.SendEvent("Session", "Session Expired", null, 0);

                    //Try background login
                    var loginResult = await NPAPI.BackgroundLogin();
                    if (loginResult.Success)
                        await GetCourseDetail(courseId);
                    else
                        Frame.Navigate(typeof(LoginPage));
                }
                else
                    detailTextBlock.Text = result.Message;
            }
        }

        private void ApplyCourseDetail(CourseDetail detail)
        {
            courseNameTextBlock.Text = detail.Name;
            string teachersText = "";
            string classesText = "";
            string classRoomsText = "";

            for (int i = 0, count = detail.Teachers.Count; i < count; i++)
                teachersText += detail.Teachers[i] + (i == count - 1 ? "" : "、");

            for (int i = 0, count = detail.Classes.Count; i < count; i++)
                classesText += detail.Classes[i] + (i == count - 1 ? "" : "、");

            for (int i = 0, count = detail.ClassRooms.Count; i < count; i++)
                classRoomsText += detail.ClassRooms[i] + (i == count - 1 ? "" : "、");

            var detailText = string.Format(
                "課號:\t\t{0}\n" +
                "名稱:\t\t{1}\n" +
                "學分:\t\t{2}\n" +
                "時數:\t\t{3}\n" +
                "類型:\t\t{4}\n" +
                "授課教師:\t{5}\n" +
                "開課班級:\t{6}\n" +
                "上課教室:\t{7}\n" +
                "修課人數:\t{8}\n" +
                "徹選人數:\t{9}\n\n" +
                "{10}",
                detail.CourseId, detail.Name, detail.Credits, detail.Hours, detail.Type, teachersText, classesText, classRoomsText, detail.PeopleCount, detail.QuitPeopleCount, detail.Note);

            detailTextBlock.Text = detailText;

            for (int i = 0, count = detail.Students.Count; i < count; i++)
            {
                var student = detail.Students[i];
                var margin = new Thickness(10, 10, 10, 10);
                TextBlock classTextBlock = new TextBlock();
                TextBlock idTextBlock = new TextBlock();
                TextBlock nameTextBlock = new TextBlock();
                TextBlock englishNameTextBlock = new TextBlock();
                TextBlock classStatusTextBlock = new TextBlock();
                TextBlock schoolStatusTextBlock = new TextBlock();
                Button searchButton = new Button();
                classTextBlock.Margin = idTextBlock.Margin = nameTextBlock.Margin = englishNameTextBlock.Margin = classStatusTextBlock.Margin = schoolStatusTextBlock.Margin = margin;
                classTextBlock.FontSize = idTextBlock.FontSize = nameTextBlock.FontSize = englishNameTextBlock.FontSize = classStatusTextBlock.FontSize = schoolStatusTextBlock.FontSize = 15;

                classTextBlock.Text = student.Class;
                idTextBlock.Text = student.StudentId;
                nameTextBlock.Text = student.Name;
                englishNameTextBlock.Text = student.EnglishName;
                classStatusTextBlock.Text = student.ClassStatus;
                schoolStatusTextBlock.Text = student.SchoolStatus;
                searchButton.Content = "查詢課表";

                searchButton.Click += (sender, e) =>
                {
                    Frame.Navigate(typeof(CurriculumPage), student.StudentId);
                };

                Grid.SetColumn(classTextBlock, 0);
                Grid.SetColumn(idTextBlock, 1);
                Grid.SetColumn(nameTextBlock, 2);
                Grid.SetColumn(englishNameTextBlock, 3);
                Grid.SetColumn(classStatusTextBlock, 4);
                Grid.SetColumn(schoolStatusTextBlock, 5);
                Grid.SetColumn(searchButton, 6);
                Grid.SetRow(classTextBlock, i);
                Grid.SetRow(idTextBlock, i);
                Grid.SetRow(nameTextBlock, i);
                Grid.SetRow(englishNameTextBlock, i);
                Grid.SetRow(classStatusTextBlock, i);
                Grid.SetRow(schoolStatusTextBlock, i);
                Grid.SetRow(searchButton, i);

                studentsGrid.Children.Add(classTextBlock);
                studentsGrid.Children.Add(idTextBlock);
                studentsGrid.Children.Add(nameTextBlock);
                studentsGrid.Children.Add(englishNameTextBlock);
                studentsGrid.Children.Add(classStatusTextBlock);
                studentsGrid.Children.Add(schoolStatusTextBlock);
                studentsGrid.Children.Add(searchButton);

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = GridLength.Auto;
                studentsGrid.RowDefinitions.Add(rowDefinition);
            }
        }
    }
}
