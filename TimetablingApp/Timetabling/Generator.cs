using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Timetabling
{
    internal delegate void ProgressEvent(float progress);

    class Generator
    {
        public event ProgressEvent ProgressUpdate;

        public List<List<ScheduledClass>> GenPermutations(IEnumerable<ClassInfo> classInfos, int maxClashes = 0)
        {
            var slots = new byte[24 * 4 * 5];//15 min slots over the 5 day class period
            
            numPredicted = PossiblePermutationsCount(classInfos);
            numGenerated = 0;
            generationMaxClashes = maxClashes;

            //Order classes so that we start from least number of possible choices to most
            //Allows unresolvable clashes to occur early, so generation will not create unnecessary classes.
            List<ClassInfo> sortedClassInfos = classInfos.OrderBy(ci => ci.ScheduledClasses.Count).ToList();


            //Make sure we don't reach a point where a memory error is likely to occur
            //Set 128MB as a safe limit
            //An InsufficientMemoryException will be raised if this fails
            MemoryFailPoint failPoint = new MemoryFailPoint(128);

            var classes = genPermutations(classInfos.ToList(), slots, 0);

            return classes;
        }

        public int PossiblePermutationsCount(IEnumerable<ClassInfo> classInfos)
        {
            int prod = 1;
            foreach (var classInfo in classInfos)
                prod *= classInfo.ScheduledClasses.Count;

            return prod;
        }

        private int generationMaxClashes = 0;
        private int numPredicted = 0;
        private int numGenerated = 0;

        private List<List<ScheduledClass>> genPermutations(List<ClassInfo> classInfos, byte[] slots, int depth)
        {
            numGenerated++;

            var permutations = new List<List<ScheduledClass>>();
            foreach (var scheduledClass in classInfos[depth].ScheduledClasses)
            {
                //If we have too many clashes, give up
                if (numClashes(scheduledClass, slots) > generationMaxClashes)
                    continue;

                //Check if we're at the base case (at the final classInfo)
                if (depth != classInfos.Count - 1)
                {
                    //If we're not, generate sub-permutations recursively
                    var depthPerms = genPermutations(classInfos, updateSlots(scheduledClass, slots), depth + 1);
                    
                    //Try and update our progress (flaky, could be improved)
                    ProgressUpdate?.Invoke((float)numGenerated / numPredicted);

                    //Add our class to each of the generated sub-permutations, then add those sub-permutations to our final list of permutations
                    foreach (var depthPerm in depthPerms)
                    {
                        depthPerm.Add(scheduledClass);
                        permutations.Add(depthPerm);
                    }
                }
                else
                {
                    //We're at the end, so we have one possibility for our permutation: just the current class
                    permutations.Add(new List<ScheduledClass> { scheduledClass });
                }
            }

            //Return our new list
            return permutations;
        }

        public IEnumerable<Timetable> SortPermutations(IEnumerable<List<ScheduledClass>> permutations, bool laterStarts, bool lessDays)
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

        public IEnumerable<Timetable> SortClashedPermutations(IEnumerable<List<ScheduledClass>> permutations, bool laterStarts, bool lessDays)
        {
            var timetables = permutations.Select(analysePermutation);

            if (laterStarts)
            {
                if (lessDays)
                    return timetables.OrderBy(t => t.NumberClashes)
                        .ThenBy(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
                else
                    return timetables.OrderBy(t=>t.NumberClashes)
                        .ThenByDescending(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
            }
            else
            {
                if (lessDays)
                    return timetables.OrderBy(t => t.NumberClashes)
                        .ThenBy(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);
                else
                    return timetables.OrderBy(t=>t.NumberClashes)
                        .ThenByDescending(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);

            }
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
