using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Entity;

namespace SpeakingClub.Data.Abstract
{
    public interface IArticleRepository: IGenericRepository<Article>
    {
        Task<IEnumerable<Article>> GetArticlesByTeacherIdAsync(string teacherId);
        Task<IEnumerable<Article>> SearchArticlesByKeywordAsync(string keyword);
        Task<int> CountArticlesByTeacherAsync(string teacherId);
    }
}