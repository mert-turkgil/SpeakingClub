using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpeakingClub.Models;

namespace SpeakingClub.Services
{
    public interface IDictionaryService
    {
        Task<WordViewModel?> GetWordDetailsAsync(string word);
    }
}