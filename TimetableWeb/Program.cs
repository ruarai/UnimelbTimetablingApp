#define ELECTRON
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
#if ELECTRON
using ElectronNET.API;
#endif

namespace TimetableWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
#if ELECTRON
            return WebHost.CreateDefaultBuilder(args)
                .UseElectron(args)
                .UseStartup<Startup>()
                .Build();
#else
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
#endif

        }
    }
}
