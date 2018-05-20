using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Timetabling;

namespace TimetablingApp.Models
{
    public class TimetableModel
    {
        public TimetableModel(IEnumerable<Timetable> timetables, string status,string message = "")
        {
            Timetables = timetables;
            ResultStatus = status;
            ResultMessage = message;
        }
        public IEnumerable<Timetable> Timetables { get; set; }
        public string ResultStatus { get; set; }
        public string ResultMessage { get; set; }
    }
}
