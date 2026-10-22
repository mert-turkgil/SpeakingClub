using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    public class IndexModel
    {
        // Newest posts for the home page: the ones marked "show on home" first, then the latest
        public List<BlogCardModel> LatestPosts { get; set; } = new List<BlogCardModel>();
    }
}
