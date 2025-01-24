using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UI.Entity;
using Microsoft.EntityFrameworkCore;

namespace Data.Configuration
{
    public static class ModelBuilderExtensions
    {
        
         public static void Seed(this ModelBuilder builder)
        {
            builder.Entity<Quiz>().HasData(  
            new Quiz() { Id = 1, Soru = "S1-S1P", Cevap = "Text4", Text = new string[] { "Text1","Text2","Text3","Text4" }, Zaman = 60, Date = DateTime.Now },
            new Quiz() { Id = 2, Soru = "S2-S2P", Cevap = "Text1", Text = new string[] { "Text1","Text2","Text3","Text4" }, Zaman = 60, Date = DateTime.Now }
            );
            // Seeding Blog data
            builder.Entity<Blog>().HasData(
                new Blog()
                {
                    Id = 1,
                    Title = "Die Bedeutung der deutschen Artikel: Der, Die, Das",
                    Content = "<p>In diesem Beitrag werden wir die Verwendung der deutschen Artikel <strong>der</strong>, <strong>die</strong> und <strong>das</strong> untersuchen. Diese kleinen Wörter sind entscheidend für das Verständnis und die korrekte Verwendung der deutschen Sprache.</p><p><strong>Der</strong> wird für maskuline Nomen verwendet, <strong>die</strong> für feminine und <strong>das</strong> für neutrale Nomen. Zum Beispiel:</p><ul><li>Der Mann</li><li>Die Frau</li><li>Das Kind</li></ul><p>Es ist wichtig, die Artikel mit den Nomen auswendig zu lernen, da sie keinen festen Regeln folgen.</p>",
                    Author = "Frau Müller",
                    DateCreated = DateTime.UtcNow
                },
                new Blog()
                {
                    Id = 2,
                    Title = "Türkçe'de Fiillerin Çekimi: Şimdiki Zaman",
                    Content = "<p>Bu yazıda Türkçe'de fiillerin şimdiki zaman çekimini ele alacağız. Şimdiki zaman, şu anda gerçekleşen eylemleri ifade etmek için kullanılır.</p><p>Örnekler:</p><ul><li>Ben <strong>yazıyorum</strong>.</li><li>Sen <strong>okuyorsun</strong>.</li><li>O <strong>konuşuyor</strong>.</li></ul><p>Fiilin köküne uygun ekleri ekleyerek fiili çekimleyebiliriz.</p>",
                    Author = "Mehmet Hoca",
                    DateCreated = DateTime.UtcNow
                });
        }
    }
}