using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Timetable
{
    internal delegate void ProgressEvent(float progress);

    class Generator
    {
        public event ProgressEvent ProgressUpdate;

        public int MaxClash = 0;
        public List<List<ScheduledClass>> GenPermutations(List<ClassInfo> classInfos)
        {
            var slots = new byte[24 * 4 * 5];//15 min slots over the 5 day class period
            
            int prod = 1;
            foreach (var classInfo in classInfos)
                prod *= classInfo.ScheduledClasses.Count;

            numPredicted = prod;

            return genPermutations(classInfos, slots, 0);
        }

        private int numPredicted = 0;

        private List<List<ScheduledClass>> genPermutations(List<ClassInfo> classInfos, byte[] slots, int depth)
        {
            int i = 0;
            var permutations = new List<List<ScheduledClass>>();
            foreach (var scheduledClass in classInfos[depth].ScheduledClasses)
            {
                if (numClashes(scheduledClass, slots) > MaxClash)
                    continue;

                if (depth != classInfos.Count - 1)
                {
                    var depthPerms = genPermutations(classInfos, updateSlots(scheduledClass, slots), depth + 1);
                    
                    ProgressUpdate?.Invoke((float)permutations.Count / numPredicted);

                    foreach (var depthPerm in depthPerms)
                    {
                        depthPerm.Add(scheduledClass);
                        permutations.Add(depthPerm);
                    }
                }
                else
                {
                    permutations.Add(new List<ScheduledClass> { scheduledClass });
                }
            }

            return permutations;
        }

        public IEnumerable<Timetable> SortPermutations(List<List<ScheduledClass>> permutations, bool laterStarts, bool lessDays)
        {
            var timetables = permutations.Select(analysePermutation);

            if (laterStarts)
            {
                if(lessDays)
                    return timetables.OrderBy(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
                else
                    return timetables.OrderByDescending(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
            }
            else
            {
                if(lessDays)
                    return timetables.OrderBy(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);
                else
                    return timetables.OrderByDescending(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);

            }

        }

        public IEnumerable<Timetable> SortClashedPermutations(List<List<ScheduledClass>> permutations)
        {
            var timetables = permutations.Select(analysePermutation);

            return
                timetables.OrderBy(t => t.NumberClashes).ThenBy(t => t.NumberDaysClasses)
                    .ThenByDescending(t => t.AverageStartTime);
        }

        private Timetable analysePermutation(List<ScheduledClass> permutation)
        {
            Timetable t = new Timetable(permutation);

            long startTimes = 0;
            long endTimes = 0;

            for (int i = 1; i < 6; i++)
            {
                var classes = t.Classes.Where(c => (int)c.TimeStart.DayOfWeek == i).OrderBy(c => c.TimeStart);

                if (classes.Any())
                {
                    t.NumberDaysClasses++;

                    startTimes += classes.First().TimeStart.TimeOfDay.Ticks;
                    endTimes += classes.Last().TimeEnd.TimeOfDay.Ticks;
                }
            }

            t.AverageStartTime = startTimes / t.NumberDaysClasses;
            t.AverageEndTime = endTimes / t.NumberDaysClasses;


            return t;
        }



        private byte[] updateSlots(ScheduledClass scheduledClass, byte[] slots)
        {
            var newSlots = new byte[24 * 4 * 5];
            Array.Copy(slots, newSlots, slots.Length);

            var classes = new List<ScheduledClass> { scheduledClass }.Concat(scheduledClass.ChildClasses);

            foreach (var childClass in classes)
            {
                for (int i = childClass.SlotStart; i < childClass.SlotEnd; i++)
                {
                    newSlots[i]++;
                }
            }
            return newSlots;
        }

        private int numClashes(ScheduledClass scheduledClass, byte[] slots)
        {
            var classes = new List<ScheduledClass> { scheduledClass }.Concat(scheduledClass.ChildClasses);

            int clashes = 0;

            foreach (var childClass in classes)
            {
                for (int i = childClass.SlotStart; i < childClass.SlotEnd; i++)
                {
                    if (slots[i] > 0)
                    {
                        clashes++;
                        break;
                    }
                }
            }
            return clashes;
        }


    }
}
