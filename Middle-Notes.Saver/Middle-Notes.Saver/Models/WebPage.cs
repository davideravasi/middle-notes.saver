namespace Middle_Notes.Saver.Models
{
    public class WebPage
    {
        public long WebsitesPageId { get; set; }
        public long WebsiteId { get; set; }
        public string Url { get; set; }
        public string Html { get; set; }
        public long Bytes { get; set; }
        public bool IsDone { get; set; }
        public bool IsDeleted { get; set; }
        public bool HasError { get; set; }
    }
}
