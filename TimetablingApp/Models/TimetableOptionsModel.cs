using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TimetablingApp.Models
{
    public class TimetableOptionsModel
    {
        public List<String> SubjectCodes { get; set; }

        public bool LaterStarts { get; set; }
        public bool LessDays { get; set; }
    }
}
