using Microsoft.Extensions.Configuration;
using Middle_Notes.Saver.Helpers;
using Middle_Notes.Saver.Models;
using Middle_Notes.Saver.Models.Options;
using Middle_Notes.Saver.Repositories;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Middle_Notes.Saver
{
    class Program
    {
        private static CancellationTokenSource _tokenSource;

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
                //Stop saving tasks
                _tokenSource?.Cancel();
                _tokenSource?.Dispose();
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
            for (int i = 0; i < webPagesCount; i += (webPagesCount / maxDegreeOfParallelism))
            {
                webPagesForTask.Add(webPages.Skip(i).Take(webPagesCount / maxDegreeOfParallelism));
            }

            //Create task list
            var tasks = new List<Task>();
            //Add n task for each page list (usually same number as MaximumTasksNumber but sometimes the split method generate one more list)
            //Ex with 10 as MaximumTasksNumber: 40010 / 10 = 10 lists of 4000 elements and 1 list of 10 elements = 11 lists
            _tokenSource = new CancellationTokenSource();
            var token = _tokenSource.Token;

            object writingLocker = new object();

            for (int i = 0; i < webPagesForTask.Count; i++)
            {
                var index = i;

                FirefoxDriver driver = null;
                if (website.NeedBrowser)
                {
                    var browserService = FirefoxDriverService.CreateDefaultService
                        (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                    browserService.SuppressInitialDiagnosticInformation = true;
                    browserService.HideCommandPromptWindow = true;
                    var browserOptions = new FirefoxOptions();
                    browserOptions.AddArguments("--headless", "--incognito", "--log-level=3", 
                        "--hide-scrollbars", "--silent");
                    driver = new FirefoxDriver(browserService, browserOptions);
                    driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(45);
                }

                tasks.Add(Task.Run(async () =>
                {
                    foreach (var webPage in webPagesForTask[index])
                    {
                        if (_tokenSource.Token.IsCancellationRequested) break;
                        //Sleep for n millisecond to avoid server banlist
                        await Task.Delay(options.MillisecondDelayAfterCalls);

                        WebPage savedWebPage = null;

                        if (website.NeedBrowser)
                        {
                            //Selenium procedure
                            driver.Navigate().GoToUrl(webPage.Url);
                            if (website.SessionStorageParameters != null && website.SessionStorageParameters.Count > 0)
                            {
                                foreach (var item in website.SessionStorageParameters)
                                {
                                    var js = ((IJavaScriptExecutor)driver);
                                    js.ExecuteScript(string.Format("window.sessionStorage.setItem('{0}','{1}')", item.Key, item.Value));
                                }
                                driver.Navigate().Refresh();
                            }
                            if (options.MillisecondDelayAfterPageLoading > 0) await Task.Delay(options.MillisecondDelayAfterPageLoading);

                            //Variable to check page status code
                            var statusCode = HttpStatusCode.OK;

                            //Sometime webpages are loaded correctly (statuscode = 200) but the page content is equal of a 404 result
                            if (website.ErrorIdentifiers != null && website.ErrorIdentifiers.Count > 0)
                            {
                                foreach (var errorIdentifier in website.ErrorIdentifiers)
                                {
                                    if (driver.PageSource.Contains(errorIdentifier.Key)) { statusCode = errorIdentifier.Value; break; }
                                }
                            }

                            savedWebPage = new WebPage()
                            {
                                WebsiteId = website.Id,
                                WebsitesPageId = webPage.WebsitesPageId,
                                Url = webPage.Url,
                                Html = driver.Url == webPage.Url ? driver.PageSource : null,
                                //Bytes = byte.Parse(driver.PageSource ?? string.Empty),
                                IsDone = statusCode == HttpStatusCode.OK,
                                IsDeleted = statusCode == HttpStatusCode.NotFound || driver.Url != webPage.Url,
                                HasError = statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.NotFound,
                            };
                        }
                        else
                        {
                            savedWebPage = await SavingHelpers.Save(webPage);
                        }
                        switch (arg?.ToLower())
                        {
                            case "aggregates":
                                lock(writingLocker)
                                {
                                    war.UpdateWebPage(savedWebPage);
                                    var hrefRegex = "href=\".+?\"";
                                    var hrefMatches = Regex.Matches(savedWebPage.Html, hrefRegex);
                                    if (hrefMatches == null || hrefMatches.Count == 0) break;
                                    List<string> newUrls = new List<string>();
                                    foreach (Match hrefMatch in hrefMatches)
                                    {
                                        var cleanHref = hrefMatch.Value.Replace("\"", "").Replace("href=", "");
                                        if (cleanHref.StartsWith(@"\")) 
                                            cleanHref = Path.Combine(website.Host, cleanHref);
                                        if (Regex.IsMatch(cleanHref, website.AggregateRegex) 
                                        && !newUrls.Contains(cleanHref))
                                            newUrls.Add(cleanHref);
                                    }
                                    foreach (var newUrl in newUrls)
                                    {
                                        if (war.ExistsWebPage(website.Id, newUrl)) continue;
                                        war.InsertWebPage(new WebPage()
                                        {
                                            WebsiteId = website.Id,
                                            Url = newUrl,
                                            IsDone = false,
                                            IsDeleted = false,
                                            HasError = false
                                        });
                                    }
                                }
                                break;
                            case "masters":
                                lock (writingLocker)
                                {
                                    wmr.InsertWebPage(savedWebPage);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    if (website.NeedBrowser)
                    {
                        driver.Dispose();
                        Log.Information("------DRIVER DISPOSED CORRECTLY------");
                    }
                }, token));
            }
            await Task.WhenAll(tasks);
        }
    }
}
