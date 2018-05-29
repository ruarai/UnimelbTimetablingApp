using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Timetabling
{

    class Generator
    {
        public bool SortLaterStarts { get; set; }
        public bool SortLessDays { get; set; }

        public List<Timetable> GeneratePermutationsExpanding(IEnumerable<ClassInfo> classInfos)
        {
            //preferred class time we will expand from
            int preferredTime = 9 * 4;
            //Time in hours from the preferred time we will try to schedule within
            int period = 0;

            //Allow for expanding beyond what is necessary to get better results
            int forceMore = 0;

            List<ScheduledClass> classes = classInfos.SelectMany(ci => ci.ScheduledClasses).ToList();
            classes.ForEach(c => c.DoTimetable = false);
            List<Timetable> timetables = new List<Timetable>();

            generationMaxClashes = 0;
            while (!timetables.Any() || !classesValid(classInfos) || (timetables.Count < 25000 && forceMore++ < 2))
            {
                period += 4;//Take an hour

                if(period > 24 * 4 - preferredTime)
                {
                    period = 4;
                    generationMaxClashes++;
                }

                if (generationMaxClashes > 4)
                    break;

                //array of days we'll try to schedule on
                bool[] days = new bool[] { true, true, true, true, true };

                bool[] allowedSlots = new bool[24 * 4 * 5];//15 min slots over the 5 day class period
                for (int day = 0; day < 5; day++)
                {
                    //Calculate the first slot of the day
                    int daySlot = day * 24 * 4;

                    //Check iff we want to schedule on this day
                    if (days[day])
                    {
                        //Starting from our preferred time, set our desired periods to true
                        for (int quarters = preferredTime; quarters < preferredTime + period; quarters++)
                            allowedSlots[daySlot + quarters] = true;

                    }
                }

                //Make sure each class is only timetabled if it is within our new period
                classes.ForEach(c => c.DoTimetable = (allowedSlots[c.SlotStart] && allowedSlots[c.SlotEnd]));

                if (!classesValid(classInfos))
                    continue;

                var slots = new byte[24 * 4 * 5];
                var permutations = genPermutations(classInfos.ToList(), slots, 0);

                if (permutations == null)
                    continue;

                permutations = permutations.Where(p => permutationValid(p, classInfos)).ToList();

                if (generationMaxClashes == 0)
                    timetables = sortPermutations(permutations).ToList();
                else
                    timetables = sortClashedPermutations(permutations).ToList();
            }

            return timetables;
        }

        public List<Timetable> GenerateTimetablesBruteForce(IEnumerable<ClassInfo> classInfos)
        {
            List<Timetable> timetables = new List<Timetable>();

            classInfos.SelectMany(ci => ci.ScheduledClasses).ToList().ForEach(c => c.DoTimetable = true);

            generationMaxClashes = 0;
            while (!timetables.Any())
            {
                if (generationMaxClashes == 0)
                {
                    var permutations = generateBruteForce(classInfos.ToList());
                    if(permutations != null)
                        timetables = sortPermutations(permutations).ToList();
                }
                else
                {
                    var permutations = generateBruteForce(classInfos.ToList());
                    if (permutations != null)
                        timetables = sortClashedPermutations(permutations).ToList();
                }

                generationMaxClashes++;
            }

            return timetables;
        }

        private bool classesValid(IEnumerable<ClassInfo> classInfos)
        {
            //For every classInfo,
            foreach (var classInfo in classInfos)
            {
                if (!classInfo.ScheduledClasses.Any(scheduledClass => scheduledClass.DoTimetable))
                    return false;//If not, we're missing a required class
            }
            return true;
        }


        private List<List<ScheduledClass>> generateBruteForce(IEnumerable<ClassInfo> classInfos)
        {
            var slots = new byte[24 * 4 * 5];//15 min slots over the 5 day class period

            numPredicted = PossiblePermutationsCount(classInfos);
            numGenerated = 0;

            //Order classes so that we start from least number of possible choices to most
            //Allows unresolvable clashes to occur early, so generation will not create unnecessary classes.
            List<ClassInfo> sortedClassInfos = classInfos.OrderBy(ci => ci.ScheduledClasses.Count).ToList();

            
            var classes = genPermutations(classInfos.ToList(), slots, 0);

            return classes;
        }

        public static long PossiblePermutationsCount(IEnumerable<ClassInfo> classInfos)
        {
            long prod = 1;
            foreach (var classInfo in classInfos)
                prod *= classInfo.ScheduledClasses.Count;

            return prod;
        }

        private int generationMaxClashes = 0;
        private long numPredicted = 0;
        private long numGenerated = 0;

        private List<List<ScheduledClass>> genPermutations(List<ClassInfo> classInfos, byte[] slots, int depth)
        {
            numGenerated++;

            var permutations = new List<List<ScheduledClass>>();
            foreach (var scheduledClass in classInfos[depth].ScheduledClasses)
            {
                //Don't schedule ones we don't want to
                if (!scheduledClass.DoTimetable)
                    continue;

                //If we have too many clashes, give up
                if (numClashes(scheduledClass, slots) > generationMaxClashes)
                    continue;

                //Check if we're at the base case (at the final classInfo)
                if (depth != classInfos.Count - 1)
                {
                    //If we're not, generate sub-permutations recursively
                    var depthPerms = genPermutations(classInfos, updateSlots(scheduledClass, slots), depth + 1);

                    //Our permutation is no good, continue
                    if (depthPerms == null)
                        continue;

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

            if (!permutations.Any())
                return null;

            //Return our new list
            return permutations;
        }

        private IEnumerable<Timetable> sortPermutations(IEnumerable<List<ScheduledClass>> permutations)
        {
            var timetables = permutations.Select(analysePermutation);

            if (SortLaterStarts)
            {
                if (SortLessDays)
                    return timetables.OrderBy(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
                else
                    return timetables.OrderByDescending(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
            }
            else
            {
                if (SortLessDays)
                    return timetables.OrderBy(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);
                else
                    return timetables.OrderByDescending(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);

            }
        }

        private IEnumerable<Timetable> sortClashedPermutations(IEnumerable<List<ScheduledClass>> permutations)
        {
            var timetables = permutations.Select(analysePermutation);

            if (SortLaterStarts)
            {
                if (SortLessDays)
                    return timetables.OrderBy(t => t.NumberClashes)
                        .ThenBy(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
                else
                    return timetables.OrderBy(t => t.NumberClashes)
                        .ThenByDescending(t => t.NumberDaysClasses)
                        .ThenByDescending(t => t.AverageStartTime);
            }
            else
            {
                if (SortLessDays)
                    return timetables.OrderBy(t => t.NumberClashes)
                        .ThenBy(t => t.NumberDaysClasses)
                        .ThenBy(t => t.AverageStartTime);
                else
                    return timetables.OrderBy(t => t.NumberClashes)
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

        private bool permutationValid(List<ScheduledClass> permutation, IEnumerable<ClassInfo> classInfos)
        {
            //Make sure every classInfo in classInfos has a corresponding scheduled class in our permutation
            //That is, every class to be scheduled is scheduled.
            foreach (var classInfo in classInfos)
            {
                if (!permutation.Any(p => classInfo.ScheduledClasses.Contains(p)))
                    return false;
            }
            return true;
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
