using System.Collections.Generic;
using System.Linq;
using Timetabling;

namespace TimetablingApp.Models
{
    public class TimetableBuildResultModel
    {
        public TimetableBuildResultModel(List<Timetable> timetables,
            int timetablesGenerated,
            List<ScheduledClass> allScheduledClasses,
            List<ClassInfo> originalClassInfos)
        {
            //Assign unique ids to each of the 'original' class infos, allowing for compression to work later, and same for scheduled classes
            int id = 0;
            foreach (var classInfo in originalClassInfos)
                classInfo.ID = id++;
            id = 0;
            foreach (var scheduledClass in allScheduledClasses)
                scheduledClass.ID = id++;


            //Build the bi-directional neighbour class listings
            foreach (var scheduledClass in allScheduledClasses)
            {
                scheduledClass.NeighbourClassIDs = scheduledClass.ChildClasses.Select(s => s.ID).ToList();

                foreach (var childClass in scheduledClass.ChildClasses)
                {
                    var otherChildIDs = scheduledClass.NeighbourClassIDs.Except(new[] { childClass.ID });

                    childClass.NeighbourClassIDs = otherChildIDs.Append(scheduledClass.ID).ToList();
                }
            }

            Timetables = compressTimetables(timetables);
            TimetablesGenerated = timetablesGenerated;
            AllScheduledClasses = allScheduledClasses;
            OriginalClassInfos = originalClassInfos;
        }

        public List<List<int>> Timetables { get; set; }
        public List<ScheduledClass> AllScheduledClasses { get; set; }
        public List<ClassInfo> OriginalClassInfos { get; set; }

        public int TimetablesGenerated { get; set; }

        private static List<List<int>> compressTimetables(List<Timetable> timetables)
        {
            //Builds a list of lists of class ids
            return timetables.Select(t => t.Classes.Select(c => c.ID).ToList()).ToList();
        }
    }
}
