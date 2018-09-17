using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

namespace NTUTWin
{
    class NPAPI
    {
        public class GetSemestersResult
        {
            public GetSemestersResult(List<Semester> semesters, string name)
            {
                Semesters = semesters;
                Name = name;
            }

            public List<Semester> Semesters { get; private set; }
            public string Name { get; private set; }
        }

        public class SessionExpiredException : Exception
        {
            public SessionExpiredException() : base("連線逾時")
            {
            }
        }

        private static ConnectionHelper connectionHelper = new ConnectionHelper();
        private static CaptchaHelper captchaHelper = new CaptchaHelper(connectionHelper);

        public static async Task LoginNPortal(string id, string password)
        {
            string captchaText = await captchaHelper.GetCapchaText();

            var BlowFish = new BlowFishCS.BlowFish(Encoding.UTF8.GetBytes(password));
            var md5Code = BlowFish.Encrypt_ECB(id);

            var responseString = await connectionHelper.RequestString("https://nportal.ntut.edu.tw/login.do", "POST", new Dictionary<string, object>()
                {
                    {"muid", id },
                    {"mpassword", password },
                    {"authcode", captchaText },
                    {"md5Code", md5Code }
                }, new Dictionary<string, object>()
                {
                    {"User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/49.0.2623.112 Safari/537.36" },
                    {"Referer", "https://nportal.ntut.edu.tw/index.do?thetime=" + connectionHelper.GetTimeStampString() }
                });

            if (responseString.Contains("登入失敗"))
            {
                if (responseString.Contains("密碼錯誤"))
                    throw new Exception("帳號密碼錯誤");
                else if (responseString.Contains("驗證碼"))
                    await LoginNPortal(id, password); // Wrong captcha, retry
                else if (responseString.Contains("帳號已被鎖住"))
                    throw new Exception("嘗試錯誤太多次，帳號已被鎖定10分鐘");
            }
            else if (!responseString.Contains("\"myPortal.do?thetime="))
                throw new Exception("遇到不明的錯誤");

            //The following 2 request will make the server allowing us to login sub-systems
            var response = await connectionHelper.Request("https://nportal.ntut.edu.tw/myPortalHeader.do");
            response.Dispose();

            response = await connectionHelper.Request("https://nportal.ntut.edu.tw/aptreeBox.do");
            response.Dispose();

            //Login aps
            await LoginSubSystem("https://nportal.ntut.edu.tw/ssoIndex.do?apUrl=https://aps.ntut.edu.tw/course/tw/courseSID.jsp&apOu=aa_0010-&sso=true&datetime1=" + connectionHelper.GetTimeStampString());
            //Login aps-stu
            await LoginSubSystem("https://nportal.ntut.edu.tw/ssoIndex.do?apUrl=https://aps-stu.ntut.edu.tw/StuQuery/LoginSID.jsp&apOu=aa_003&sso=big5&datetime1=" + connectionHelper.GetTimeStampString());
        }

        public static async Task LogoutNPortal()
        {
            var response = await connectionHelper.Request("https://nportal.ntut.edu.tw/logout.do", "GET");

            if (!response.IsSuccessStatusCode)
                throw new Exception("登入失敗");

            var roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values.Remove("JSESSIONID");
            roamingSettings.Values.Remove("id");
            roamingSettings.Values.Remove("password");
        }

        public static async Task LoginAps()
        {
            var responseString = await connectionHelper.RequestString("https://nportal.ntut.edu.tw/ssoIndex.do?apUrl=https://aps.ntut.edu.tw/course/tw/courseSID.jsp&apOu=aa_0010&sso=true", "GET");

            var matches = Regex.Matches(responseString, "<input type='hidden' name='([a-zA-Z]+)' value='([^']+)'>");

            var postData = new Dictionary<string, object>();
            foreach (Match match in matches)
                postData.Add(match.Groups[1].Value, match.Groups[2].Value);

            responseString = await connectionHelper.RequestBig5String("https://aps.ntut.edu.tw/course/tw/courseSID.jsp", "POST", postData);

            Debug.WriteLine(responseString);
        }

        public static async Task LoginSubSystem(string url)
        {
            
            var responseString = await connectionHelper.RequestString(url, "GET", null, new Dictionary<string, object> {
                {"Referer", "https://nportal.ntut.edu.tw/myPortal.do?thetime=" + connectionHelper.GetTimeStampString() + "_true" }
            });

            var matches = Regex.Matches(responseString, "<input type='hidden' name='([a-zA-Z]+)' value='([^']+)'>");
            var target = Regex.Match(responseString, "action='([^']+)'").Groups[1].Value;

            var postData = new Dictionary<string, object>();
            foreach (Match match in matches)
                postData.Add(match.Groups[1].Value, match.Groups[2].Value);

            responseString = await connectionHelper.RequestBig5String(target, "POST", postData);

            Debug.WriteLine(responseString);
        }

        public static async Task BackgroundLogin()
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;

            if (!roamingSettings.Values.ContainsKey("id") || !roamingSettings.Values.ContainsKey("password"))
                throw new Exception("登入失敗");

            string id = (string)roamingSettings.Values["id"];
            string password = (string)roamingSettings.Values["password"];

            if(string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(password))
                throw new Exception("登入失敗");

            await LoginNPortal(id, password);
        } 

        public static async Task<GetSemestersResult> GetSemesters(string id)
        {
            var url = "https://aps.ntut.edu.tw/course/tw/Select.jsp";
            var parameters = new Dictionary<string, object>() {
                {"code", id},
                {"format", -3}
            };
            var responseString = await connectionHelper.RequestNPortal(url, "POST", parameters);

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
                    throw new Exception(match.Groups[1].Value);
                else
                    throw new Exception("查詢失敗");
            }

            return new GetSemestersResult(semesters, name);
        }

        public static async Task<List<Course>> GetCourses(string id, int year, int semester)
        {
            var url = string.Format("https://aps.ntut.edu.tw/course/tw/Select.jsp?format=-2&code={0}&year={1}&sem={2}", id, year, semester);
            var responseString = await connectionHelper.RequestNPortal(url);

            return Course.ParseFromDocument(responseString);
        }

        // TODO: Fix this
        public static async Task<Schedule> GetSchedule(int academicYear)
        {
            var responseString = await connectionHelper.RequestString(string.Format("https://www.cc.ntut.edu.tw/~wwwoaa/oaa-nwww/oaa-cal/oaa-cal_{0}.html", academicYear), "GET");

            return Schedule.Parse(responseString);
        }

        public static async Task<MidAlerts> GetMidAlerts()
        {
            var url = "https://aps-stu.ntut.edu.tw/StuQuery/QrySCWarn.jsp";
            var responseString = await connectionHelper.RequestNPortal(url);

            return MidAlerts.Parse(responseString);
        }

        public static async Task<Credits> GetCredits()
        {
            var url = "https://aps-stu.ntut.edu.tw/StuQuery/QryScore.jsp";
            var parameters = new Dictionary<string, object>() { { "format", -2 } };
            var responseString = await connectionHelper.RequestNPortal(url, "POST", parameters);

            return await Credits.Parse(responseString);
        }

        public static async Task<CourseDetail> GetCourseDetail(string courseId)
        {
            var url = "https://aps.ntut.edu.tw/course/tw/Select.jsp";
            var parameters = new Dictionary<string, object>() { { "code", courseId }, { "format", -1 } };
            var responseString = await connectionHelper.RequestNPortal(url, "POST", parameters);

            return CourseDetail.Parse(responseString);
        }

        public static async Task<AttendenceAndHonors> GetAttendenceAndHonors()
        {
            var url = "https://aps-stu.ntut.edu.tw/StuQuery/QryAbsRew.jsp";
            var parameters = new Dictionary<string, object>() { { "format", -2 } };
            var responseString = await connectionHelper.RequestNPortal(url, "POST", parameters);

            return AttendenceAndHonors.Parse(responseString);
        }

		public static async Task<bool> IsLoggedIn()
		{
            var url = "https://nportal.ntut.edu.tw/myPortal.do";
            var responseString = await connectionHelper.RequestString(url, "GET");

            return 
                !responseString.Contains("您目前已和伺服器中斷連線") &&
                !responseString.Contains("您的帳號已於其他地方登入。");
        }
    }
}