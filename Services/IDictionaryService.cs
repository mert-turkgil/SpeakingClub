using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public interface IDictionaryService
    {
        Task<WordViewModel?> GetWordDetailsAsync(string word, string sourceLang, string targetLang);
    }
}
