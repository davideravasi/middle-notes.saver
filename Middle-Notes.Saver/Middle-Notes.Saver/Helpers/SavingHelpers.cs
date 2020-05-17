using Middle_Notes.Saver.Models;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Middle_Notes.Saver.Helpers
{
    public static class SavingHelpers
    {
        public static async Task<WebPage> Save(WebPage webPage)
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
    }
}
