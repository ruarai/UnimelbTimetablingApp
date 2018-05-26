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
using Microsoft.AspNetCore.SignalR;
using ElectronNET.API;
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

        private static List<Timetable> lastTimetables = null;

        public HomeController(IHostingEnvironment environment, IHubContext<UIHub> hubContext)
        {
            _hostingEnvironment = environment;
            _uiHub = hubContext;
        }

        public IActionResult Index()
        {
            return View(subjectList);
        }
        
        [HttpPost("/Home/BuildTimetable")]
        public async Task<IActionResult> BuildTimetable([FromBody] TimetableOptionsModel model)
        {
            generatorStatusUpdate("Fetching timetables from online...");

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

            Generator g = new Generator { SortLaterStarts = model.LaterStarts, SortLessDays = model.LessDays };
            long possiblePermutations = Generator.PossiblePermutationsCount(classInfos);

            if (possiblePermutations == 0)
                return Json(new TimetableBuildResultModel(null, 0, "failure", "No classes can be scheduled within your filtered time."));

            generatorStatusUpdate("Generating timetables...");

            //Check what algorithm to use, if we have over 5M permutations use the expanding algorithm
            if (possiblePermutations > 5 * 1000 * 1000)
                lastTimetables = g.GeneratePermutationsExpanding(classInfos);
            else
                lastTimetables = g.GenerateTimetablesBruteForce(classInfos);

            generatorStatusUpdate(string.Format("Generated {0:n0} timetable{1}.", lastTimetables.Count, lastTimetables.Count > 1 ? "s" : ""));
            return Json(new TimetableBuildResultModel(lastTimetables[0], lastTimetables.Count,"success"));
        }

        public IActionResult GetTimetable(int index)
        {
            return Json(lastTimetables[index]);
        }

        private readonly IHubContext<UIHub> _uiHub;

        private void generatorStatusUpdate(string newStatus)
        {
            _uiHub.Clients.All.SendAsync("status", newStatus);
        }
        private void subjectInfoUpdate(string subjectInfo)
        {
            _uiHub.Clients.All.SendAsync("subjectInfo", subjectInfo);
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