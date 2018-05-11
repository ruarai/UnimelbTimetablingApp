using System.Collections.Generic;
using System.Linq;

namespace Timetable
{
    public class Timetable
    {
        public Timetable(List<ScheduledClass> permutation)
        {
            int size = permutation.Count + permutation.Sum(c => c.ChildClasses.Count);

            Classes = new ScheduledClass[size];

            int i = 0;
            foreach (var scheduledClass in permutation)
            {
                Classes[i] = scheduledClass;
                i++;
                foreach (var childClass in scheduledClass.ChildClasses)
                {
                    Classes[i] = childClass;
                    i++;
                }
            }
        }


        public ScheduledClass[] Classes { get; set; }
        public long AverageStartTime { get; set; }
        public long AverageEndTime { get; set; }
        public byte NumberDaysClasses { get; set; }
        public long NumberClashes {
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
