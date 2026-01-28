using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        new Task<Tag?> GetByIdAsync(int id);
    }
}