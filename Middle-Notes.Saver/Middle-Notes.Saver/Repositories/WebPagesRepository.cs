using Middle_Notes.Saver.Models;

namespace Middle_Notes.Saver.Repositories
{
    public class WebPagesRepository : BaseRepository
    {
        public WebPagesRepository(string connectionString) : base(connectionString)
        {
        }

        public bool InsertPage(long websiteId, WebPage wp)
        {
            var sql = "INSERT INTO websites_pages_aggregates (" +
                "website_id, url, html, is_done, is_deleted, has_error) VALUES ( " +
                 "" + EscapeIntegerValue(websiteId) + ", " +
                 "" + EscapeAndQuoteStringValue(wp.Url) + ", " +
                 "" + EscapeAndQuoteStringValue(wp.Html) + ", " +
                 "" + EscapeBooleanValue(wp.IsDone) + ", " +
                 "" + EscapeBooleanValue(wp.IsDeleted) + ", " +
                 "" + EscapeBooleanValue(wp.HasError) + " )";
            return ExecuteNonQuery(sql);
        }
    }
}
