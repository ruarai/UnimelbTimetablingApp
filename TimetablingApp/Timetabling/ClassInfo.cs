using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Timetabling
{
    public class ClassInfo
    {
        public string ClassName { get; set; }

        [JsonIgnore]
        public List<ScheduledClass> ScheduledClasses { get; set; }

        [JsonIgnore]
        public IEnumerable<ScheduledClass> AllScheduledClasses =>
            ScheduledClasses.SelectMany(s => s.ChildClasses).Concat(ScheduledClasses);

        public Subject ParentSubject { get; set; }

        public string ClassType { get; set; }

        public string ClassDescription => ClassName.Split(" ")[0];//eg, Lecture, Tutorial

        public int ID { get; set; }
    }
}
