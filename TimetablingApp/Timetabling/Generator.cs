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

        public CancellationToken CancellationToken { get; set; }

        public List<Timetable> GeneratePermutationsExpanding(IEnumerable<ClassInfo> classInfos)
        {
            //Order classes so that we start from least number of possible choices to most
            //Allows unresolvable clashes to occur early, so generation will not create unnecessary classes.
            classInfos = classInfos.OrderBy(ci => ci.ScheduledClasses.Count);

            //preferred class time we will expand from, 9am and forwards if we prefer early starts, 5pm and before if we prefer later
            int preferredTime = SortLaterStarts ? 17 * 4 : 9 * 4;

            //Time in hours from the preferred time we will try to schedule within
            int period = 0;

            List<ScheduledClass> classes = classInfos.SelectMany(ci => ci.ScheduledClasses).ToList();
            classes.ForEach(c => c.DoTimetable = false);
            List<Timetable> timetables = new List<Timetable>();

            generationMaxClashes = 0;
            while (!timetables.Any() || !classesValid(classInfos) || !timetablesSpacedOut(timetables, period))
            {
                period += 4;//Add an hour

                if (period > 24 * 4 - preferredTime)
                {
                    period = 4;
                    generationMaxClashes++;
                }

                if (generationMaxClashes > 4)
                    break;

                bool[] allowedSlots = new bool[24 * 4 * 5];//15 min slots over the 5 day class period
                for (int day = 0; day < 5; day++)
                {
                    //Calculate the first slot of the day
                    int daySlot = day * 24 * 4;

                    //Starting from our preferred time, set our desired periods to true
                    if(SortLaterStarts)
                    {
                        for (int quarters = preferredTime - period; quarters < preferredTime; quarters++)
                            allowedSlots[daySlot + quarters] = true;
                    }
                    else
                    {
                        for (int quarters = preferredTime; quarters < preferredTime + period; quarters++)
                            allowedSlots[daySlot + quarters] = true;
                    }
                }

                //Make sure each class is only timetabled if it is within our new period, or if there is no actual choice for this class (1 option)
                foreach (var c in classes)
                {
                    c.DoTimetable = (allowedSlots[c.SlotStart] && allowedSlots[c.SlotEnd]) || c.OnlyChoice;
                }

                if (!classesValid(classInfos))
                    continue;

                var slots = new byte[24 * 4 * 5];
                var permutations = genPermutations(classInfos.ToList(), slots, 0);

                if (permutations == null)
                    continue;

                permutations = permutations.Where(p => permutationValid(p, classInfos)).ToList();

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
                var permutations = generateBruteForce(classInfos.ToList());
                if (permutations != null)
                    timetables = sortClashedPermutations(permutations).ToList();

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
        
        //Returns if a given list of timetables has at least one timetable where a day is not fully scheduled
        private bool timetablesSpacedOut(List<Timetable> timetables, int period)
        {
            foreach (var t in timetables)
            {
                bool timetableSpaced = true;

                for (int day = 1; day <= 5; day++)
                {
                    var classesOnDay = t.Classes.Where(c => (int)c.TimeStart.DayOfWeek == day).OrderBy(c => c.TimeStart);

                    if (!classesOnDay.Any())
                        continue;

                    int earliestSlot = classesOnDay.First().SlotStart;

                    int latestSlot = classesOnDay.Last().SlotEnd;

                    if (latestSlot - earliestSlot == period)
                        timetableSpaced = false;
                }

                if (timetableSpaced)
                    return true;
            }

            return false;
        }


        private List<List<ScheduledClass>> generateBruteForce(IEnumerable<ClassInfo> classInfos)
        {
            var slots = new byte[24 * 4 * 5];//15 min slots over the 5 day class period

            //Order classes so that we start from least number of possible choices to most
            //Allows unresolvable clashes to occur early, so generation will not create unnecessary classes.
            classInfos = classInfos.OrderBy(ci => ci.ScheduledClasses.Count);
            
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

        private int generationMaxClashes;

        private List<List<ScheduledClass>> genPermutations(List<ClassInfo> classInfos, byte[] slots, int depth)
        {
            var permutations = new List<List<ScheduledClass>>();
            foreach (var scheduledClass in classInfos[depth].ScheduledClasses)
            {
                //If the request is cancelled, throw an exception upstream
                CancellationToken.ThrowIfCancellationRequested();

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

            //Return our new list
            return !permutations.Any() ? null : permutations;
        }

        private IEnumerable<Timetable> sortClashedPermutations(IEnumerable<List<ScheduledClass>> permutations)
        {
            var timetables = permutations.Select(p => new Timetable(p));

            var clashes = new Func<Timetable, long>(t => t.NumberClashes);
            var days = new Func<Timetable, byte>(t => t.NumberDaysClasses);
            var startTime = new Func<Timetable, long>(t => t.AverageStartTime);

            var clashOrder = timetables.OrderBy(clashes);
            
            if (SortLaterStarts)
            {
                if (SortLessDays)
                    return clashOrder
                        .ThenBy(days)
                        .ThenByDescending(startTime);
                else
                    return clashOrder
                        .ThenByDescending(days)
                        .ThenByDescending(startTime);
            }
            else
            {
                if (SortLessDays)
                    return clashOrder
                        .ThenBy(days)
                        .ThenBy(startTime);
                else
                    return clashOrder
                        .ThenByDescending(days)
                        .ThenBy(startTime);
            }
        }
        
        private static bool permutationValid(List<ScheduledClass> permutation, IEnumerable<ClassInfo> classInfos)
        {
            //Make sure every classInfo in classInfos has a corresponding scheduled class in our permutation
            //That is, every class to be scheduled is scheduled.
            return classInfos.All(classInfo => permutation.Any(p => classInfo.ScheduledClasses.Contains(p)));
        }
        
        //Adds occupancy to slots for some new scheduledClass
        private static byte[] updateSlots(ScheduledClass scheduledClass, byte[] slots)
        {
            var newSlots = new byte[24 * 4 * 5];
            Array.Copy(slots, newSlots, slots.Length);

            //Don't care about classes we have no choice about clashing
            if (scheduledClass.OnlyChoice)
                return newSlots;

            var classes = new List<ScheduledClass> { scheduledClass }.Concat(scheduledClass.ChildClasses);

            foreach (var childClass in classes)
            {
                for (int i = childClass.SlotStart; i < childClass.SlotEnd; i++)
                    newSlots[i]++;
            }
            return newSlots;
        }

        //Calculate the total number of clashes that will occur after adding some scheduledClass on slots
        private static int numClashes(ScheduledClass scheduledClass, byte[] slots)
        {
            var classes = new List<ScheduledClass> { scheduledClass }.Concat(scheduledClass.ChildClasses);

            //Don't care about classes we have no choice about clashing
            if (scheduledClass.OnlyChoice)
                return 0;

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
