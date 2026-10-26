namespace SpeakingClub.Models
{
    /// <summary>
    /// What Views/Home/_LegalLayout.cshtml needs to draw the frame of a legal page (privacy policy, terms of use):
    /// the hero, the table of contents and the link to the other legal text. The page itself supplies the sections.
    /// </summary>
    public record LegalPage(
        string Eyebrow,
        string EyebrowIcon,
        string Title,
        string Lead,
        string Updated,
        (string Id, string Title)[] Sections,
        string OtherTitle,
        string OtherText,
        string OtherPath,
        string OtherIcon);
}
