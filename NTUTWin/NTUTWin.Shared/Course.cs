using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace NTUTWin
{
    class Course
    {
        public int IdForSelect { get; set; }
        public string IdForCurr { get; set; }
        public int Phase { get; set; }
        public int Hours { get; set; }
        public float Credit { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Note { get; set; }
        public List<string> Teachers { get; set; } = new List<string>();
        public List<string> Classes { get; set; } = new List<string>();
        public List<string> ClassRooms { get; set; } = new List<string>();
        public Dictionary<int, List<int>> Schedule { get; set; } = new Dictionary<int, List<int>>();

        public static List<Course> ParseFromDocument(string docString)
        {
            List<Course> courses = new List<Course>();

            var courseTrs = new List<string>();

            int trOpenIndex = 0, trCloseIndex = 0;

            trOpenIndex = docString.IndexOf("<tr>\n", 0);
            while (trOpenIndex != -1)
            {
                trCloseIndex = docString.IndexOf("</tr>\n", trOpenIndex);

                courseTrs.Add(docString.Substring(trOpenIndex, trCloseIndex - trOpenIndex));

                trOpenIndex = docString.IndexOf("<tr>\n", trCloseIndex);
            }

            foreach (var courseTr in courseTrs)
                courses.Add(Course.ParseFromTr(courseTr));

            return courses;
        }

        public static Course ParseFromTr(string trString)
        {
            Course course = new Course();
            Match match;
            MatchCollection matches;

            //Get 課號
            match = Regex.Match(trString, "<A href=\"Select\\.jsp\\?format=-1&code=([0-9]+)\">[0-9]+</A>");
            course.IdForSelect = int.Parse(match.Groups[1].Value);
            //Get 課程名稱
            match = Regex.Match(trString, "<A href=\"Curr\\.jsp\\?format=-2&code=([a-zA-Z0-9]+)\">([^<]+)</A>");
            course.IdForCurr = match.Groups[1].Value;
            course.Name = match.Groups[2].Value;
            Debug.WriteLine(course.Name);
            //Get 階段,學分,時數
            matches = Regex.Matches(trString, "<td align=CENTER>([0-9\\.]+)\n");
            course.Phase = int.Parse(matches[0].Groups[1].Value);
            course.Credit = float.Parse(matches[1].Groups[1].Value);
            course.Hours = int.Parse(matches[2].Groups[1].Value);
            //Get 修
            match = Regex.Match(trString, "<td><div align=center>([^<])</div>\n");
            course.Type = match.Groups[1].Value;
            //Get 教師
            matches = Regex.Matches(trString, "<A href=\"Teach\\.jsp\\?format=-3&year=([0-9]+)&sem=([0-9]+)&code=([0-9]+)\">([^<]+)</A>");
            foreach (Match tMatch in matches)
                course.Teachers.Add(tMatch.Groups[4].Value);
            //Get 班級
            matches = Regex.Matches(trString, "<A href=\"Subj\\.jsp\\?format=-4&year=([0-9]+)&sem=([0-9]+)&code=([0-9]+)\">([^<]+)</A>");
            foreach (Match tMatch in matches)
                course.Classes.Add(tMatch.Groups[4].Value);
            //Get 日,一,二,三,四,五,六
            matches = Regex.Matches(trString.Substring(IndexOf(trString, "<td", 9)), "<td>\\s((?:\\s?[0-9A-Z]?)+)");
            for (int d = 0; d < 7; d++)
            {
                List<int> times = new List<int>();
                var originalTimeString = matches[d].Groups[1].Value; //ex: "5 6", "3", "a" etc...

                if (originalTimeString.Length == 0) continue;

                var timeStrings = originalTimeString.Split(' ');
                foreach (string timeString in timeStrings)
                {
                    Debug.Assert(Regex.IsMatch(timeString, "[0-9a-zA-Z]"), "Invalid character");

                    int time;
                    if (!int.TryParse(timeString, out time))
                        time = char.ToUpper(timeString[0]) - 64 + 9; //Convert A to 9, B to 10, C to 11 etc...
                    times.Add(time);
                }

                if (times.Count > 0)
                {
                    var day = (d + 6) % 7; // Shift orders
                    course.Schedule.Add(day, times);
                }
            }
            //Get 教室
            matches = Regex.Matches(trString, "<A href=\"Croom.jsp[^>]+>([^<]+)</A>");
            foreach (Match cMatch in matches)
                course.ClassRooms.Add(cMatch.Groups[1].Value);

            //Get 備註
            match = Regex.Match(trString.Substring(IndexOf(trString, "<td", 21)), "<td>([^\n]+)");
            course.Note = match.Groups[1].Value;

            return course;

            /*
            <tr>
            <td><A href="Select.jsp?format=-1&code=195204">195204</A>
            <td><A href="Curr.jsp?format=-2&code=5703034">創業管理</A>
            <td align=CENTER>1
            <td align=CENTER>3.0
            <td align=CENTER>3
            <td><div align=center>選</div>
            <td><A href="Teach.jsp?format=-3&year=103&sem=2&code=22361">呂芳堯</A><BR>
            <A href="Teach.jsp?format=-3&year=103&sem=2&code=10412">廖森貴</A><BR>

            <td><A href="Subj.jsp?format=-4&year=103&sem=2&code=1005">最後一哩課程(大)</A><BR>
            <A href="Subj.jsp?format=-4&year=103&sem=2&code=1593">四管三</A><BR>

            <td>　<td>　<td>　<td>　<td> 5 6 7<td>　<td>　<td><A href="Croom.jsp?format=-3&year=103&sem=2&code=342">綜二演講廳</A><BR>

            <td><div align=center>　</div>
            <td>　
            <td align=CENTER>　
            <td align=center><BR><A href="ShowSyllabus.jsp?snum=195204&code=10412">查詢</A><BR>
            <td>經管三與最後一哩合開，限180人
            </tr>
            */

            /*
            var matches = Regex.Matches(responseString, "<tr>\n"
                + "<td><A href=\"Select\\.jsp\\?format=-1&code=([0-9]+)\">[0-9]+</A>\n" //課號
                + "<td><A href=\"Curr\\.jsp\\?format=-2&code=([0-9]+)\">([^<]+)</A>\n" //課程名稱
                + "<td align=CENTER>([0-9])\n<td align=CENTER>([0-9\\.]+)\n<td align=CENTER>([0-9])\n" //階段,學分,時數
                + "<td><div align=center>([必選通])</div>\n" //修
                + "<td><A href=\"Teach\\.jsp\\?format=-3&year=([0-9]+)&sem=([0-9]+)&code=([0-9]+)\">([^<]+)</A><BR>\n\n" //教師
                + "<td><A href=\"Subj\\.jsp\\?format=-4&year=([0-9]+)&sem=([0-9]+)&code=([0-9]+)\">([^<]+)</A><BR>\n\n" //班級
                + "<td>([0-9\\s]*)<td>([0-9\\s]*)<td>([0-9\\s]*)<td>([0-9\\s]*)<td>([0-9\\s]*)<td>([0-9\\s]*)<td>([0-9\\s]*)" //日,一,二,三,四,五,六
                + "<td><A href=\"Croom\\.jsp\\?format=-3&year=([0-9]+)&sem=([0-9]+)&code=([0-9]+)\">([^<]+)</A><BR>\n" //教室
                + "<A href=\"Croom\\.jsp\\?format=-([0-9]+)&year=([0-9]+)&sem=([0-9]+)&code=([0-9]+)\">([^<]+)</A><BR>\n\n"
                + "<td><div align=center>　</div>\n<td>　\n<td align=CENTER>　\n" //選課狀況, 教學助理, 授課語言
                + "<td align=center><A href=\"ShowSyllabus\\.jsp\\?snum=([0-9]+)&code=([0-9]+)\">查詢</A><BR>\n" //教學大綱與進度表
                + "<td>([^\n]+)\n</tr>" //備註
                );
            */
        }

        //String helper
        private static int IndexOf(string inputString, string value, int time, int startIndex = 0)
        {
            if (time < 1)
                throw new ArgumentException("time should greater than 0");

            int index = inputString.IndexOf(value, startIndex);
            if (time == 1 || index == -1)
                return index;
            else
                return IndexOf(inputString, value, time - 1, index + value.Length);
        }

        public static string GetTimeString(int time)
        {
            string[] timeStrings = new string[]
            {
                "08:10 - 09:00", //1~4
                "09:10 - 10:00",
                "10:10 - 11:00",
                "11:10 - 12:00",
                "13:10 - 14:00", //5~9
                "14:10 - 15:00",
                "15:10 - 16:00",
                "16:10 - 17:00",
                "17:10 - 18:00",
                "18:30 - 19:20", //A~D
                "19:20 - 20:10",
                "20:20 - 21:10",
                "21:10 - 22:00"
            };

            if (time > 0 && time <= timeStrings.Length)
                return timeStrings[time - 1];
            else
                return null;
        }
    }
}
