using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Timetabling;
using System.Web;
using Microsoft.AspNetCore.Hosting;
using TimetablingApp.Models;
using System.Threading;

namespace TimetablingApp.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _hostingEnvironment;

        private static List<Subject> _subjectList = null;
        private List<Subject> subjectList
        {
            get
            {
                if (_subjectList == null)
                {
                    _subjectList = getSubjects();
                }
                
                return _subjectList;
            }
        }

        public HomeController(IHostingEnvironment environment)
        {
            _hostingEnvironment = environment;
        }

        public IActionResult Index()
        {
            return View(subjectList);
        }
        
        [HttpPost("/Home/BuildTimetable")]
        public async Task<IActionResult> BuildTimetable([FromBody] TimetableOptionsModel model)
        {
            List<Subject> subjects = new List<Subject>();
            foreach(var subjectCode in model.SubjectCodes)
            {
                Subject subject = subjectList.FirstOrDefault(s => s.Code == subjectCode);

                if (subject == null)
                    continue;

                if(subject.ClassInfos == null)
                    await subject.UpdateTimetable();

                subjects.Add(subject);
            }

            IEnumerable<ClassInfo> classInfos = subjects.SelectMany(subject => subject.ClassInfos);
            IEnumerable<ClassInfo> originalClassInfos = subjects.SelectMany(subject => subject.OriginalClassInfos);

            int id = 0;
            foreach(var classInfo in originalClassInfos)
                classInfo.ID = id++;

            Generator g = new Generator { SortLaterStarts = model.LaterStarts, SortLessDays = model.LessDays };
            long possiblePermutations = Generator.PossiblePermutationsCount(classInfos);

            List<Timetable> timetables = new List<Timetable>();
            
            //Check what algorithm to use, if we have over 5M permutations use the expanding algorithm
            if (possiblePermutations > 5 * 1000 * 1000)
                timetables = g.GeneratePermutationsExpanding(classInfos);
            else
                timetables = g.GenerateTimetablesBruteForce(classInfos);

            var compressedTimetables = timetables.Select(t => new CompressedTimetable(t)).Take(25000).ToList();
                
            return Json(new TimetableBuildResultModel(compressedTimetables, originalClassInfos.ToList()));
        }


        public async Task<IActionResult> GetSubjectInfo(string subjectCode)
        {
            Subject testSubject = subjectList.FirstOrDefault(s => s.Code == subjectCode);

            if (testSubject == null)
                return Json(null);

            await testSubject.UpdateTimetable();

            return Json(testSubject.ClassInfos);
        }

        [HttpPost("/Home/UpdateSelectedSubjects")]
        public async Task<IActionResult> UpdateSelectedSubjects([FromBody]TimetableOptionsModel model)
        {
            List<Subject> subjects = new List<Subject>();
            foreach (var subjectCode in model.SubjectCodes)
            {
                Subject subject = subjectList.FirstOrDefault(s => s.Code == subjectCode);

                if (subject == null)
                    continue;

                if (subject.ClassInfos == null)
                    await subject.UpdateTimetable();

                subjects.Add(subject);
            }

            //No subjects? No possible timetables.
            if(!subjects.Any())
                return Json(0);

            IEnumerable<ClassInfo> classInfos = subjects.SelectMany(subject => subject.ClassInfos);

            long numPermutations = Generator.PossiblePermutationsCount(classInfos);

            return Json(numPermutations);
        }
        

        private bool filterClass(ScheduledClass scheduledClass,TimeSpan earliestTime, TimeSpan latestTime, string daysString)
        {
            return scheduledClass.TimeStart.TimeOfDay >= earliestTime &&
                   scheduledClass.TimeEnd.TimeOfDay <= latestTime &&
                   daysString[(int)scheduledClass.TimeStart.DayOfWeek - 1] == '1';
        }

        private List<Subject> getSubjects()
        {
            List<Subject> subjectList = new List<Subject>();

            string subjectsText = System.IO.File.ReadAllText(Path.Combine(_hostingEnvironment.WebRootPath, "codes_2018_sem2.json"));

            var rawSubjects = JsonConvert.DeserializeObject<List<string>>(subjectsText);

            foreach (var rawSubject in rawSubjects)
            {
                var parts = rawSubject.Split(' ');

                Subject subject = new Subject
                {
                    Code = parts[0],
                    Name = rawSubject.Substring(parts[0].Length + 1)
                };

                if (subjectList.All(s => s.Code != subject.Code))
                    subjectList.Add(subject);
            }

            return subjectList;
        }
    }
}