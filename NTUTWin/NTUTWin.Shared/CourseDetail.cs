using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NTUTWin
{
    class CourseDetail
    {

        public class Student
        {
            public string Class { get; set; }
            public string StudentId { get; set; }
            public string Name { get; set; }
            public string EnglishName { get; set; }
            public string ClassStatus { get; set; }
            public string SchoolStatus { get; set; }
        }

        public string CourseId { get; set; }
        public int SchoolYear { get; set; }
        public int Semester { get; set; }
        public string Name { get; set; }
        public int Phase { get; set; }
        public float Hours { get; set; }
        public float Credits { get; set; }
        public string Type { get; set; }
        public List<string> Teachers { get; set; } = new List<string>();
        public List<string> Classes { get; set; } = new List<string>();
        public List<string> ClassRooms { get; set; } = new List<string>();
        public int PeopleCount { get; set; }
        public int QuitPeopleCount { get; set; }
        public List<string> TeachingAssistants { get; set; } = new List<string>();
        public string Language { get; set; }
        public bool Audit { get; set; }
        public bool Pratice { get; set; }
        public string Note { get; set; }
        public List<Student> Students { get; set; } = new List<Student>();

        public static CourseDetail Parse(string html)
        {
            CourseDetail detail = new CourseDetail();
            Regex regex = new Regex(
                "<table border=1>\n" +
                "<tr><th>課　　號<td>([^\n]*)\n" +
                "<tr><th>學 年 度<td>([^<]*)</tr>\n" +
                "<tr><th>學　　期<td>([^<]*)</tr>\n" +
                "<tr><th>課程名稱\n" +
				"<td><a href=\"[^\"]+\">([^<]*)</a>\n" +
				"<tr><th>階　　段<td>([^<]*)</tr>\n" +
				"<tr><th>學　　分<td>([^<]*)</tr>\n" +
				"<tr><th>時　　數<td>([^<]*)</tr>\n" +
				"<tr><th>類　　別<td>([^<]*)</tr>\n" +
				"<tr><th>授課教師\n" +
				"<td>(?:<a href=\"Teach.jsp[^\"]+\">(?<teachers>[^<]*)</a>(?:<br>)?\\s*\n*)*\\s*\n*" +
				"(?:<A href=\"[^\"]+\">《查詢教學大綱與進度表》</A><BR>\n+)?" +
				"<tr><th>開課班級\n" +
				"<td>(?:<a href=\"[^\"]+\">(?<classes>[^<]*)</a><br>\n)*\\s?\n" +
				"<tr><th>教　　室\n" +
				"<td>(?:<a href=\"[^\"]+\">(?<classRooms>[^<]*)</a><br>\n)*\\s?\n" +
				"<tr><th>修課人數<td>([^<]*)</tr>\n" +
				"<tr><th>撤選人數<td>([^<]*)</tr>\n" +
				"<tr><th>教學助理\n" +
				"<td>(?:(?!<tr>).|\n)+" +
				"<tr><th>授課語言<td>([^\n]*)\n" +
				"<tr><th>隨班附讀<td>([^\n]*)\n" +
				"<tr><th>實驗、實習<td>([^\n]*)\n" +
				"<tr><th>備　　註<td>([^\n]*)\n"
				, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            var match = regex.Match(html);

            detail.CourseId = match.Groups[1].Value;
            detail.SchoolYear = int.Parse(match.Groups[2].Value);
            detail.Semester = int.Parse(match.Groups[3].Value);
            detail.Name = match.Groups[4].Value;
            detail.Phase = int.Parse(match.Groups[5].Value);
            detail.Credits = float.Parse(match.Groups[6].Value);
            detail.Hours = float.Parse(match.Groups[7].Value);
            detail.Type = match.Groups[8].Value;

            switch (detail.Type)
            {
                case "○":
                    detail.Type = "部訂共同必修";
                    break;
                case "△":
                    detail.Type = "校訂共同必修";
                    break;
                case "☆":
                    detail.Type = "共同選修";
                    break;
                case "●":
                    detail.Type = "部訂專業必修";
                    break;
                case "▲":
                    detail.Type = "校訂專業必修";
                    break;
                case "★":
                    detail.Type = "專業選修";
                    break;
            }

            foreach (Capture capture in match.Groups["teachers"].Captures)
                detail.Teachers.Add(capture.Value);

            foreach (Capture capture in match.Groups["classes"].Captures)
                detail.Classes.Add(capture.Value);

            foreach (Capture capture in match.Groups["classRooms"].Captures)
                detail.ClassRooms.Add(capture.Value);

            detail.PeopleCount = int.Parse(match.Groups[9].Value);
            detail.QuitPeopleCount = int.Parse(match.Groups[10].Value);
            detail.Language = match.Groups[11].Value;
            detail.Audit = match.Groups[12].Value == "是";
            detail.Pratice = match.Groups[13].Value == "是";
            detail.Note = match.Groups[14].Value;

            var studentsRegex = new Regex("^<tr><td>([^\\s]*)\\s*<td><a href=\"[^\"]+\">([^<]*)</a><td>([^<]*)<td>\\s*([^<]+)<td><div align=center>([^<]*)</div><td(?: bgcolor=#ffcccc)?>([^\\s]*)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var studentsMatches = studentsRegex.Matches(html);
            foreach(Match studentMatch in studentsMatches)
            {
                Student student = new Student();
                student.Class = studentMatch.Groups[1].Value;
                student.StudentId = studentMatch.Groups[2].Value;
                student.Name = studentMatch.Groups[3].Value;
                student.EnglishName = studentMatch.Groups[4].Value;
                student.ClassStatus = studentMatch.Groups[5].Value;
                student.SchoolStatus = studentMatch.Groups[6].Value;
                detail.Students.Add(student);
            }
            return detail;
        }
    }
}