using System;
using System.Collections.Generic;
using System.Text;

namespace NTUTWin
{
    class Semester
    {
        public Semester(int year, int semester)
        {
            Year = year;
            SemesterNumber = semester;
        }


        public override string ToString()
        {
            return string.Format("{0}年第{1}學期", Year, SemesterNumber);
        }

        public int Year { get;  set; }
        public int SemesterNumber { get; set; }
    }
}
