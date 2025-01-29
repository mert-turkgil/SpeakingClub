using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Models
{
    public class CardModel
    {
        #nullable disable
        public List<string> Images { get; set; }

        public int BlogCount { get; set; }

        public int InstaPosts { get; set; }
    }
}