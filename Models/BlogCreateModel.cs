using System;

namespace SpeakingClub.Models
{
    public class BlogCreateModel : BlogFormModel
    {
        public string TempBlogId { get; set; } = Guid.NewGuid().ToString();
    }
}
