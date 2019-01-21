using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using WebUtility = System.Net.WebUtility;

namespace NTUTWin
{
    internal class ConnectionHelper
    {
        // Big5 to Unicode mapping table
        private Dictionary<int, int> big5UnicodeMap;

        public string GetTimeStampString()
        {
            return ((ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds).ToString();
        }

        public async Task<string> RequestNPortal(string url, string method = "GET", Dictionary<string, object> parameters = null)
        {
            var responseString = await RequestBig5String(url, method, parameters);

            if (responseString.Contains("應用系統已中斷連線，請重新由入口網站主畫面左方的主選單，點選欲使用之系統!"))
                throw new NPAPI.SessionExpiredException();

            if (responseString.Contains("《尚未登錄入口網站》 或 《應用系統連線已逾時》"))
                throw new NPAPI.SessionExpiredException();

            return responseString;
        }

        public async Task<string> RequestString(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null, bool useBig5Encoding = false)
        {
            var response = await Request(url, method, parameters, headers);
            var inputStream = await response.Content.ReadAsInputStreamAsync();
            var responseString = await ConvertStreamToString(inputStream.AsStreamForRead(), useBig5Encoding);
            response.Dispose();
            return responseString;
        }

        public async Task<WriteableBitmap> RequestWritableBitmap(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            var response = await Request(url, method, parameters, headers);
            var inputStream = await response.Content.ReadAsInputStreamAsync();
            WriteableBitmap writableBitmap = await ConvertStreamToWritableBitmap(inputStream.AsStreamForRead());
            response.Dispose();
            return writableBitmap;
        }

        public async Task<HttpResponseMessage> Request(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            url = url.Replace("http://", "https://");

            method = method.ToLower();
            if (method != "post" && method != "get")
                throw new ArgumentException("Unsuportted method");

            var uri = new Uri(url);

            Debug.WriteLine(url);

            var filter = new HttpBaseProtocolFilter();
            filter.AllowAutoRedirect = false;

            SetCookieFromRoamingSettings(filter, "JSESSIONID", uri.Host, "/");
            SetCookieFromRoamingSettings(filter, "cookiesession1", uri.Host, "/");
            SetCookieFromRoamingSettings(filter, "UqZBpD3n", uri.Host, "/");

            var client = new HttpClient(filter);

            client.DefaultRequestHeaders.IfModifiedSince = new DateTimeOffset(new DateTime(1970, 1, 1));


            //Set headesr
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    client.DefaultRequestHeaders.Add(key, headers[key].ToString());
                }
            }

            string requestData = null;
            if (parameters != null)
            {
                var enumerator = parameters.Keys.GetEnumerator();

                foreach (string name in parameters.Keys)
                {
                    object value = parameters[name];
                    if (value is Array)
                    {
                        var array = value as Array;

                        foreach (object arrayChild in array)
                        {
                            if (requestData == null)
                                requestData = WebUtility.UrlEncode(name + "[]") + "=" + WebUtility.UrlEncode(arrayChild.ToString());
                            else
                                requestData += "&" + WebUtility.UrlEncode(name + "[]") + "=" + WebUtility.UrlEncode(arrayChild.ToString());
                        }
                    }
                    else
                    {
                        if (requestData == null)
                            requestData = WebUtility.UrlEncode(name) + "=" + WebUtility.UrlEncode(value.ToString());
                        else
                            requestData += "&" + WebUtility.UrlEncode(name) + "=" + WebUtility.UrlEncode(value.ToString());
                    }
                }
            }

            HttpStringContent postContent = null;
            if (requestData != null)
                postContent = new HttpStringContent(requestData, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/x-www-form-urlencoded");
            var cookies = filter.CookieManager.GetCookies(uri);

            var response = await (method == "get" ? client.GetAsync(uri, HttpCompletionOption.ResponseContentRead) : client.PostAsync(uri, postContent));

            string headerValue;
            if (response.Headers.TryGetValue("Set-Cookie", out headerValue))
            {
                var setCookieHeader = headerValue;
                SetCookieToRoamingSettings("JSESSIONID", setCookieHeader);
                SetCookieToRoamingSettings("cookiesession1", setCookieHeader);
                SetCookieToRoamingSettings("UqZBpD3n", setCookieHeader);
            }


            if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.MovedPermanently)
                return await Request(response.Headers.Location.ToString());

            return response;
        }

        public async Task<string> RequestBig5String(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            return await RequestString(url, method, parameters, headers, true);
        }

        private void SetCookieFromRoamingSettings(HttpBaseProtocolFilter filter, string name, string host, string path)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;

            var value = (string)roamingSettings.Values[name];
            if (value != null)
            {
                var cookie = new HttpCookie(name, host, path);
                cookie.Value = value;
                filter.CookieManager.SetCookie(cookie);
            }
        }

        private void SetCookieToRoamingSettings(string name, string setCookieHeader)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;

            var match = Regex.Match(setCookieHeader, name + "=([a-zA-Z0-9-]+)");
            if (match.Success)
            {
                var value = match.Groups[1].Value;
                if (roamingSettings.Values.ContainsKey(name))
                    roamingSettings.Values[name] = value;
                else
                    roamingSettings.Values.Add(name, value);

                Debug.WriteLine("New " + name + ": " + value);
            }
        }

        private async Task<string> ConvertStreamToString(Stream stream, bool useBig5Encoding = false)
        {
            if (useBig5Encoding)
                return (await Big5ToUnicode(stream)).ToString();
            else
            {
                StreamReader reader = new StreamReader(stream);
                string result = reader.ReadToEnd();
                stream.Dispose();
                return result;
            }
        }

        private async Task<StringBuilder> Big5ToUnicode(Stream s)
        {
            if (big5UnicodeMap == null)
                big5UnicodeMap = await CreateBig5ToUnicodeDictionary();

            StringBuilder lSB = new StringBuilder();
            byte[] big5Buffer = new byte[2];
            int input;
            while ((input = s.ReadByte()) != -1)
            {
                if (input > 0x81 && big5Buffer[0] == 0)
                {
                    big5Buffer[0] = (byte)input;
                }
                else if (big5Buffer[0] != 0)
                {
                    big5Buffer[1] = (byte)input;
                    int Big5Char = (big5Buffer[0] << 8) + big5Buffer[1];
                    try
                    {
                        int UTF8Char = big5UnicodeMap[Big5Char];
                        lSB.Append((char)UTF8Char);
                    }
                    catch (Exception)
                    {
                        lSB.Append((char)big5UnicodeMap[0xA148]);
                    }

                    big5Buffer = new byte[2];
                }
                else
                {
                    lSB.Append((char)input);
                }
            }
            s.Dispose();
            return lSB;
        }

        private async Task<Dictionary<int, int>> CreateBig5ToUnicodeDictionary()
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/big5.txt", UriKind.Absolute));
            Stream fileStream = await file.OpenStreamForReadAsync();
            StreamReader sr = new StreamReader(fileStream);
            string line;
            var dictionary = new Dictionary<int, int>();
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("#")) continue;
                string[] lTokens = line.Split(new char[] { '\t' });
                dictionary.Add(HexToInt(lTokens[0].Substring(2)), HexToInt(lTokens[1].Substring(2)));
            }

            return dictionary;
        }

        private int HexToInt(string hexString)
        {
            return int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
        }

        private async Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream)
        {
            var randomAccessStream = new InMemoryRandomAccessStream();
            var outputStream = randomAccessStream.GetOutputStreamAt(0);
            var dw = new DataWriter(outputStream);
            var task = Task.Factory.StartNew(() => dw.WriteBytes(memoryStream.ToArray()));
            await task;
            await dw.StoreAsync();
            await outputStream.FlushAsync();
            return randomAccessStream;
        }

        private async Task<BitmapImage> ConvertStreamToBitmapImage(Stream stream)
        {
            BitmapImage bitmapImage = new BitmapImage();
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            IRandomAccessStream a1 = await ConvertToRandomAccessStream(ms);
            await bitmapImage.SetSourceAsync(a1);
            stream.Dispose();
            return bitmapImage;
        }

        private async Task<WriteableBitmap> ConvertStreamToWritableBitmap(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            IRandomAccessStream a1 = await ConvertToRandomAccessStream(ms);
            BitmapImage bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(a1);

            WriteableBitmap writableBitmap = new WriteableBitmap(bitmapImage.PixelWidth, bitmapImage.PixelHeight);
            await writableBitmap.SetSourceAsync(a1.CloneStream());
            return writableBitmap;
        }
    }
}
