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
    public sealed partial class CreditsPage : Page
    {
        public CreditsPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("CreditsPage");

            await GetCredits();
        }

        private async Task GetCredits()
        {
            var result = await NPAPI.GetCredits();
            if (result.Success)
            {
                var credits = result.Data;
                semestersListView.ItemsSource = credits.Semesters;
            }
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
                {
                    await new MessageDialog(result.Message, "錯誤").ShowAsync();
                }
            }
        }
    }
}
