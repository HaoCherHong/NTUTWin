using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NTUTWin
{
    class AttendenceAndHonors
    {
        public class Semester
        {
            public string Name { get; set; }
            public Dictionary<string, int> HonorsStatistics { get; set; } = new Dictionary<string, int>();
            public List<Honor> HonorDetails { get; set; } = new List<Honor>();
            public Dictionary<string, int> AttendenceStatistics { get; set; } = new Dictionary<string, int>();
            public List<Attendence> AttendenceDetails { get; set; } = new List<Attendence>();

            public override string ToString()
            {
                return Name;
            }
        }

        public class Honor
        {
            public string Time { get; set; }
            public string Type { get; set; }
            public int Count { get; set; }
            public string Detail { get; set; }
        }

        public class Attendence
        {
            public int Week { get; set; }
            public string Time { get; set; }
            public string Session { get; set; }
            public string RollcallSheetNumber { get; set; }
            public string Type { get; set; }
            public string Detail { get; set; }
        }

        public List<Semester> Semesters { get; set; } = new List<Semester>();

        public static AttendenceAndHonors Parse(string html)
        {
            AttendenceAndHonors result = new AttendenceAndHonors();
            var semestersRegex = new Regex(
                "<img src=./image/or_ball.gif>([^\n]+)\n" +
                "\\s<blockquote>((?:(?!<\\/blockquote>).|\n)+)<\\/blockquote>\n<blockquote>((?:(?!<\\/blockquote>).|\n)+)<\\/blockquote>");
            var semestersMatches = semestersRegex.Matches(html);
            foreach(Match semesterMatch in semestersMatches)
            {
                Semester semester = new Semester();
                semester.Name = semesterMatch.Groups[1].Value;

                var honorStatisticsRegex = new Regex("^<tr><td align=center BGCOLOR=CCFFFF>([^<]+)<td align=center BGCOLOR=99FF99>([^<\n]+)\n", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var honorStatisticsMatches = honorStatisticsRegex.Matches(semesterMatch.Groups[2].Value);

                foreach(Match match in honorStatisticsMatches)
                    semester.HonorsStatistics.Add(match.Groups[1].Value, int.Parse(match.Groups[2].Value));

                var honorDetailsRegex = new Regex(
                    "^<tr><td align=center BGCOLOR=CCFFFF>([^<]+)" +
                    "<td align=center BGCOLOR=99FF99>([^<]+)" +
                    "<td align=center BGCOLOR=CCFFFF>([^<]+)" +
                    "<td align=Left   BGCOLOR=99FF99>([^\n]+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var honorDetailsMatches = honorDetailsRegex.Matches(semesterMatch.Groups[2].Value);

                foreach (Match match in honorDetailsMatches)
                {
                    Honor honor = new Honor();
                    honor.Time = match.Groups[1].Value;
                    honor.Type = match.Groups[2].Value;
                    honor.Count = int.Parse(match.Groups[3].Value);
                    honor.Detail = match.Groups[4].Value;
                    semester.HonorDetails.Add(honor);
                }

                var attendenceStatisticsRegex = new Regex("^<tr><td align=center BGCOLOR=CCFFFF>([^<]+)<td align=center BGCOLOR=99FF99>([^<\n]+)\n", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var attendenceStatisticsMatches = attendenceStatisticsRegex.Matches(semesterMatch.Groups[3].Value);

                foreach (Match match in attendenceStatisticsMatches)
                    semester.AttendenceStatistics.Add(match.Groups[1].Value == "遲到�早退" ? "遲到/早退" : match.Groups[1].Value, int.Parse(match.Groups[2].Value));

                var attendenceDetailsRegex = new Regex(
                    "^<tr><td align=center BGCOLOR=CCFFFF>([^<]+)" + 
                    "<td align=center BGCOLOR=99FF99>([^<]+)" + 
                    "<td align=center BGCOLOR=CCFFFF>([^<]+)" + 
                    "<td align=center BGCOLOR=99FF99>([^<]+)" + 
                    "<td align=center BGCOLOR=CCFFFF>([^<]+)" + 
                    "<td align=Left   BGCOLOR=99FF99>([^\n]+)"
                    , RegexOptions.Multiline | RegexOptions.IgnoreCase);
                var attendenceDetailsMatches = attendenceDetailsRegex.Matches(semesterMatch.Groups[3].Value);

                foreach (Match match in attendenceDetailsMatches)
                {
                    Attendence attendence = new Attendence();
                    attendence.Week = int.Parse(match.Groups[1].Value);
                    attendence.Time = match.Groups[2].Value;
                    attendence.Session = match.Groups[3].Value;
                    attendence.RollcallSheetNumber = match.Groups[4].Value;
                    attendence.Type = match.Groups[5].Value == "遲到�早退" ? "遲到/早退" : match.Groups[5].Value;
                    attendence.Detail = new Regex("<BR>$").Replace(match.Groups[6].Value, "");
                    semester.AttendenceDetails.Add(attendence);
                }

                result.Semesters.Add(semester);
            }

            return result;
        }
    }
}

/*
 <HTML>
<HEAD>
<meta http-equiv="Content-Type" content="text/html; charset=big5">
<TITLE>學生查詢專區--考勤、獎懲查詢</TITLE>
<Script src="http://font.cc.ntut.edu.tw/wfs/js/lib/wfs.js" type="text/javascript" ></script>
<Script Language=JavaScript>
<!--
function toQuery(form, code) {
  form.format.value = code;
  form.submit();
}
-->
</Script>
</HEAD>

<BODY bgcolor=#ffffdf background="./image/grey918y.gif" text=#003398>
<form name="FormQ" action="" method="post">
<font size=5><b>獎懲、缺曠課、請假記錄表</b></font>
  <input type="hidden" name="format" value="-1">　
  <input type="button" name="QrySem" value="查詢本學期"   onClick="toQuery(FormQ, -1);">　
  <input type="button" name="QryAll" value="查詢歷年記錄" onClick="toQuery(FormQ, -2);">
</form>

<H4>洪？哲(103590044)獎懲、缺曠課、請假記錄狀況</H4>
<img src=./image/or_ball.gif>104 學年度 第 2 學期
 <blockquote>
<table border=1>
<tr><th colspan=2 BGCOLOR=FFCCCC>獎懲統計表
<tr><th BGCOLOR=CCFFFF>類別<th BGCOLOR=99FF99>次數
<tr><td align=center BGCOLOR=CCFFFF>申誡<td align=center BGCOLOR=99FF99>2
</table>
<P>
<table border=1>
<tr><th colspan=4 BGCOLOR=FFCCCC>獎懲詳細記錄
<tr><th BGCOLOR=CCFFFF>日期(年月日)<th BGCOLOR=99FF99>類別<th BGCOLOR=CCFFFF>次數<th BGCOLOR=99FF99>獎懲事實
<tr><td align=center BGCOLOR=CCFFFF>105.03.15<td align=center BGCOLOR=99FF99>申誡<td align=center BGCOLOR=CCFFFF>1<td align=Left   BGCOLOR=99FF99>無故不參加週會
<tr><td align=center BGCOLOR=CCFFFF>105.04.01<td align=center BGCOLOR=99FF99>申誡<td align=center BGCOLOR=CCFFFF>1<td align=Left   BGCOLOR=99FF99>無故不參加校運會
</table>
</blockquote>
<blockquote>
<table border=1>
<tr><th colspan=2 BGCOLOR=FFCCCC>缺曠課、請假統計表
<tr><th BGCOLOR=CCFFFF>類別<th BGCOLOR=99FF99>次數
<tr><td align=center BGCOLOR=CCFFFF>曠課<td align=center BGCOLOR=99FF99>14
</table>
<P>
<table border=1>
<tr><th colspan=6 BGCOLOR=FFCCCC>缺曠課、請假詳細記錄</th></tr>
<tr><th BGCOLOR=CCFFFF>週次</th><th BGCOLOR=99FF99>日期(年月日)</th><th BGCOLOR=CCFFFF>節次</th><th BGCOLOR=99FF99>點名單號</th><th BGCOLOR=CCFFFF>類別</th><th BGCOLOR=99FF99>備註</th></tr>
<tr><td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>105.03.08<td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>49368<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>105.03.08<td align=center BGCOLOR=CCFFFF>4<td align=center BGCOLOR=99FF99>49369<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>1<td align=center BGCOLOR=99FF99>49778<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>49779<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>49780<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>4<td align=center BGCOLOR=99FF99>49781<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>49782<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>6<td align=center BGCOLOR=99FF99>49783<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>49784<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>105.03.20<td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>49785<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>李秉吾【四資一】週會<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>105.04.14<td align=center BGCOLOR=CCFFFF>1<td align=center BGCOLOR=99FF99>50639<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>簡名豪【209559】體育<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>105.04.14<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>50640<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>簡名豪【209559】體育<BR>
<tr><td align=center BGCOLOR=CCFFFF>10<td align=center BGCOLOR=99FF99>105.04.28<td align=center BGCOLOR=CCFFFF>1<td align=center BGCOLOR=99FF99>51071<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>簡名豪【209559】體育<BR>
<tr><td align=center BGCOLOR=CCFFFF>11<td align=center BGCOLOR=99FF99>105.05.05<td align=center BGCOLOR=CCFFFF>1<td align=center BGCOLOR=99FF99>51251<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>簡名豪【209559】體育<BR>
</table>
</blockquote>
 <img src=./image/or_ball.gif>103 學年度 第 2 學期
 <blockquote>
<table border=1>
<tr><th colspan=2 BGCOLOR=FFCCCC>獎懲統計表
<tr><th BGCOLOR=CCFFFF>類別<th BGCOLOR=99FF99>次數
</table>
<P>
<table border=1>
<tr><th colspan=4 BGCOLOR=FFCCCC>獎懲詳細記錄
<tr><th BGCOLOR=CCFFFF>日期(年月日)<th BGCOLOR=99FF99>類別<th BGCOLOR=CCFFFF>次數<th BGCOLOR=99FF99>獎懲事實
<tr><td align=center colspan=4 BGCOLOR=CCFFFF>無
</table>
</blockquote>
<blockquote>
<table border=1>
<tr><th colspan=2 BGCOLOR=FFCCCC>缺曠課、請假統計表
<tr><th BGCOLOR=CCFFFF>類別<th BGCOLOR=99FF99>次數
<tr><td align=center BGCOLOR=CCFFFF>曠課<td align=center BGCOLOR=99FF99>25
</table>
<P>
<table border=1>
<tr><th colspan=6 BGCOLOR=FFCCCC>缺曠課、請假詳細記錄</th></tr>
<tr><th BGCOLOR=CCFFFF>週次</th><th BGCOLOR=99FF99>日期(年月日)</th><th BGCOLOR=CCFFFF>節次</th><th BGCOLOR=99FF99>點名單號</th><th BGCOLOR=CCFFFF>類別</th><th BGCOLOR=99FF99>備註</th></tr>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>104.03.25<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>44000<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>104.03.25<td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>44001<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>5<td align=center BGCOLOR=99FF99>104.03.25<td align=center BGCOLOR=CCFFFF>9<td align=center BGCOLOR=99FF99>44002<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>6<td align=center BGCOLOR=99FF99>104.04.01<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>44144<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>6<td align=center BGCOLOR=99FF99>104.04.01<td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>44145<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>104.04.08<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>44295<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>104.04.08<td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>44296<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>104.04.08<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>44257<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>104.04.08<td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>44282<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>104.04.08<td align=center BGCOLOR=CCFFFF>9<td align=center BGCOLOR=99FF99>44283<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>104.04.14<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>44461<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>曾毅誠【203477】經濟學概論<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>104.04.15<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>44503<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>104.04.15<td align=center BGCOLOR=CCFFFF>3<td align=center BGCOLOR=99FF99>44504<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>104.04.15<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>44528<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>104.04.15<td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>44529<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>104.04.15<td align=center BGCOLOR=CCFFFF>9<td align=center BGCOLOR=99FF99>44530<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>10<td align=center BGCOLOR=99FF99>104.04.29<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>44837<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>10<td align=center BGCOLOR=99FF99>104.04.29<td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>44847<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>10<td align=center BGCOLOR=99FF99>104.04.29<td align=center BGCOLOR=CCFFFF>9<td align=center BGCOLOR=99FF99>44848<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>11<td align=center BGCOLOR=99FF99>104.05.04<td align=center BGCOLOR=CCFFFF>2<td align=center BGCOLOR=99FF99>44943<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>楊士萱【201042】線性代數<BR>
<tr><td align=center BGCOLOR=CCFFFF>11<td align=center BGCOLOR=99FF99>104.05.06<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>45029<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>11<td align=center BGCOLOR=99FF99>104.05.06<td align=center BGCOLOR=CCFFFF>8<td align=center BGCOLOR=99FF99>45030<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>11<td align=center BGCOLOR=99FF99>104.05.06<td align=center BGCOLOR=CCFFFF>9<td align=center BGCOLOR=99FF99>45031<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>尤信程【201044】數位邏輯設計實習<BR>
<tr><td align=center BGCOLOR=CCFFFF>13<td align=center BGCOLOR=99FF99>104.05.21<td align=center BGCOLOR=CCFFFF>6<td align=center BGCOLOR=99FF99>45700<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>鄧碧珍【201034】體育<BR>
<tr><td align=center BGCOLOR=CCFFFF>13<td align=center BGCOLOR=99FF99>104.05.21<td align=center BGCOLOR=CCFFFF>7<td align=center BGCOLOR=99FF99>45701<td align=center BGCOLOR=CCFFFF>曠課<td align=left   BGCOLOR=99FF99>鄧碧珍【201034】體育<BR>
</table>
</blockquote>
 <img src=./image/or_ball.gif>103 學年度 第 1 學期
 <blockquote>
<table border=1>
<tr><th colspan=2 BGCOLOR=FFCCCC>獎懲統計表
<tr><th BGCOLOR=CCFFFF>類別<th BGCOLOR=99FF99>次數
<tr><td align=center BGCOLOR=CCFFFF>小功<td align=center BGCOLOR=99FF99>1
<tr><td align=center BGCOLOR=CCFFFF>申誡<td align=center BGCOLOR=99FF99>2
</table>
<P>
<table border=1>
<tr><th colspan=4 BGCOLOR=FFCCCC>獎懲詳細記錄
<tr><th BGCOLOR=CCFFFF>日期(年月日)<th BGCOLOR=99FF99>類別<th BGCOLOR=CCFFFF>次數<th BGCOLOR=99FF99>獎懲事實
<tr><td align=center BGCOLOR=CCFFFF>103.12.12<td align=center BGCOLOR=99FF99>小功<td align=center BGCOLOR=CCFFFF>1<td align=Left   BGCOLOR=99FF99>協助測試校園入口網站資訊安全問題
<tr><td align=center BGCOLOR=CCFFFF>103.12.12<td align=center BGCOLOR=99FF99>申誡<td align=center BGCOLOR=CCFFFF>1<td align=Left   BGCOLOR=99FF99>無故未參加資工系週會
<tr><td align=center BGCOLOR=CCFFFF>103.12.24<td align=center BGCOLOR=99FF99>申誡<td align=center BGCOLOR=CCFFFF>1<td align=Left   BGCOLOR=99FF99>無故未參加電資學院週會
</table>
</blockquote>
<blockquote>
<table border=1>
<tr><th colspan=2 BGCOLOR=FFCCCC>缺曠課、請假統計表
<tr><th BGCOLOR=CCFFFF>類別<th BGCOLOR=99FF99>次數
</table>
<P>
<table border=1>
<tr><th colspan=6 BGCOLOR=FFCCCC>缺曠課、請假詳細記錄</th></tr>
<tr><th BGCOLOR=CCFFFF>週次</th><th BGCOLOR=99FF99>日期(年月日)</th><th BGCOLOR=CCFFFF>節次</th><th BGCOLOR=99FF99>點名單號</th><th BGCOLOR=CCFFFF>類別</th><th BGCOLOR=99FF99>備註</th></tr>
<tr><td align=center colspan=6 BGCOLOR=CCFFFF>無
</table>
</blockquote>
  
<Center><Font size=4 color=red></Font></Center>
<P>備註：</P><OL>
<LI>今天是<FONT COLOR=#FF0000> 105.06.05</font>，
上列缺曠課、請假資料為<FONT COLOR=#FF0000> 105.06.02</font>前登錄。
<LI>本資料係由本校學務處、進修部、進修學院及計網中心所共同提供！</LI>
<LI>本資料僅供參考，正式資料仍以學務處、進修部、進修學院所公佈之書面資料為準。</LI>
<LI>對於上述資料，如有任何疑義，請備妥相關證明文件至各部處生(學)輔組查詢。
<!--
<LI>您是自97年10月20日以來第<IMG SRC="http://dbs-pp.cc.ntut.edu.tw/cgi-bin/cgiwrap/dbquery/db_abs_cntnew">人次查詢本頁。</LI></OL>
-->
</BODY>
</HTML>
 * */
