using Microsoft.Extensions.Configuration;
using Middle_Notes.Saver.Helpers;
using Middle_Notes.Saver.Models;
using Middle_Notes.Saver.Models.Options;
using Middle_Notes.Saver.Repositories;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Middle_Notes.Saver
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false, true)
               .AddEnvironmentVariables();
            var configuration = builder.Build();

            Log.Logger = new LoggerConfiguration()
               .ReadFrom.Configuration(configuration)
               .CreateLogger();

            Log.Information("Start {0} procedure", args[0]);

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error((Exception)e.ExceptionObject, "Fatal exception");
            };

            var connectionStringOptions = configuration.GetSection("ConnectionStringOptions").Get<ConnectionStringOptions>();
            var savingOptions = configuration.GetSection("SavingOptions").Get<SavingOptions>();

            var wr = new WebsitesRepository(connectionStringOptions.DefaultConnectionString);
            var websites = wr.GetWebsitesId();
            var war = new WebPagesAggregatesRepository(connectionStringOptions.DefaultConnectionString);
            var wmr = new WebPagesMastersRepository(connectionStringOptions.DefaultConnectionString);
            foreach (var website in websites)
            {
                await SaveWebPages(website, savingOptions, war, wmr, args[0]);
            }
        }

        public static async Task SaveWebPages(Website website, SavingOptions options, 
            WebPagesAggregatesRepository war, WebPagesMastersRepository wmr, string arg)
        {
            IEnumerable<WebPage> webPages = null;
            switch (arg?.ToLower())
            {
                case "aggregates":
                    webPages = war.GetWebPagesToSave(website.Id);
                    break;
                case "masters":
                    webPages = wmr.GetWebPagesToSave(website.Id);
                    break;
                default:
                    break;
            }
            if (webPages == null) return;
            var webPagesCount = webPages.Count();
            if (webPagesCount == 0) return;

            //Generate n list of tasks based on maximum task number
            var webPagesForTask = new List<IEnumerable<WebPage>>();
            var maxDegreeOfParallelism = options.MaxDegreeOfParallelism < webPagesCount
                ? options.MaxDegreeOfParallelism : webPagesCount;
            var webPagesForTaskCount = (int)Math.Ceiling((float)webPagesCount / maxDegreeOfParallelism);
            for (int i = 0; i < options.MaxDegreeOfParallelism; i += 1)
            {
                webPagesForTask.Add(webPages.Skip(i * webPagesForTaskCount).Take(webPagesForTaskCount));
            }

            //lock db access
            object writingLocker = new object();

            //Create task list
            var tasks = new List<Task>();

            var aggregatesNewUrl = new List<string>();
            for (int i = 0; i < webPagesForTask.Count; i++)
            {
                var index = i;
                //Add n task for each page list (usually same number as MaximumTasksNumber but sometimes the split method generate one more list)
                //Ex with 10 as MaximumTasksNumber: 40010 / 10 = 10 lists of 4000 elements and 1 list of 10 elements = 11 lists
                tasks.Add(Task.Run(async () =>
                {
                    using (var driver = website.NeedBrowser ? SavingHelpers.GetConfiguredFirefoxDriver() : null)
                    {
                        foreach (var webPage in webPagesForTask[index])
                        {
                            await Task.Delay(options.MillisecondDelayAfterCalls);

                            WebPage savedWebPage = null;

                            if (website.NeedBrowser)
                            {
                                savedWebPage = await SavingHelpers.DownloadWithSelenium(driver, website,
                                    webPage, options);
                            }
                            else
                            {
                                savedWebPage = await SavingHelpers.DownloadWithHttpClient(webPage);
                            }
                            switch (arg?.ToLower())
                            {
                                case "aggregates":
                                    lock (writingLocker)
                                    {
                                        war.UpdateWebPage(savedWebPage);
                                    }
                                    var pageAggregatesNewUrl = SavingHelpers.FindNewUrl(savedWebPage.Html, website.AggregateRegex, website.Host);
                                    if (pageAggregatesNewUrl != null && pageAggregatesNewUrl.Count > 0)
                                    {
                                        foreach (var newUrl in pageAggregatesNewUrl)
                                        {
                                            if (!aggregatesNewUrl.Contains(newUrl)) aggregatesNewUrl.Add(newUrl);
                                        }
                                    }
                                    break;
                                case "masters":
                                    lock (writingLocker)
                                    {
                                        wmr.UpdateWebPage(savedWebPage);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);

            if (aggregatesNewUrl.Count == 0 || arg != "aggregates") return;
            foreach (var newUrl in aggregatesNewUrl)
            {
                if (war.ExistsWebPage(website.Id, newUrl)) continue;
                war.InsertWebPage(new WebPage()
                {
                    WebsiteId = website.Id,
                    Url = newUrl
                });
            }
        }
    }
}
