using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using WindowsPreview.Media.Ocr;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private OcrEngine ocrEngine = new OcrEngine(OcrLanguage.English);

        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Login()
        {
            //Send GA Event
            App.Current.GATracker.SendEvent("Session", "Attempt Login", null, 0);

            string id = idTextBox.Text, password = passwordTextBox.Password;

            passwordTextBox.IsEnabled = loginAppBarButton.IsEnabled = false;
            idTextBox.IsReadOnly = true;

            //await progressbar.ShowAsync();

            var loginResult = await NPAPI.LoginNPortal(id, password);

            //await progressbar.HideAsync();

            if (loginResult.Success)
            {
                //Store logged id, password
                var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

                if (roamingSettings.Values.ContainsKey("id"))
                    roamingSettings.Values["id"] = id;
                else
                    roamingSettings.Values.Add("id", id);

                if (roamingSettings.Values.ContainsKey("password"))
                    roamingSettings.Values["password"] = password;
                else
                    roamingSettings.Values.Add("password", password);

                //Login Aps

                await NPAPI.LoginAps();

                //Go to previous page
                if (Frame.CanGoBack)
                    Frame.GoBack();
            }
            else
            {
                await new MessageDialog(loginResult.Message).ShowAsync();
            }

            passwordTextBox.IsEnabled = loginAppBarButton.IsEnabled = true;
            idTextBox.IsReadOnly = false;
        }

        
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("LoginPage");

            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            idTextBox.Text = roamingSettings.Values.ContainsKey("id") ? (string)roamingSettings.Values["id"] : "";
            passwordTextBox.Password = roamingSettings.Values.ContainsKey("password") ? (string)roamingSettings.Values["password"] : "";
        }

        private void loginAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void inputs_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender == idTextBox)
                    passwordTextBox.Focus(FocusState.Programmatic);
                else if (sender == passwordTextBox)
                    Login();
            }
        }
    }
}
