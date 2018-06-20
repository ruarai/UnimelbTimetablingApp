using System.Collections.Generic;
using System.Linq;

namespace Timetabling
{
    public class Timetable
    {
        public Timetable(List<ScheduledClass> permutation)
        {
            int size = permutation.Count + permutation.Sum(c => c.ChildClasses.Count);

            Classes = new ScheduledClass[size];

            //Fill the classes array, including all child classes
            int j = 0;
            foreach (var scheduledClass in permutation)
            {
                scheduledClass.NeighbourClassIDs = scheduledClass.ChildClasses.Select(s => s.ID).ToList();

                Classes[j] = scheduledClass;
                j++;
                foreach (var childClass in scheduledClass.ChildClasses)
                {
                    var otherChildIDs = scheduledClass.NeighbourClassIDs.Except(new [] {childClass.ID});

                    childClass.NeighbourClassIDs = otherChildIDs.Append(scheduledClass.ID).ToList();

                    Classes[j] = childClass;
                    j++;
                }
            }

            //Calculate average start time and total number of days
            long startTimes = 0;
            for (int i = 1; i <= 5; i++)
            {
                var classes = Classes.Where(c => (int)c.TimeStart.DayOfWeek == i).OrderBy(c => c.TimeStart);

                if (classes.Any())
                {
                    NumberDaysClasses++;
                    startTimes += classes.First().TimeStart.TimeOfDay.Ticks;
                }
            }

            AverageStartTime = startTimes / NumberDaysClasses;
        }


        public ScheduledClass[] Classes { get; set; }
        public long AverageStartTime { get; set; }
        public byte NumberDaysClasses { get; set; }
        public int NumberClashes {
            get
            {
                var slots = new int[24 * 4 * 5];
                foreach (var scheduledClass in Classes)
                {
                    for (int i = scheduledClass.SlotStart; i < scheduledClass.SlotEnd; i++)
                        slots[i]++;
                }

                int clashes = 0;

                for (int i = 0; i < 24 * 4 * 5; i++)
                    clashes += slots[i] > 0 ? slots[i] - 1 : 0;

                return clashes;
            }
        }

    }
}
