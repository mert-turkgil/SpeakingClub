using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpeakingClub.Services.Email;

namespace SpeakingClub.ViewComponents
{
    /// <summary>
    /// "Abonelere e-postayla duyur" on the post and quiz forms: a switch with the number of members who would get the
    /// announcement, or, once the post/quiz was announced, how far the sending got (an announcement goes out once).
    /// </summary>
    public class AnnounceBoxViewComponent : ViewComponent
    {
        private readonly EmailQueue _queue;

        public AnnounceBoxViewComponent(EmailQueue queue) => _queue = queue;

        public record Model(string Kind, bool Checked, int Recipients, EmailQueue.CampaignSummary? Campaign);

        /// <param name="kind">EmailTemplates.NewPost or EmailTemplates.NewQuiz</param>
        /// <param name="entityId">BlogId / quiz id; 0 while creating</param>
        public async Task<IViewComponentResult> InvokeAsync(string kind, int entityId, bool isChecked)
        {
            var campaign = entityId > 0 ? await _queue.CampaignSummaryAsync(kind, entityId) : null;
            var recipients = campaign == null ? await _queue.RecipientCountAsync(kind) : 0;
            return View(new Model(kind, isChecked, recipients, campaign));
        }
    }
}
