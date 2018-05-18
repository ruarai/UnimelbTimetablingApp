using System.Collections.Generic;
using Newtonsoft.Json;

namespace Timetabling
{
    public class ClassInfo
    {
        public string ClassName { get; set; }

        [JsonIgnore]
        public List<ScheduledClass> ScheduledClasses { get; set; }
        public Subject ParentSubject { get; set; }

        public string ClassType { get; set; }
    }
}
