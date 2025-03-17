using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Models
{
    public class IndexModel
    {
    // Use the entity namespace instead of the model namespace
    public List<SpeakingClub.Entity.Blog> BlogItems { get; set; } = new List<SpeakingClub.Entity.Blog>();
    }
}