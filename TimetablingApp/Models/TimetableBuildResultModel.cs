

using Timetabling;

namespace TimetablingApp.Models
{
    public class TimetableBuildResultModel
    {
        public TimetableBuildResultModel(Timetable topTimetable,int numberTimetables, string status,string message = "")
        {
            TopTimetable = topTimetable;
            NumberTimetables = numberTimetables;
            ResultStatus = status;
            ResultMessage = message;
        }
        public Timetable TopTimetable { get; set; }
        public int NumberTimetables { get; set; }
        public string ResultStatus { get; set; }
        public string ResultMessage { get; set; }
    }
}
