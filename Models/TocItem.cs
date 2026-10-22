namespace SpeakingClub.Models
{
    // One entry of a blog post's table of contents: an h2/h3 of the article and the anchor id it was given.
    public class TocItem
    {
        public int Level { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}
