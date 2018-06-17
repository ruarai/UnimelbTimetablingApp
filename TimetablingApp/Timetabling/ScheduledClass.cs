using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Timetabling
{
    public class ScheduledClass
    {
        public ScheduledClass(DateTime timeStart, DateTime timeEnd)
        {
            TimeStart = timeStart;
            TimeEnd = timeEnd;
        }
        
        [JsonIgnore]
        public Subject ParentSubject => ClassInfo.ParentSubject;

        [JsonIgnore]
        public ClassInfo ClassInfo { get; set; }

        [JsonIgnore]
        public string Location { get; set; }
        
        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }

        [JsonIgnore]
        public bool OnlyChoice => ClassInfo.ScheduledClasses.Count == 1;

        [JsonIgnore]
        public short SlotStart => dateToSlot(TimeStart);
        [JsonIgnore]
        public short SlotEnd => dateToSlot(TimeEnd);

        [JsonIgnore]
        public short ClassNumber { get; set; }

        [JsonIgnore]
        public bool DoTimetable { get; set; } = false;

        //Classes that are of the same number, e.g. stream lectures
        //These classes are dependent entirely upon the scheduling of this class.
        [JsonIgnore]
        public List<ScheduledClass> ChildClasses { get; set; } = new List<ScheduledClass>();
        
        public int ID { get; set; }
        public int ClassInfoID => ClassInfo.ID;

        

        private static short dateToSlot(DateTime dt)
        {
            int day = (int)dt.DayOfWeek - 1;//Day indicating 0 for monday, 4 for friday.

            int hour = dt.Hour;//Hour between 0 and 23

            int quarter = dt.Minute / 15;//Quarter of hours between 0 and 3

            return (short)(day * 24 * 4 + hour * 4 + quarter);
        }
    }
}
