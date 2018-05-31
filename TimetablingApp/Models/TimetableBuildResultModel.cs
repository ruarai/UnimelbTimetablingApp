using System.Collections.Generic;
using System.Linq;
using Timetabling;

namespace TimetablingApp.Models
{
    public class TimetableBuildResultModel
    {
        public TimetableBuildResultModel(List<Timetable> timetables, List<ScheduledClass> allScheduledClasses, List<ClassInfo> originalClassInfos)
        {
            Timetables = compressTimetables(timetables);
            AllScheduledClasses = allScheduledClasses;
            OriginalClassInfos = originalClassInfos;
        }

        public List<List<int>> Timetables { get; set; }
        public List<ScheduledClass> AllScheduledClasses { get; set; }
        public List<ClassInfo> OriginalClassInfos { get; set; }

        private static List<List<int>> compressTimetables(List<Timetable> timetables)
        {
            //Builds a list of lists of class ids
            return timetables.Select(t => t.Classes.Select(c => c.ID).ToList()).ToList();
        }
    }
}
