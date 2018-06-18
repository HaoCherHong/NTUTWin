using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

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
            var responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), useBig5Encoding);
            response.Dispose();
            return responseString;
        }

        public async Task<WriteableBitmap> RequestWritableBitmap(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            var response = await Request(url, method, parameters, headers);
            WriteableBitmap writableBitmap = await ConvertStreamToWritableBitmap(await response.Content.ReadAsStreamAsync());
            response.Dispose();
            return writableBitmap;
        }

        public async Task<HttpResponseMessage> Request(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            method = method.ToLower();
            if (method != "post" && method != "get")
                throw new ArgumentException("Unsuportted method");

            Debug.WriteLine(url);


            CookieContainer cookieContainer = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;

            HttpClient client = new HttpClient(handler);
            client.DefaultRequestHeaders.IfModifiedSince = new DateTimeOffset(new DateTime(1970, 1, 1));

            //Set headesr
            if (headers != null)
            {
                foreach (string key in headers.Keys)
                {
                    client.DefaultRequestHeaders.Add(key, headers[key].ToString());
                }
            }

            //Get roaming settings
            var roamingSettings = ApplicationData.Current.RoamingSettings;

            var sessionId = (string)roamingSettings.Values["JSESSIONID"];
            if (sessionId != null)
            {
                var uri = new Uri(url);
                cookieContainer.Add(new Uri(uri.AbsoluteUri.Replace(uri.AbsolutePath, "")), new Cookie("JSESSIONID", sessionId));
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

            //Start fetching response
            StringContent postContent = null;
            if (requestData != null)
                postContent = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await (method == "get" ? client.GetAsync(url) : client.PostAsync(url, postContent));

            IEnumerable<string> values;
            if (response.Headers.TryGetValues("Set-Cookie", out values))
            {
                var it = values.GetEnumerator();
                if (it.MoveNext())
                {
                    var setCookieHeader = it.Current;
                    var match = Regex.Match(setCookieHeader, "JSESSIONID=([a-zA-Z0-9-]+)");
                    if (match.Success)
                    {
                        sessionId = match.Groups[1].Value;
                        //Save to roaming settings
                        if (roamingSettings.Values.ContainsKey("JSESSIONID"))
                            roamingSettings.Values["JSESSIONID"] = sessionId;
                        else
                            roamingSettings.Values.Add("JSESSIONID", sessionId);

                        Debug.WriteLine("New JSESSIONID: " + sessionId);
                    }
                }
            }

            return response;
        }

        public async Task<string> RequestBig5String(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            return await RequestString(url, method, parameters, headers, true);
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
