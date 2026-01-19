using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpeakingClub.Models
{
    #nullable disable
    public class AboutPageViewModel
    {
        public string PageTitle { get; set; }
        public HeroSectionModel HeroSection { get; set; }
        public AboutSectionModel AboutSection { get; set; }
        public ProcessSectionModel ProcessSection { get; set; }
        public FAQSectionModel FAQSection { get; set; }
        public ContactSectionModel ContactSection { get; set; }
        public ContactFormViewModel ContactForm { get; set; } = new ContactFormViewModel();
    }

    public class HeroSectionModel
    {
        // "Merhaba, Ben Suna!" ifadesinde span’larla ayrılan kısımlar da dahil
        public string Greeting { get; set; }       // Örn: "Merhaba, Ben Suna!" veya çevirisine göre "Hello, I'm Suna!" gibi.
        public string Subtitle { get; set; }       // Örn: "Almanca Konuşma Kulübü"
        public string ImageAlt { get; set; }        // Örn: "Suna"
    }

    public class AboutSectionModel
    {
        public string Title { get; set; }           // Örn: "Hakkımda" / "About Me" / "Über mich"
        public string Paragraph1 { get; set; }
        public string Paragraph2 { get; set; }
        public string Paragraph3 { get; set; }
        public string Paragraph4 { get; set; }
        public string Paragraph5 { get; set; }
        public string ImageAlt { get; set; }        // Örn: "Suna Hoca" / "Suna, the Instructor" / "Suna, die Lehrerin"
    }

    public class ProcessSectionModel
    {
        public string Title { get; set; }           // Örn: "Online Almanca Konuşma Kulübü Nasıl İşliyor?" vs.
        public List<ProcessStepModel> Steps { get; set; }
    }

    public class ProcessStepModel
    {
        public int Id { get; set; }
        public string Title { get; set; }           // Örn: "Seviye Belirleme", "Konuşma Partneri", vb.
        public string Description { get; set; }     // Adım açıklaması.
        public string LinkText { get; set; }  
        public string IconClass { get; set; }       // Örn: "DETAY →" veya "DETAIL →"
    }

    public class FAQSectionModel
    {
        public string Title { get; set; }           // Örn: "Sıkça Sorulan Sorular" vs.
        public List<FAQItemModel> FAQItems { get; set; }
    }

    public class FAQItemModel
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class ContactSectionModel
    {
        public string Title { get; set; }           // Örn: "İletişime Geçin" / "Contact Us" / "Kontakt aufnehmen"
        public string NameLabel { get; set; }
        public string EmailLabel { get; set; }
        public string MessageLabel { get; set; }
        public string ButtonText { get; set; }
    }
    
}