using Middle_Notes.Saver.Models;
using Middle_Notes.Saver.Models.Options;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Middle_Notes.Saver.Helpers
{
    public static class SavingHelpers
    {
        public static async Task<WebPage> DownloadWithHttpClient(WebPage webPage)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(webPage.Url);
                var statusCode = response.StatusCode;
                string html = null;
                if (statusCode == HttpStatusCode.OK)
                {
                    var content = response.Content;
                    html = await content.ReadAsStringAsync();
                }
                return new WebPage()
                {
                    WebsiteId = webPage.WebsiteId,
                    WebsitesPageId = webPage.WebsitesPageId,
                    Url = webPage.Url,
                    Html = html,
                    //Bytes = byte.Parse(html ?? string.Empty),
                    IsDone = statusCode == HttpStatusCode.OK,
                    IsDeleted = statusCode == HttpStatusCode.NotFound,
                    HasError = statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.NotFound,
                };
            }
        }

        public static async Task<WebPage> DownloadWithSelenium(IWebDriver driver, Website website, 
            WebPage webPage, SavingOptions options)
        {
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

            return new WebPage()
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

        public static List<string> FindNewUrl (string html, string regex, string host)
        {
            var hrefRegex = "href=\".+?\"";
            var hrefMatches = Regex.Matches(html, hrefRegex);
            if (hrefMatches == null || hrefMatches.Count == 0) return null;
            List<string> newUrlList = new List<string>();
            foreach (Match hrefMatch in hrefMatches)
            {
                var cleanHref = hrefMatch.Value.Replace("\"", "").Replace("href=", "");
                if (cleanHref.StartsWith(@"\"))
                    cleanHref = Path.Combine(host, cleanHref);
                if (Regex.IsMatch(cleanHref, regex)
                && !newUrlList.Contains(cleanHref))
                    newUrlList.Add(cleanHref);
            }
            return newUrlList;
        }

        public static FirefoxDriver GetConfiguredFirefoxDriver()
        {
            var browserService = FirefoxDriverService.CreateDefaultService
                        (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            browserService.SuppressInitialDiagnosticInformation = true;
            browserService.HideCommandPromptWindow = true;
            var browserOptions = new FirefoxOptions();
            browserOptions.AddArguments("--headless", "--incognito", "--log-level=3", "--hide-scrollbars", "--silent");
            var driver = new FirefoxDriver(browserService, browserOptions);
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(45);
            return driver;
        }
    }
}
