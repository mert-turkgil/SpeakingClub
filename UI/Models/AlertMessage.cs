using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



namespace UI.Models
{
    public class AlertMessage
    {
        #nullable disable
        public string Title { get; set; }
        public string Message { get; set; }
        public string AlertType { get; set; }

        public string icon { get; set; }

        public string icon2 { get; set; }
    }
}