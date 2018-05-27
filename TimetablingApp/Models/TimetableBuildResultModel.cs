using System.Collections.Generic;
using Timetabling;

namespace TimetablingApp.Models
{
    public class TimetableBuildResultModel
    {
        public TimetableBuildResultModel(List<CompressedTimetable> timetables, List<ClassInfo> originalClassInfos)
        {
            Timetables = timetables;
            OriginalClassInfos = originalClassInfos;
        }
        public List<CompressedTimetable> Timetables { get; set; }
        public List<ClassInfo> OriginalClassInfos { get; set; }
    }
}
