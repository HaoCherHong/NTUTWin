﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using WindowsPreview.Media.Ocr;

namespace NTUTWin
{
    class NPAPI
    {
        public class RequestResult
        {
            public enum ErrorType
            {
                None,
                Unauthorized,
                ParsingFailed,
                WrongIdPassword,
                WrongCaptcha,
                AccountLocked
            }

            public RequestResult(bool success, ErrorType error, string message)
            {
                Success = success;
                Error = error;
                Message = message;
            }

            public bool Success { get; private set; }
            public ErrorType Error { get; private set; }
            public string Message { get; private set; }
        }

        public class RequestResult<T> : RequestResult
        {
            public RequestResult(bool success, ErrorType error, string message, T data) : base(success, error, message)
            {
                Data = data;
            }

            public T Data { get; private set; }
        }

        public class GetSemestersResult : RequestResult
        {
            public GetSemestersResult(bool success, ErrorType error, string message, List<Semester> semesters, string name) : base(success, error, message)
            {
                Semesters = semesters;
                Name = name;
            }

            public List<Semester> Semesters { get; private set; }
            public string Name { get; private set; }
        }

        // Big5 to Unicode mapping table
        private static Dictionary<int, int> big5UnicodeMap;

        //OCR Engine
        private static OcrEngine ocrEngine = new OcrEngine(OcrLanguage.English);

        private static async Task<Dictionary<int, int>> CreateBig5ToUnicodeDictionary()
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
                dictionary.Add(hexToInt(lTokens[0].Substring(2)), hexToInt(lTokens[1].Substring(2)));
            }

            return dictionary;
        }

        private static int hexToInt(string hexString)
        {
            return int.Parse(hexString, System.Globalization.NumberStyles.HexNumber);
        }

        public static async Task<RequestResult> LoginNPortal(string id, string password)
        {
            string captchaText;
            do
            {
                var captchaImage = await GetCaptchaImage();
                captchaText = await GetCaptchaText(captchaImage);
            } while (string.IsNullOrEmpty(captchaText) || captchaText.Length != 4);

            var response = await Request("https://nportal.ntut.edu.tw/login.do", "POST", new Dictionary<string, object>()
            {
                {"muid", id },
                {"mpassword", password },
                {"authcode", captchaText }
            });

            string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
            response.Dispose();

            bool success = false;
            string message = null;
            RequestResult.ErrorType errorType = RequestResult.ErrorType.None;

            if (responseString.Contains("登入失敗"))
            {
                if (responseString.Contains("密碼錯誤"))
                {
                    errorType = RequestResult.ErrorType.WrongIdPassword;
                    message = "帳號密碼錯誤";
                }
                else if (responseString.Contains("驗證碼"))
                {
                    errorType = RequestResult.ErrorType.WrongCaptcha;
                    message = "驗證碼錯誤";

                    //Retry
                    return await LoginNPortal(id, password);
                }
                else if (responseString.Contains("帳號已被鎖住"))
                {
                    errorType = RequestResult.ErrorType.AccountLocked;
                    message = "嘗試錯誤太多次，帳號已被鎖定10分鐘";
                }
            }
            else
            {
                message = "登入成功";
                success = true;
            }

            return new RequestResult(success, errorType, message);
        }

        public static async Task<RequestResult> LogoutNPortal()
        {
            var response = await Request("https://nportal.ntut.edu.tw/logout.do", "GET");

            if(response.IsSuccessStatusCode)
            {
                var roamingSettings = ApplicationData.Current.RoamingSettings;
                //roamingSettings.Values.Remove("password");
                roamingSettings.Values.Remove("JSESSIONID");
            }

            return new RequestResult(response.IsSuccessStatusCode, RequestResult.ErrorType.None, null);
        }

        public static async Task LoginAps()
        {
            var response = await Request("http://nportal.ntut.edu.tw/ssoIndex.do?apUrl=http://aps.ntut.edu.tw/course/tw/courseSID.jsp&apOu=aa_0010&sso=true", "GET");
            string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
            response.Dispose();

            var matches = Regex.Matches(responseString, "<input type='hidden' name='([a-zA-Z]+)' value='([^']+)'>");

            var postData = new Dictionary<string, object>();
            foreach (Match match in matches)
                postData.Add(match.Groups[1].Value, match.Groups[2].Value);

            response = await Request("http://aps.ntut.edu.tw/course/tw/courseSID.jsp", "POST", postData);
            responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
            response.Dispose();

            Debug.WriteLine(responseString);
        }

        public static async Task<RequestResult> BackgroundLogin()
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            if(!roamingSettings.Values.ContainsKey("id") || !roamingSettings.Values.ContainsKey("password"))
                return new RequestResult(false, RequestResult.ErrorType.Unauthorized, null);

            string id = (string)roamingSettings.Values["id"], 
                password = (string)roamingSettings.Values["password"];

            RequestResult result = await LoginNPortal(id, password);

            if (result.Success)
                await LoginAps();

            return result;
        } 

        public static async Task<GetSemestersResult> GetSemesters(string id)
        {
            var response = await Request("http://aps.ntut.edu.tw/course/tw/Select.jsp", "POST", new Dictionary<string, object>()
            {
                {"code", id},
                {"format", -3}
            });
            string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
            response.Dispose();

            //Check if connection is expired
            if (responseString.Contains("《尚未登錄入口網站》 或 《應用系統連線已逾時》"))
                return new GetSemestersResult(false, RequestResult.ErrorType.Unauthorized, "連線逾時", null, null);

            //Get semesters
            var matches = Regex.Matches(responseString, "<a href=\"Select.jsp\\?format=-2&code=" + id + "&year=([0-9]+)&sem=([0-9]+)\">([^<]+)</a>");

            var semesters = new List<Semester>();
            foreach (Match match in matches)
                semesters.Add(new Semester(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value)));

            //Get student name
            var nameMatch = Regex.Match(responseString, "<tr><th>([^(]+)");
            string name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : null;


            if (matches.Count == 0)
            {
                var match = Regex.Match(responseString, "<font color=\"#FF0000\">([^<]+)</font>");
                if(match.Success)
                    return new GetSemestersResult(false, RequestResult.ErrorType.None, match.Groups[1].Value, null, null);
                else
                    return new GetSemestersResult(false, RequestResult.ErrorType.ParsingFailed, "查詢失敗", null, null);
            }

            return new GetSemestersResult(true, RequestResult.ErrorType.None, null, semesters, name);
        }

        public static async Task<RequestResult<List<Course>>> GetCourses(string id, int year, int semester)
        {
            var response = await Request(string.Format("http://aps.ntut.edu.tw/course/tw/Select.jsp?format=-2&code={0}&year={1}&sem={2}", id, year, semester), "GET");
            string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
            response.Dispose();

            if (responseString.Contains("《尚未登錄入口網站》 或 《應用系統連線已逾時》"))
                return new RequestResult<List<Course>>(false, RequestResult.ErrorType.Unauthorized, "連線逾時", null);

            try
            {
                var courses = Course.ParseFromDocument(responseString);
                return new RequestResult<List<Course>>(true, RequestResult.ErrorType.None, null, courses);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debugger.Break();
                return new RequestResult<List<Course>>(true, RequestResult.ErrorType.None, e.Message, null);
            }
        }

        public static async Task<RequestResult<Schedule>> GetSchedule()
        {
            var response = await Request("http://www.cc.ntut.edu.tw/~wwwoaa/oaa-nwww/oaa-cal/oaa-cal_099.html", "GET");
            string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), false);
            response.Dispose();

            Schedule schedule = Schedule.Parse(responseString);

            return new RequestResult<Schedule>(false, RequestResult.ErrorType.Unauthorized, null, schedule);
        }

        #region connection helper

        private static async Task<StringBuilder> Big5ToUnicode(Stream s)
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

        public static async Task<WriteableBitmap> GetCaptchaImage()
        {
            //Check if we have JSESSIONID
            var roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            if (!roamingSettings.Values.ContainsKey("JSESSIONID"))
                await Request("https://nportal.ntut.edu.tw/", "GET");

            var response = await Request("https://nportal.ntut.edu.tw/authImage.do", "GET");
            //BitmapImage captchaImage = await ConvertStreamToBitmapImage(await response.Content.ReadAsStreamAsync());
            WriteableBitmap captchaImage = await ConvertStreamToWritableBitmap(await response.Content.ReadAsStreamAsync());
            response.Dispose();

            return GetClearImage(captchaImage);
        }

        private static async Task<IRandomAccessStream> ConvertToRandomAccessStream(MemoryStream memoryStream)
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

        private static async Task<BitmapImage> ConvertStreamToBitmapImage(Stream stream)
        {
            BitmapImage bitmapImage = new BitmapImage();
            MemoryStream ms = new MemoryStream();
            stream.CopyTo(ms);
            IRandomAccessStream a1 = await ConvertToRandomAccessStream(ms);
            await bitmapImage.SetSourceAsync(a1);
            stream.Dispose();
            return bitmapImage;
        }

        private static async Task<WriteableBitmap> ConvertStreamToWritableBitmap(Stream stream)
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

        private static async Task<string> ConvertStreamToString(Stream stream, bool useBig5Encoding = false)
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

        private static async Task<HttpResponseMessage> Request(string url, string method, Dictionary<string, object> parameters = null)
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

            //Get roaming settings
            var roamingSettings = ApplicationData.Current.RoamingSettings;

            var sessionId = (string)roamingSettings.Values["JSESSIONID"];
            if (sessionId != null)
                cookieContainer.Add(new Uri(url), new Cookie("JSESSIONID", sessionId));

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
            if(requestData != null)
                postContent = new StringContent(requestData, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await (method == "get" ?  client.GetAsync(url) : client.PostAsync(url, postContent));

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

        #endregion

        #region captcha helper

        private static WriteableBitmap GetClearImage(WriteableBitmap source)
        {
            //Leave only white pixels
            var bytes = source.ToByteArray();
            for (var i = 0; i < bytes.Length; i += 4)
                if (!(bytes[i] == 255 && bytes[i + 1] == 255 && bytes[i + 2] == 255))
                    bytes[i] = bytes[i + 1] = bytes[i + 2] = 0;

            //Resize to recognizable size
            return new WriteableBitmap(source.PixelWidth, source.PixelHeight).FromByteArray(bytes).Resize(300, 100, WriteableBitmapExtensions.Interpolation.Bilinear);
        }

        private static byte[] ConvertBitmapToByteArray(WriteableBitmap bitmap)
        {
            using (Stream stream = bitmap.PixelBuffer.AsStream())
            using (MemoryStream memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private static async Task<string> GetCaptchaText(WriteableBitmap target)
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

        #endregion
    }
}