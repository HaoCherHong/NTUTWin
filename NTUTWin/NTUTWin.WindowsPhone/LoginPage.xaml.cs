using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using WindowsPreview.Media.Ocr;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace NTUTWin
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {

        private StatusBarProgressIndicator progressbar = StatusBar.GetForCurrentView().ProgressIndicator;
        private OcrEngine ocrEngine = new OcrEngine(OcrLanguage.English);

        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void Login()
        {
            //Send GA Event
            App.Current.GATracker.SendEvent("Session", "Attempt Login", null, 0);

            string id = idTextBox.Text, password = passwordTextBox.Password;

            passwordTextBox.IsEnabled = loginAppBarButton.IsEnabled = false;
            idTextBox.IsReadOnly = captchaTextBox.IsReadOnly = true;

            await progressbar.ShowAsync();

            var loginResult = await NPAPI.LoginNPortal(id, password, captchaTextBox.Text);

            await progressbar.HideAsync();

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
                UpdateCapchaImage();
                //captchaTextBox.Text = "";
            }

            passwordTextBox.IsEnabled = loginAppBarButton.IsEnabled = true;
            idTextBox.IsReadOnly = captchaTextBox.IsReadOnly = false;
        }

        private async void UpdateCapchaImage()
        {
            captchaImage.Opacity = 0.5;
            var captchaBitmapImage = await NPAPI.GetCaptchaImage();
            var clearImage = GetClearImage(captchaBitmapImage);
            
            captchaImage.Source = clearImage;

            captchaTextBox.Text = await GetCaptchaText(clearImage);

            if(string.IsNullOrEmpty(captchaTextBox.Text))
            {
                UpdateCapchaImage();
                return;
            }
            captchaImage.Opacity = 1;
        }

        private WriteableBitmap GetClearImage(WriteableBitmap source)
        {
            //Leave only white pixels
            var bytes = source.ToByteArray();
            for (var i = 0; i < bytes.Length; i += 4)
                if (!(bytes[i] == 255 && bytes[i + 1] == 255 && bytes[i + 2] == 255))
                    bytes[i] = bytes[i + 1] = bytes[i + 2] = 0;

            //Resize to recognizable size
            return new WriteableBitmap(90, 30).FromByteArray(bytes).Resize(300, 100, WriteableBitmapExtensions.Interpolation.Bilinear);
        }

        async Task<string> GetCaptchaText(WriteableBitmap target)
        {
            OcrResult data = await ocrEngine.RecognizeAsync((uint)target.PixelHeight, (uint)target.PixelWidth, ConvertBitmapToByteArray(target));
            string result = "";
            if(data.Lines != null)
                foreach (OcrLine item in data.Lines)
                    foreach (OcrWord inneritem in item.Words)
                        result += inneritem.Text;
            result = result.ToLower();
            return result;
        }

        private byte[] ConvertBitmapToByteArray(WriteableBitmap bitmap)
        {
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //Send GA View
            App.Current.GATracker.SendView("LoginPage");

            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            idTextBox.Text = roamingSettings.Values.ContainsKey("id") ? (string)roamingSettings.Values["id"] : "";
            passwordTextBox.Password = roamingSettings.Values.ContainsKey("password") ? (string)roamingSettings.Values["password"] : "";
            UpdateCapchaImage();
        }

        private void loginAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void inputs_KeyUp(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender == idTextBox)
                    passwordTextBox.Focus(FocusState.Programmatic);
                else if (sender == passwordTextBox)
                    captchaTextBox.Focus(FocusState.Programmatic);
                else if(sender == captchaTextBox)
                    Login();

            }
        }

        private void captchaGrid_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            UpdateCapchaImage();
        }
    }
}
