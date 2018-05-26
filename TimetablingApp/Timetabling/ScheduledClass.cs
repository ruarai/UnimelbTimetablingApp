using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Timetabling
{
    public class ScheduledClass
    {
        public ScheduledClass(DateTime timeStart, DateTime timeEnd)
        {
            TimeStart = timeStart;
            TimeEnd = timeEnd;
        }
        
        public Subject ParentSubject => ClassInfo.ParentSubject;
        public string ClassName => ClassInfo.ClassName;
        public string ClassDescription => ClassInfo.ClassName.Split(" ")[0];//eg, Lecture, Tutorial

        public ClassInfo ClassInfo { get; set; }

        [JsonIgnore]
        public string Location { get; set; }

        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }

        [JsonIgnore]
        public short SlotStart { get { return dateToSlot(TimeStart); } }
        [JsonIgnore]
        public short SlotEnd { get { return dateToSlot(TimeEnd); } }

        [JsonIgnore]
        public TimeSpan Duration => TimeEnd - TimeStart;
        [JsonIgnore]
        public short ClassNumber { get; set; }

        [JsonIgnore]
        public bool DoTimetable { get; set; } = false;

        public List<ScheduledClass> ChildClasses { get; set; } = new List<ScheduledClass>();
        //Classes that are of the same number, e.g. stream lectures
        //These classes are dependent entirely upon the scheduling of this class.

        

        private static short dateToSlot(DateTime dt)
        {
            int day = (int)dt.DayOfWeek - 1;//Day indicating 0 for monday, 4 for friday.

            int hour = dt.Hour;//Hour between 0 and 23

            int quarter = dt.Minute / 15;//Quarter of hours between 0 and 3

            return (short)(day * 24 * 4 + hour * 4 + quarter);
        }
    }
}
