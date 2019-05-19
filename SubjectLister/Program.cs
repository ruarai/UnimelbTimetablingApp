using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RestSharp;

namespace SubjectLister
{
    class Program
    {
        //What year/semester you are fetching for
        //This should still include year long subjects
        private const int Year = 2019;
        private const int Semester = 2;

        //Number of pages to fetch
        //Needs to be determined manually for each new semester, just look at
        //https://handbook.unimelb.edu.au/search?query=&year=2018&types%5B%5D=subject&level_type%5B%5D=all&study_periods%5B%5D=semester_2&study_periods%5B%5D=year_long&area_of_study=all&faculty=all&department=all
        //manually to determine this (changing year/semester in the url, of course)
        private const int Pages = 116;

        

        static void Main(string[] args)
        {
            //ugly url to perform the search
            string url =
                $"search?query=&faculty=all&department=all&year={Year}&area_of_study=all&types%5B%5D=subject&level_type%5B%5D=all&study_periods%5B%5D=semester_{Semester}&study_periods%5B%5D=year_long&sort=external_code%7Casc&page=";

            //create a client to perform the scraping
            RestClient c = new RestClient("https://handbook.unimelb.edu.au/");

            //It actually needs a useragent, surprisingly
            c.UserAgent =
                "agent:Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36";

            List<string> codes = new List<string>();

            //Fetch and parse every page
            for (int i = 1; i <= Pages; i++)
            {
                RestRequest request = new RestRequest(url + i);

                request.Method = Method.GET;
                request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                request.AddHeader("accept-encoding", "gzip");

                var r = c.Execute(request);

                if (!r.IsSuccessful)
                {
                    Console.WriteLine("failed to find page?");
                    break;
                }

                HtmlDocument d = new HtmlDocument();

                d.LoadHtml(r.Content);

                var results = d.DocumentNode.QuerySelectorAll(".search-results__accordion-item");

                foreach (var result in results)
                {
                    string name = result.QuerySelector(".search-results__accordion-title").InnerText;

                    string code = result.QuerySelector(".search-results__accordion-code").InnerText;

                    name = name.Replace(code, "");

                    string full = code.Trim() + " " + name.Trim();

                    codes.Add(full);

                    Console.WriteLine(full);
                }
            }

            //Writes the output to a JSON file
            File.WriteAllText($"codes_{Year}_sem_{Semester}.json", JsonConvert.SerializeObject(codes));
        }
    }
}
