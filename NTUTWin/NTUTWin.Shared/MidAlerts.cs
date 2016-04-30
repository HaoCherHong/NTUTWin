using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NTUTWin
{
    class MidAlerts
    {
        public class MidAlert
        {
            public class AlertRatio
            {
                public int Alerted { get; set; }
                public int All { get; set; }
                public override string ToString()
                {
                    if (Alerted == 0 && All == 0)
                        return string.Empty;
                    else
                        return "(" + Alerted + "/" + All + ")";
                }
            }

            public string CourseNumber { get; set; }
            public string Type { get; set; }
            public string CourseName { get; set; }
            public float Credit { get; set; }
            public string Note { get; set; }
            public bool Alerted { get; set; }
            public bool AlertSubmitted { get; set; }
            public AlertRatio Ratio { get; set; }
        }
        
        public List<MidAlert> Alerts { get; private set; }
        public string Semester { get; private set; }

        public static MidAlerts Parse(string data)
        {
            MidAlerts result = new MidAlerts();
            result.Alerts = new List<MidAlert>();
            result.Semester = new Regex("<img src=./image/or_ball.gif>([^<]+)</h3>", RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(data).Groups[1].Value.Trim();
            var regex = new Regex("<tr><td align=Right>([^\n]*)\n<td align=Center>([^\n]*)\n<td align=Left>([^\n]*)\n<th align=Right>([^\n]*)\n<td align=Left>([^\n]*)\n<th>([^<]*)<th>([^<]*)<th>(\\d+) / (\\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            var matches = regex.Matches(data);
            foreach(Match match in matches)
            {
                MidAlert alert = new MidAlert();
                alert.CourseNumber = match.Groups[1].Value;
                alert.Type = match.Groups[2].Value;
                alert.CourseName = match.Groups[3].Value;
                alert.Credit = float.Parse(match.Groups[4].Value);
                alert.Note = match.Groups[5].Value;
                alert.Alerted = !string.IsNullOrWhiteSpace(match.Groups[6].Value);
                alert.AlertSubmitted = string.IsNullOrWhiteSpace(match.Groups[7].Value);
                alert.Ratio = new MidAlert.AlertRatio();
                alert.Ratio.Alerted = int.Parse(match.Groups[8].Value);
                alert.Ratio.All = int.Parse(match.Groups[9].Value);
                result.Alerts.Add(alert);
            }

            return result;
        }
    }
}
