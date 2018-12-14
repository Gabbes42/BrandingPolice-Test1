using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace WebJob
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.DefaultConnectionLimit = 100;

            var config = new JobHostConfiguration();
            config.DashboardConnectionString = "";
            var loggerFactory = new LoggerFactory();
            config.LoggerFactory = loggerFactory.AddConsole();
            var host = new JobHost(config);
            host.RunAndBlock();
        }
    }
}
