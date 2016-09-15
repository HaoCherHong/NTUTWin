using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DonatePage : Page
    {
        public DonatePage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("FeedbackPage");
        }

        private async void contactButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.facebook.com/HaoCherHong"));

            //Send GA Event
            App.Current.GATracker.SendEvent("Other", "Go to author Facebook", null, 0);
        }

        private async void rateAndReviewButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store:reviewapp?appid=5c805945-21cb-4160-9a45-1de3ec408a9d"));

            //Send GA Event
            App.Current.GATracker.SendEvent("Other", "Go Rating Page", null, 0);
        }

        private async void donateButton_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=XV4H7YAT3CAMQ"));

            //Send GA Event
            App.Current.GATracker.SendEvent("Other", "Go to PayPal donate page", null, 0);
        }
    }
}
