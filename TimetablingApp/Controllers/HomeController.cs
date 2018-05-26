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

                if(subject.Classes == null)
                    await subject.UpdateTimetable();

                subjects.Add(subject);
            }

            IEnumerable<ClassInfo> classInfos = subjects.SelectMany(subject => subject.Classes);

            Generator g = new Generator();
            int possiblePermutations = Generator.PossiblePermutationsCount(classInfos);

            if (possiblePermutations == 0)
                return Json(new TimetableBuildResultModel(null, 0, "failure", "No classes can be scheduled within your filtered time."));

            g.ProgressUpdate += generatorProgressUpdate;

            lastTimetables = new List<Timetable>();

            int maxClashes = 0;

            /*while(!lastTimetables.Any())
            {
                try
                {
                    if (maxClashes == 0)
                    {
                        generatorStatusUpdate(string.Format("Generating up to {0:n0} timetable{1}...", possiblePermutations, possiblePermutations > 1 ? "s" : ""));
                        var permutations = g.GenPermutations(classInfos.ToList(), maxClashes);
                        generatorStatusUpdate("Sorting timetables...");
                        lastTimetables = g.SortPermutations(permutations, model.LaterStarts, model.LessDays).ToList();
                    }
                    else
                    {
                        generatorStatusUpdate(string.Format("Generating up to {0:n0} timetable{1}...", possiblePermutations, possiblePermutations > 1 ? "s" : ""));
                        var permutations = g.GenPermutations(classInfos.ToList(), maxClashes);
                        generatorStatusUpdate("Sorting timetables...");
                        lastTimetables = g.SortClashedPermutations(permutations, model.LaterStarts, model.LessDays).ToList();
                    }
                }
                catch (InsufficientMemoryException)
                {
                    return Json(new TimetableBuildResultModel(null, 0, "failure", "Timetables could not be generated as the generator ran out of memory. " +
                                                                                  "Try adding additional filtering to reduce the number of possible timetables."));
                }


                maxClashes++;
            }*/

            lastTimetables = g.GeneratePermutationsExpanding(classInfos);

            generatorStatusUpdate(string.Format("Generated {0:n0} timetable{1}.", lastTimetables.Count, lastTimetables.Count > 1 ? "s" : ""));
            return Json(new TimetableBuildResultModel(lastTimetables[0], lastTimetables.Count,"success"));
        }

        public IActionResult GetTimetable(int index)
        {
            return Json(lastTimetables[index]);
        }

        private readonly IHubContext<UIHub> _uiHub;

        private void generatorProgressUpdate(float progress)
        {
            _uiHub.Clients.All.SendAsync("progress", progress);
        }
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

            return Json(testSubject.Classes);
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

                if (subject.Classes == null)
                    await subject.UpdateTimetable();

                subjects.Add(subject);
            }

            IEnumerable<ClassInfo> classInfos = subjects.SelectMany(subject => subject.Classes);

            int numPermutations = Generator.PossiblePermutationsCount(classInfos);
            
            if(numPermutations > 1)
                return Json(string.Format("{0:n0} possible timetables.", numPermutations));
            else
                return Json("No possible timetables under these filters.");
        }

        private IEnumerable<ClassInfo> filterClasses(IEnumerable<ClassInfo> classes, string earliestTimeString, string latestTimeStrng, string daysString)
        {
            TimeSpan earliestTime = TimeSpan.Parse(earliestTimeString);
            TimeSpan latestTime = TimeSpan.Parse(latestTimeStrng);

            foreach(var classInfo in classes)
            {
                ClassInfo newClassInfo = new ClassInfo { ClassName = classInfo.ClassName, ClassType = classInfo.ClassType };

                newClassInfo.ScheduledClasses = classInfo.ScheduledClasses.Where(c => filterClass(c,earliestTime,latestTime,daysString)
                                                                                   && c.ChildClasses.All(cc => filterClass(cc, earliestTime, latestTime, daysString))).ToList();

                yield return newClassInfo;
            }
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