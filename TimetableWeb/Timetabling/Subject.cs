using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RestSharp;

namespace Timetable
{
    public class Subject
    {
        public string Code { get; set; }
        public string ShortCode => Code.Replace("0", "");
        public string Name { get; set; }

        public string DisplayName => Code + " " + Name;

        [JsonIgnore]
        public List<ClassInfo> Classes { get; set; }


        public async Task UpdateTimetable()
        {
            Classes = new List<ClassInfo>();


            string resultStr = await getTimetableHTML(Code);

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(resultStr);

            //Portable .NET doesn't let me use XPATH sadly. luckily there is only one table
            var timesTable = doc.DocumentNode.Descendants("table").FirstOrDefault();

            if (timesTable == null)
                return;

            var rows = timesTable.Descendants("tbody").First().Descendants("tr");

            foreach (var row in rows)
            {
                try
                {
                    var elements = row.Descendants("td").ToArray();

                    string date = elements[9].InnerText;
                    string start = date + " " + elements[3].InnerText;
                    string end = date + " " + elements[4].InnerText;

                    string[] info = elements[0].InnerText.Split('/');

                    var timeStart = DateTime.ParseExact(start, "dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
                    var timeEnd = DateTime.ParseExact(end, "dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

                    ScheduledClass scheduledClass = new ScheduledClass(timeStart, timeEnd)
                    {
                        Location = elements[7].InnerText,
                        ClassNumber = short.Parse(info[5])
                    };

                    var classInfo = Classes.FirstOrDefault(c => c.ClassType == info[4]);

                    if (classInfo != null)
                    {
                        scheduledClass.ClassInfo = classInfo;
                        classInfo.ScheduledClasses.Add(scheduledClass);
                    }
                    else
                    {
                        classInfo = new ClassInfo
                        {
                            ClassName = elements[1].InnerText,
                            ScheduledClasses = new List<ScheduledClass> { scheduledClass },
                            ParentSubject = this,
                            ClassType = info[4]
                        };
                        scheduledClass.ClassInfo = classInfo;
                        Classes.Add(classInfo);
                    }
                }
                catch (FormatException e)//Sadly can't trust timetable to be in correct format
                {}
            }

            //Consolidating lecture streams:

            var lectures = Classes.Where(c => c.ClassType.StartsWith("L")).OrderBy(c => c.ClassType);

            if (!lectures.Any())
                return;

            foreach (var scheduledLecture in lectures.First().ScheduledClasses)
            {
                foreach (var lecture in lectures.Skip(1))
                {
                    var pairedLecture =
                        lecture.ScheduledClasses.FirstOrDefault(c => c.ClassNumber == scheduledLecture.ClassNumber);
                    scheduledLecture.ChildClasses.Add(pairedLecture);
                }
            }

            foreach (var lecture in lectures.Skip(1))
            {
                Classes.Remove(lecture);//Remove any lectures that are not the initial from class list
            }

            //Removing equivalent classes:

            foreach (var classInfo in Classes)
            {
                var removals = new List<ScheduledClass>();//Can't remove them during enumeration, must collect to remove later
                foreach (var scheduledClass in classInfo.ScheduledClasses)
                {
                    if (removals.Contains(scheduledClass))
                        continue;

                    var equivalents = classInfo.ScheduledClasses.Where(b => classEquivalent(scheduledClass, b) && b != scheduledClass);
                    removals.AddRange(equivalents);
                }

                foreach (var removal in removals)
                    classInfo.ScheduledClasses.Remove(removal);
            }

            DateTime earliestClass = DateTime.MaxValue;

            foreach (var scheduledClass in Classes.SelectMany(classInfo => classInfo.ScheduledClasses))
            {
                if (scheduledClass.TimeStart < earliestClass)
                    earliestClass = scheduledClass.TimeStart;
            }
            int earliestWeek = getWeekOfYear(earliestClass);


            foreach (var scheduledClass in Classes.SelectMany(classInfo => classInfo.ScheduledClasses))
            {
                while(getWeekOfYear(scheduledClass.TimeStart) > earliestWeek)
                {
                    scheduledClass.TimeStart = scheduledClass.TimeStart - TimeSpan.FromDays(7);
                    scheduledClass.TimeEnd = scheduledClass.TimeEnd - TimeSpan.FromDays(7);
                }
            }
        }

        private int getWeekOfYear(DateTime dt)
        {
            GregorianCalendar calendar = new GregorianCalendar();
            return calendar.GetWeekOfYear(dt, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
        }

        private bool classEquivalent(ScheduledClass a, ScheduledClass b)
        {
            return a.SlotStart == b.SlotStart &&
                   a.SlotEnd == b.SlotEnd;
        }

        private async Task<string> getTimetableHTML(string subjectCode)
        {
            RestClient c = new RestClient("https://sws.unimelb.edu.au");

            string query = "?objects=" + subjectCode + "&weeks=1-26&days=1-7&periods=1-56&template=module_by_group_list";

            var request = new RestRequest("/2018/Reports/List.aspx" + query);

            return (await c.ExecuteTaskAsync<string>(request)).Content;
        }
    }

}
