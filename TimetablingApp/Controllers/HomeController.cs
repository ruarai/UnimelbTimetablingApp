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

        public HomeController(IHostingEnvironment environment, IHubContext<UIHub> hubContext)
        {
            _hostingEnvironment = environment;
            _uiHub = hubContext;
        }

        public IActionResult Index()
        {
            return View(subjectList);
        }

        public async Task<IActionResult> GetTimetable(string codes, bool laterStarts, bool lessDays)
        {
            //very gross but idk lol
            string[] subjectCodes = codes.Split('|');


            List<Subject> subjects = new List<Subject>();
            foreach(var subjectCode in subjectCodes)
            {
                Subject subject = subjectList.FirstOrDefault(s => s.Code == subjectCode);

                if (subject == null)
                    continue;

                await subject.UpdateTimetable();

                subjects.Add(subject);
            }

            List<ClassInfo> classInfos = subjects.SelectMany(subject => subject.Classes).ToList();

            Generator g = new Generator();

            g.ProgressUpdate += generatorProgressUpdate;

            List<Timetable> timetables = new List<Timetable>();

            int maxClashes = 0;

            while(!timetables.Any())
            {
                if (maxClashes == 0)
                    timetables = g.SortPermutations(g.GenPermutations(classInfos, maxClashes), laterStarts, lessDays).ToList();
                else
                    timetables = g.SortClashedPermutations(g.GenPermutations(classInfos, maxClashes), laterStarts, lessDays).ToList();

                maxClashes++;
            }


            return Json(timetables);
        }
        private readonly IHubContext<UIHub> _uiHub;

        private void generatorProgressUpdate(float progress)
        {
            _uiHub.Clients.All.SendAsync("progress", progress);
        }

        public async Task<IActionResult> GetSubjectInfo(string subjectCode)
        {
            Subject testSubject = subjectList.FirstOrDefault(s => s.Code == subjectCode);

            if (testSubject == null)
                return Json(null);

            await testSubject.UpdateTimetable();

            return Json(testSubject.Classes);
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