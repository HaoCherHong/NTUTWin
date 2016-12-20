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
                "<td>(?:<a href=\"[^\"]+\">(?<teachers>[^<]*)</a><br>\n)*\\s?\n" +
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
            detail.Hours = float.Parse(match.Groups[6].Value);
            detail.Credits = float.Parse(match.Groups[7].Value);
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

/*
<tr><td>四資三      <td><a href="Select.jsp?format=-2&code=102590045&year=104&sem=2">102590045</a><td>曾念宗<td><div align=center>撤選</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590044&year=103&sem=1">103590044</a><td>洪彻哲<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590045&year=103&sem=1">103590045</a><td>林煒淳<td><div align=center>　</div><td BGCOLOR=#FFCCCC>休學    
*/

//<A href="Teach.jsp?format=-3&year=103&sem=1&code=11733">林含怡</A><BR>
/*
<BODY bgcolor=#ffffdf background="../image/grey918y.gif" text=#003398>
<H2>選 課 表</H2>

<p><img src=../image/or_ball.gif> 課程基本資料：</p>
<table border=1>
<tr><th>課　　號<td>193043
<tr><th>學 年 度<td>103</tr>
<tr><th>學　　期<td>1</tr>
<tr><th>課程名稱
<td><A href="Curr.jsp?format=-2&code=1001001">體育</A>
<tr><th>階　　段<td>1</tr>
<tr><th>學　　分<td>0.0</tr>
<tr><th>時　　數<td>2</tr>
<tr><th>類　　別<td>△</tr>
<tr><th>授課教師
<td><A href="Teach.jsp?format=-3&year=103&sem=1&code=21904">鄧碧雲</A><BR>

<tr><th>開課班級
<td><A href="Subj.jsp?format=-4&year=103&sem=1&code=1842">四資一</A><BR>

<tr><th>教　　室
<td>　
<tr><th>修課人數<td>55</tr>
<tr><th>撤選人數<td>0</tr>
<tr><th>教學助理
<td>　
<tr><th>授課語言<td>　
<tr><th>隨班附讀<td>　
<tr><th>實驗、實習<td>　
<tr><th>備　　註<td>　
</table>

<BR>
<table border="0">
  <tr><td><form target="_blank" action="Select_2_Excel.jsp">
            <input type="hidden" name="code"       value="193043">
            <input type="submit" name="ToDownLoad" value="下載點名單 Excel 檔案">
          </form></td>
      <td width="30">　</td>
      <td><form target="_blank" action="Select_2_Csv.jsp">
            <input type="hidden" name="code"       value="193043">
            <input type="submit" name="ToDownLoad" value="下載點名單 CSV 檔案">
          </form></td>
      <td width="30">　</td>
      <td><form target="_blank"  action="Select_2_Print.jsp">
            <input type="hidden" name="code"    value="193043">
            每頁列印人數:
            <select name="psize">
              <option value="30" selected>30人</option>
              <option value="35"         >35人</option>
              <option value="40"         >40人</option>
              <option value="45"         >45人</option>
              <option value="50"         >50人</option>
            </select>
            <input type="submit" name="ToPrint" value="列印點名單"><BR>
            <font color="#FF0000">※若因資料內容太長而影響版面美觀，請使用瀏覽器之【設定列印格式】功能，將邊界值設定至最小(或10mm以下)。</font>
          </form></td>
  </tr>
</table>
<p><img src=../image/or_ball.gif> 選課名單：</p>
<table border=1>
<tr><th>班級<th>學號<th>姓名<th>選課狀況<th>在學狀況
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590001&year=103&sem=1">103590001</a><td>劉柏宏<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590002&year=103&sem=1">103590002</a><td>洪子軒<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590003&year=103&sem=1">103590003</a><td>蔡庭豪<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590004&year=103&sem=1">103590004</a><td>林可均<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590005&year=103&sem=1">103590005</a><td>莊艾潔<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590006&year=103&sem=1">103590006</a><td>曹暘鑫<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590007&year=103&sem=1">103590007</a><td>蔡欣倪<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590008&year=103&sem=1">103590008</a><td>邱彥淳<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590009&year=103&sem=1">103590009</a><td>張凱淯<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590010&year=103&sem=1">103590010</a><td>施𠗟翎<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590011&year=103&sem=1">103590011</a><td>蔡宇傑<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590012&year=103&sem=1">103590012</a><td>洪儷軒<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590013&year=103&sem=1">103590013</a><td>江紀武<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590014&year=103&sem=1">103590014</a><td>洪國竣<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590015&year=103&sem=1">103590015</a><td>林俊耀<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590016&year=103&sem=1">103590016</a><td>蔡宗穎<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590017&year=103&sem=1">103590017</a><td>張霽<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590018&year=103&sem=1">103590018</a><td>林家禾<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590019&year=103&sem=1">103590019</a><td>黃麟傑<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590020&year=103&sem=1">103590020</a><td>盧承毅<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590021&year=103&sem=1">103590021</a><td>林冠璋<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590022&year=103&sem=1">103590022</a><td>邱子源<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590023&year=103&sem=1">103590023</a><td>賴俊豪<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590024&year=103&sem=1">103590024</a><td>郭科臣<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590025&year=103&sem=1">103590025</a><td>洪俊銘<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590026&year=103&sem=1">103590026</a><td>周冠勳<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590027&year=103&sem=1">103590027</a><td>王宗昱<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590028&year=103&sem=1">103590028</a><td>簡育聲<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590029&year=103&sem=1">103590029</a><td>朱信哲<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590030&year=103&sem=1">103590030</a><td>劉顥旻<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590031&year=103&sem=1">103590031</a><td>李印君<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590032&year=103&sem=1">103590032</a><td>蕭家皓<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590033&year=103&sem=1">103590033</a><td>趙映翔<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590034&year=103&sem=1">103590034</a><td>廖宣瑋<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590035&year=103&sem=1">103590035</a><td>徐昱仁<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590036&year=103&sem=1">103590036</a><td>曾雋恩<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590037&year=103&sem=1">103590037</a><td>李孟霖<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590038&year=103&sem=1">103590038</a><td>陳郁欣<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590039&year=103&sem=1">103590039</a><td>闕瑋霆<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590040&year=103&sem=1">103590040</a><td>林宗諭<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590041&year=103&sem=1">103590041</a><td>郭晉名<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590042&year=103&sem=1">103590042</a><td>劉瑋倫<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590043&year=103&sem=1">103590043</a><td>蔡仁凱<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590044&year=103&sem=1">103590044</a><td>洪彻哲<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590045&year=103&sem=1">103590045</a><td>林煒淳<td><div align=center>　</div><td BGCOLOR=#FFCCCC>休學                
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590046&year=103&sem=1">103590046</a><td>鄭鴻仁<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590047&year=103&sem=1">103590047</a><td>曾俊霖<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590048&year=103&sem=1">103590048</a><td>阮寶瑛秀<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590050&year=103&sem=1">103590050</a><td>徐俊振<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590051&year=103&sem=1">103590051</a><td>李善程<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590052&year=103&sem=1">103590052</a><td>陳巧宜<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590053&year=103&sem=1">103590053</a><td>溫翠倩<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590054&year=103&sem=1">103590054</a><td>黃子洋<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590055&year=103&sem=1">103590055</a><td>蕭家朗<td><div align=center>　</div><td>　
<tr><td>四資一      <td><a href="Select.jsp?format=-2&code=103590056&year=103&sem=1">103590056</a><td>王裕權<td><div align=center>　</div><td>　
</table>


<P>備註：<input type="button" onClick="history.back()" value="回上一頁"></P><OL>
<LI>本資料係由本校各教學單位、教務處課務組、進修部教務組、進修學院教務組及計網中心所共同提供！</LI>
<LI>本資料僅供參考，正式資料仍以教務處、進修部、進修學院所公佈之書面資料為準。</LI>

<!--
<LI>您是自86年9月9日以來第<IMG SRC="/cgi-bin/cgiwrap/dbquery/db_select_counter">人次查詢本頁。</LI></OL>
-->
</OL>
 

</BODY>
</HTML>
*/
