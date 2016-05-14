#define DEBUG_DOC

using System;
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
                AccountLocked,
                HttpError,
                Unknown
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

        public class LoginExcpetion : Exception
        {
            public RequestResult.ErrorType ErrorType { get; private set; }
            public LoginExcpetion(string message, RequestResult.ErrorType errorType):base(message)
            {
                this.ErrorType = errorType;
            }
        }

        // Big5 to Unicode mapping table
        private static Dictionary<int, int> big5UnicodeMap;

        //OCR Engine
        private static OcrEngine ocrEngine = new OcrEngine(OcrLanguage.English);

        //Timestamp
        private static string TimeStamp
        {
            get { return ((ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds).ToString(); }
        }

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
            try
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
                    {"authcode", captchaText },
                }, new Dictionary<string, object>()
                {
                    {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.112 Safari/537.36" }
                });
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
                response.Dispose();

                if (responseString.Contains("登入失敗"))
                {
                    if (responseString.Contains("密碼錯誤"))
                        throw new LoginExcpetion("帳號密碼錯誤", RequestResult.ErrorType.WrongIdPassword);
                    else if (responseString.Contains("驗證碼"))
                        return await LoginNPortal(id, password); //Retry
                    else if (responseString.Contains("帳號已被鎖住"))
                        throw new LoginExcpetion("嘗試錯誤太多次，帳號已被鎖定10分鐘", RequestResult.ErrorType.AccountLocked);
                }
                else if (string.IsNullOrWhiteSpace(responseString))
                    throw new LoginExcpetion("遇到不明的錯誤", RequestResult.ErrorType.Unknown);
                

                //The folling 2 request will make the server allowing us to login sub-systems

                response = await Request("http://nportal.ntut.edu.tw/myPortalHeader.do");
                //responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
                response.Dispose();
                response = await Request("http://nportal.ntut.edu.tw/aptreeBox.do");
                //responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
                response.Dispose();

                //Login aps
                await LoginSubSystem("http://nportal.ntut.edu.tw/ssoIndex.do?apUrl=http://aps.ntut.edu.tw/course/tw/courseSID.jsp&apOu=aa_0010-&sso=true&datetime1=" + TimeStamp);
                //Login aps-stu
                await LoginSubSystem("http://nportal.ntut.edu.tw/ssoIndex.do?apUrl=http://aps-stu.ntut.edu.tw/StuQuery/LoginSID.jsp&apOu=aa_003&sso=big5&datetime1=" + TimeStamp);

                return new RequestResult(true, RequestResult.ErrorType.None, "登入成功");
            }
            catch (LoginExcpetion e)
            {
                return new RequestResult(false, e.ErrorType, e.Message);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult(false, RequestResult.ErrorType.HttpError, e.Message);
            }
            catch (Exception e)
            {
                return new RequestResult(false, RequestResult.ErrorType.Unknown, e.Message);
            }
        }

        public static async Task<RequestResult> LogoutNPortal()
        {
            try
            {
                var response = await Request("https://nportal.ntut.edu.tw/logout.do", "GET");

                if (response.IsSuccessStatusCode)
                {
                    var roamingSettings = ApplicationData.Current.RoamingSettings;
                    roamingSettings.Values.Remove("JSESSIONID");
                    roamingSettings.Values.Remove("id");
                    roamingSettings.Values.Remove("password");
                }

                return new RequestResult(response.IsSuccessStatusCode, RequestResult.ErrorType.None, null);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult(false, RequestResult.ErrorType.HttpError, e.Message);
            }
            catch (Exception e)
            {
                return new RequestResult(false, RequestResult.ErrorType.Unknown, e.Message);
            }
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

        public static async Task LoginSubSystem(string url)
        {
            
            var response = await Request(url, "GET", null, new Dictionary<string, object> {
                {"Referer", "http://nportal.ntut.edu.tw/myPortal.do?thetime=" + TimeStamp + "_true" }
            });
            string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
            response.Dispose();

            var matches = Regex.Matches(responseString, "<input type='hidden' name='([a-zA-Z]+)' value='([^']+)'>");
            var target = Regex.Match(responseString, "action='([^']+)'").Groups[1].Value;

            var postData = new Dictionary<string, object>();
            foreach (Match match in matches)
                postData.Add(match.Groups[1].Value, match.Groups[2].Value);

            response = await Request(target, "POST", postData);
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

            if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
                return new RequestResult(false, RequestResult.ErrorType.Unauthorized, null);

            RequestResult result = await LoginNPortal(id, password);

            return result;
        } 

        public static async Task<GetSemestersResult> GetSemesters(string id)
        {
            try
            {
                var url = "http://aps.ntut.edu.tw/course/tw/Select.jsp";
                var parameters = new Dictionary<string, object>() {
                {"code", id},
                {"format", -3}
            };
                var response = await Request(url, "POST", parameters);
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
                response.Dispose();

                //Check if connection is expired
                if (responseString.Contains("《尚未登錄入口網站》 或 《應用系統連線已逾時》"))
                    return new GetSemestersResult(false, RequestResult.ErrorType.Unauthorized, "連線逾時", null, null);

                SendStat(url, "post", responseString, parameters);

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
                    if (match.Success)
                        return new GetSemestersResult(false, RequestResult.ErrorType.None, match.Groups[1].Value, null, null);
                    else
                        return new GetSemestersResult(false, RequestResult.ErrorType.ParsingFailed, "查詢失敗", null, null);
                }

                return new GetSemestersResult(true, RequestResult.ErrorType.None, null, semesters, name);
            }
            catch (HttpRequestException e)
            {
                return new GetSemestersResult(false, RequestResult.ErrorType.HttpError, e.Message, null, null);
            }
            catch (Exception e)
            {
                return new GetSemestersResult(false, RequestResult.ErrorType.Unknown, e.Message, null, null);
            }
        }

        public static async Task<RequestResult<List<Course>>> GetCourses(string id, int year, int semester)
        {
            try
            {
                var url = string.Format("http://aps.ntut.edu.tw/course/tw/Select.jsp?format=-2&code={0}&year={1}&sem={2}", id, year, semester);
                var response = await Request(url, "GET");
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
                response.Dispose();

                if (responseString.Contains("《尚未登錄入口網站》 或 《應用系統連線已逾時》"))
                    return new RequestResult<List<Course>>(false, RequestResult.ErrorType.Unauthorized, "連線逾時", null);

                SendStat(url, "get", responseString);
            
                var courses = Course.ParseFromDocument(responseString);
                return new RequestResult<List<Course>>(true, RequestResult.ErrorType.None, null, courses);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult<List<Course>>(false, RequestResult.ErrorType.HttpError, e.Message, null);
            }
            catch (Exception e)
            {
                return new RequestResult<List<Course>>(false, RequestResult.ErrorType.Unknown, e.Message, null);
            }
        }

        public static async Task<RequestResult<Schedule>> GetSchedule()
        {
            try
            {
                var response = await Request("http://www.cc.ntut.edu.tw/~wwwoaa/oaa-nwww/oaa-cal/oaa-cal_099.html", "GET");
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), false);
                response.Dispose();

                Schedule schedule = Schedule.Parse(responseString);

                return new RequestResult<Schedule>(true, RequestResult.ErrorType.None, null, schedule);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult<Schedule>(false, RequestResult.ErrorType.HttpError, e.Message, null);
            }
            catch (Exception e)
            {
                return new RequestResult<Schedule>(false, RequestResult.ErrorType.Unknown, e.Message, null);
            }
        }

#if DEBUG
        public static async Task<RequestResult<MidAlerts>> DebugMidAlerts(int skip) {
            var url = "https://lucher.club/api/ntutwin/doc?url=http://aps-stu.ntut.edu.tw/StuQuery/QrySCWarn.jsp&limit=1&format=html&skip=" + skip;
                var response = await Request(url, "GET");
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync());
                response.Dispose();

                var result = MidAlerts.Parse(responseString);
                skip++;

                return new RequestResult<MidAlerts>(true, RequestResult.ErrorType.None, null, result);
        }
#endif

        public static async Task<RequestResult<MidAlerts>> GetMidAlerts()
        {
            try
            {
                var url = "http://aps-stu.ntut.edu.tw/StuQuery/QrySCWarn.jsp";
                var response = await Request(url, "GET");
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
                response.Dispose();

                if (responseString.Contains("應用系統已中斷連線，請重新由入口網站主畫面左方的主選單，點選欲使用之系統!"))
                    return new RequestResult<MidAlerts>(false, RequestResult.ErrorType.Unauthorized, "連線逾時", null);

                SendStat(url, "get", responseString);

                var result = MidAlerts.Parse(responseString);

                return new RequestResult<MidAlerts>(true, RequestResult.ErrorType.None, null, result);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult<MidAlerts>(false, RequestResult.ErrorType.HttpError, e.Message, null);
            }
            catch (Exception e)
            {
                return new RequestResult<MidAlerts>(false, RequestResult.ErrorType.Unknown, e.Message, null);
            }
            
        }

        public static async Task<RequestResult> SendCreditStat()
        {
            try
            {
                var url = "http://aps-stu.ntut.edu.tw/StuQuery/QryScore.jsp";
                var parameters = new Dictionary<string, object>() { {"format",-2 } };
                var response = await Request(url, "POST", parameters);
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
                response.Dispose();

                if (responseString.Contains("應用系統已中斷連線，請重新由入口網站主畫面左方的主選單，點選欲使用之系統!"))
                    return new RequestResult(false, RequestResult.ErrorType.Unauthorized, "連線逾時");

                SendStat(url, "post", responseString, parameters);

                return new RequestResult(true, RequestResult.ErrorType.None, null);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult(false, RequestResult.ErrorType.HttpError, e.Message);
            }
            catch (Exception e)
            {
                return new RequestResult(false, RequestResult.ErrorType.Unknown, e.Message);
            }
        }

        public static async Task<RequestResult> SendAttendenceAndRewardsStat()
        {
            try
            {
                var url = "http://aps-stu.ntut.edu.tw/StuQuery/QryAbsRew.jsp";
                var parameters = new Dictionary<string, object>() { { "format", -2 } };
                var response = await Request(url, "POST", parameters);
                string responseString = await ConvertStreamToString(await response.Content.ReadAsStreamAsync(), true);
                response.Dispose();

                if (responseString.Contains("應用系統已中斷連線，請重新由入口網站主畫面左方的主選單，點選欲使用之系統!"))
                    return new RequestResult(false, RequestResult.ErrorType.Unauthorized, "連線逾時");

                SendStat(url, "post", responseString, parameters);

                return new RequestResult(true, RequestResult.ErrorType.None, null);
            }
            catch (HttpRequestException e)
            {
                return new RequestResult(false, RequestResult.ErrorType.HttpError, e.Message);
            }
            catch (Exception e)
            {
                return new RequestResult(false, RequestResult.ErrorType.Unknown, e.Message);
            }
        }

        private static async void SendStat(string url, string method, string responseBody, Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
        {
            var parametersJson = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
            var headersJson = Newtonsoft.Json.JsonConvert.SerializeObject(headers);

            //Send stat
            try
            {
                var request = await Request("https://lucher.club/api/ntutwin/doc", "post", new Dictionary<string, object> {
                    { "url", url },
                    { "method", method.ToLower() },
                    { "parameters", parametersJson },
                    { "headers", headersJson },
                    { "responseBody", responseBody }
                });
                request.Dispose();
            }
            catch(Exception)
            {
            }
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

        private static async Task<HttpResponseMessage> Request(string url, string method = "get", Dictionary<string, object> parameters = null, Dictionary<string, object> headers = null)
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
                foreach(string key in headers.Keys)
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