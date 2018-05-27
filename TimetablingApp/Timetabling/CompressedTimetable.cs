using System;
using System.Collections.Generic;

namespace Timetabling
{
    public class CompressedTimetable
    {
        public CompressedTimetable(Timetable timetable)
        {
            Classes = new List<CompressedClass>();

            foreach (var scheduledClass in timetable.Classes)
            {
                CompressedClass compressedClass = new CompressedClass
                {
                    ID = scheduledClass.ClassInfo.ID,
                    Start = scheduledClass.TimeStart,
                    End = scheduledClass.TimeEnd
                };

                Classes.Add(compressedClass);
            }
        }


        public List<CompressedClass> Classes { get; set; }
    }

    public class CompressedClass
    {
        public int ID { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}
