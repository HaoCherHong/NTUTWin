using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            //replace linebreak with space
            docString = docString.Replace('\n', ' ');

            List<Course> courses = new List<Course>();

            //find tr elements
            var matches = Regex.Matches(docString, "<tr>(.(?!<tr>))+");

            //parse trs
            for(int i = 2; i < matches.Count - 1; i++)
                courses.Add(Course.ParseFromTr(matches[i].Value));

            return courses;
        }

        public static Course ParseFromTr(string trString)
        {
            var tdMatches = Regex.Matches(trString, "<td(?:.(?!<td))+.");
            Course course = new Course();
            Match match;
            MatchCollection matches;

            //Get 課號
            match = Regex.Match(tdMatches[0].Value, ">([0-9]+)</A>");
            if(match.Success)
                course.IdForSelect = int.Parse(match.Groups[1].Value);
            //Get 課程名稱
            match = Regex.Match(tdMatches[1].Value, "code=([a-zA-Z0-9]+)\">([^<]+)</A>");
            if (match.Success)
            {
                course.IdForCurr = match.Groups[1].Value;
                course.Name = match.Groups[2].Value;
            }
            else
                course.Name = Regex.Match(tdMatches[1].Value, "<td>(.*)").Groups[1].Value;
            Debug.WriteLine(course.Name);
            //Get 階段,學分,時數
            match = Regex.Match(tdMatches[2].Value, "[0-9\\.]+");
            if (match.Success)
                course.Phase = int.Parse(match.Value);
            match = Regex.Match(tdMatches[3].Value, "[0-9\\.]+");
            if (match.Success)
                course.Credit = float.Parse(match.Value);
            match = Regex.Match(tdMatches[4].Value, "[0-9\\.]+");
            if (match.Success)
                course.Hours = int.Parse(match.Value);
            //Get 修
            match = Regex.Match(tdMatches[5].Value, ">(.)<");
            if (match.Success)
                course.Type = match.Groups[1].Value;
            //Get 教師
            matches = Regex.Matches(tdMatches[6].Value, ">([^<]+)</A>");
            foreach (Match tMatch in matches)
                course.Teachers.Add(tMatch.Groups[1].Value);
            //Get 班級
            matches = Regex.Matches(tdMatches[7].Value, ">([^<]+)</A>");
            foreach (Match tMatch in matches)
                course.Classes.Add(tMatch.Groups[1].Value);
            //Get 日,一,二,三,四,五,六
            for (int d = 0; d < 7; d++)
            {
                matches = Regex.Matches(tdMatches[8 + d].Value, "[0-9A-Z]");
                if (matches.Count == 0) continue;
                List<int> times = new List<int>();
                foreach (Match tMatch in matches)
                {
                    int time;
                    if (!int.TryParse(tMatch.Value, out time))
                        time = char.ToUpper(tMatch.Value[0]) - 64 + 9; //Convert A to 9, B to 10, C to 11 etc...
                    times.Add(time);
                }

                if (times.Count > 0)
                {
                    var day = (d + 6) % 7; // Shift orders
                    course.Schedule.Add(day, times);
                }
            }
            //Get 教室
            matches = Regex.Matches(tdMatches[15].Value, ">([^<]+)</A>");
            foreach (Match cMatch in matches)
                course.ClassRooms.Add(cMatch.Groups[1].Value);

            //Get 備註
            match = Regex.Match(tdMatches[20].Value, "<td>([^<]*)");
            if (match.Success)
                course.Note = match.Groups[1].Value;

            return course;
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
