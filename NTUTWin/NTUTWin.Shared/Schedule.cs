using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace NTUTWin
{
    class Schedule
    {
        public class SchoolEvent
        {
            public string description;
            public string timeString;
            public DateTime date;

            public override string ToString()
            {
                return string.Format("{0}: {1}", timeString, description);
            }
        }

        public List<SchoolEvent> events = new List<SchoolEvent>();
        public Dictionary<int, List<SchoolEvent>> monthSchedules = new Dictionary<int, List<SchoolEvent>>();

        public static Schedule Parse(string data)
        {
            Schedule schedule = new Schedule();
            int academicYear = ParseAcademicYear(data);
            var regex = new Regex("valign=\"top\"[^>]*><p>((?:(?!<\\/p>).)+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
            var matches = regex.Matches(data);
            var eventRegex = new Regex("(?:<br \\/>)?\\s*\\(?([^\\)]+)\\)((?:(?!<br \\/>|\\(\\d).)+)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Multiline);
            var dateRegex = new Regex("(\\d+)\\/(\\d+)");
            var endCommaRegex = new Regex("、\\s?$", RegexOptions.Multiline);
            foreach(Match match in matches)
            {
                var eventMatches = eventRegex.Matches(match.Groups[1].Value);
                foreach(Match eventMatch in eventMatches)
                {
                    SchoolEvent schoolEvent = new SchoolEvent();
                    schoolEvent.timeString = eventMatch.Groups[1].Value.Replace('~', '-');
                    schoolEvent.description = endCommaRegex.Replace(eventMatch.Groups[2].Value, "");
                    //Get date
                    var dateMatch = dateRegex.Match(schoolEvent.timeString);
                    int month = int.Parse(dateMatch.Groups[1].Value);
                    int day = int.Parse(dateMatch.Groups[2].Value);
                    int year = (month > 7 ? academicYear : academicYear + 1) + 1911;
                    schoolEvent.date = new DateTime(year, month, day);
                    
                    //Insert into event list
                    schedule.events.Add(schoolEvent);
                    //Insert into month schedule
                    if(!schedule.monthSchedules.ContainsKey(month))
                        schedule.monthSchedules.Add(month, new List<SchoolEvent>());
                    var monthSchedule = schedule.monthSchedules[month];
                    monthSchedule.Add(schoolEvent);
                }
            }
            return schedule;
        }

        private static int ParseAcademicYear(string data)
        {
            var regex = new Regex("國立臺北科技大學([0-9]+)學年度第");
            var match = regex.Match(data);
            return int.Parse(match.Groups[1].Value);
        }
    }
}
