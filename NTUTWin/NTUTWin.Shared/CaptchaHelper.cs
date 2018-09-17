using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using WindowsPreview.Media.Ocr;

namespace NTUTWin
{
    internal class CaptchaHelper
    {
        private ConnectionHelper connectionHelper;
        private OcrEngine ocrEngine = new OcrEngine(OcrLanguage.English);

        public CaptchaHelper(ConnectionHelper connectionHelper)
        {
            this.connectionHelper = connectionHelper;
        }

        public async Task<string> GetCapchaText()
        {
            string captchaText;
            do
            {
                var captchaImage = await GetCaptchaImage();
                captchaText = await RecognizeBitmapToString(captchaImage);
            } while (string.IsNullOrEmpty(captchaText) || captchaText.Length != 4);

            return captchaText;
        }

        private async Task<WriteableBitmap> GetCaptchaImage()
        {
            //Check if we have JSESSIONID
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (!roamingSettings.Values.ContainsKey("JSESSIONID"))
                await connectionHelper.Request("https://nportal.ntut.edu.tw/", "GET");

            var captchaImage = await connectionHelper.RequestWritableBitmap("https://nportal.ntut.edu.tw/authImage.do", "GET");

            return GetClearImage(captchaImage);
        }

        private WriteableBitmap GetClearImage(WriteableBitmap source)
        {
            //Leave only white pixels
            var bytes = source.ToByteArray();
            for (var i = 0; i < bytes.Length; i += 4)
                if (!(bytes[i] == 255 && bytes[i + 1] == 255 && bytes[i + 2] == 255))
                    bytes[i] = bytes[i + 1] = bytes[i + 2] = 0;

            //Resize to recognizable size
            return new WriteableBitmap(source.PixelWidth, source.PixelHeight).FromByteArray(bytes).Resize(300, 100, WriteableBitmapExtensions.Interpolation.Bilinear);
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

        private async Task<string> RecognizeBitmapToString(WriteableBitmap target)
        {
            OcrResult data = await ocrEngine.RecognizeAsync((uint)target.PixelHeight, (uint)target.PixelWidth, ConvertBitmapToByteArray(target));
            string result = "";
            if (data.Lines != null)
                foreach (OcrLine item in data.Lines)
                    foreach (OcrWord inneritem in item.Words)
                        result += inneritem.Text;
            result = result.ToLower().Replace('1', 'l');
            return result;
        }
    }
}
