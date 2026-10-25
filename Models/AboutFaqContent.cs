using System;

namespace SpeakingClub.Models
{
    /// <summary>
    /// Questions and answers of the FAQ concierge on the About page (Turkish and German).
    /// The chat widget, its no-JavaScript fallback and the FAQPage structured data are all built from this
    /// list, so an answer is changed here once. Keywords feed the "type your question" search.
    /// </summary>
    public static class AboutFaqContent
    {
        public const string WhatsAppNumber = "491603866222";      // +49 160 3866222
        public const string Email = "almancakonus.info@gmail.com";

        public static string WhatsAppUrl(string text) => $"https://wa.me/{WhatsAppNumber}?text={Uri.EscapeDataString(text)}";
        public static string WhatsAppGreeting(bool de) =>
            de ? "Hallo! Ich schreibe Ihnen über Ihre Website almanca-konus.com." : "Merhaba! almanca-konus.com üzerinden yazıyorum.";

        public record FaqAction(string Label, string Href, string Icon, string Kind = "link");
        public record FaqItem(string Id, string Question, string Answer, string[] Keywords, FaqAction[] Actions);
        public record FaqTopic(string Id, string Icon, string Title, string Intro, FaqItem[] Items);

        public static FaqTopic[] Build(bool de)
        {
            string T(string tr, string german) => de ? german : tr;
            var quizzes = de ? "/pruefungen" : "/sinavlar";
            var doctors = de ? "/aerztedeutsch" : "/hekim-almancasi";
            var nurses = de ? "/pflegedeutsch" : "/hemsirelik-almancasi";

            var whatsapp = new FaqAction(T("WhatsApp'tan yazın", "Per WhatsApp schreiben"), WhatsAppUrl(WhatsAppGreeting(de)), "fa-brands fa-whatsapp", "whatsapp");
            var toQuizzes = new FaqAction(T("Seviye sınavlarına git", "Zu den Einstufungstests"), quizzes, "fa-solid fa-graduation-cap", "primary");
            var toRegister = new FaqAction(T("Ücretsiz kayıt ol", "Kostenlos registrieren"), "/Account/Register", "fa-solid fa-user-plus", "primary");
            var toForm = new FaqAction(T("İletişim formu", "Kontaktformular"), "#contact-form", "fa-solid fa-paper-plane");
            var toMail = new FaqAction(T("E-posta gönderin", "E-Mail senden"), "mailto:" + Email, "fa-regular fa-envelope");

            return new[]
            {
                new FaqTopic("lessons", "fa-solid fa-comments",
                    T("Dersler ve konuşma kulübü", "Unterricht & Gesprächsclub"),
                    T("Derslerle ilgili en çok sorulanlar bunlar. Hangisini merak ediyorsunuz?", "Das wird zum Unterricht am häufigsten gefragt. Was möchten Sie wissen?"),
                    new[]
                    {
                        new FaqItem("how", T("Konuşma derslerini nasıl yapıyorsunuz?", "Wie laufen die Gesprächsstunden ab?"),
                            T("Derslerde sürekli siz aktif rol alırsınız; cümleleri size kurdururum. Hata yapmaktan korkmadan, rahat bir ortamda konuşursunuz. Daha doğal ifade edebilmeniz için alternatif cümleler önerir, anında geri bildirim veririm.",
                              "Im Unterricht sind Sie durchgehend aktiv – ich lasse Sie die Sätze selbst bilden. Sie sprechen ohne Angst vor Fehlern in entspannter Atmosphäre. Ich schlage alternative Sätze vor, damit Sie sich natürlicher ausdrücken, und gebe sofort Feedback."),
                            new[] { "nasıl", "ders", "konuşma", "yöntem", "wie", "ablauf", "methode", "unterricht" }, new[] { whatsapp }),
                        new FaqItem("groups", T("Gruplar kaç kişiden oluşuyor?", "Wie viele Personen sind in einer Gruppe?"),
                            T("Gruplarımız en az 4, en fazla 6 kişiden oluşuyor. Daha kalabalık bir grupta herkes yeterince konuşamaz.",
                              "Unsere Gruppen bestehen aus mindestens 4 und höchstens 6 Personen. In größeren Gruppen käme nicht jeder ausreichend zu Wort."),
                            new[] { "grup", "kişi", "kaç", "kalabalık", "gruppe", "personen", "teilnehmer" }, Array.Empty<FaqAction>()),
                        new FaqItem("grammar", T("Gramer dersi veriyor musunuz?", "Gibt es auch Grammatikunterricht?"),
                            T("Evet, ama kısaca: o haftanın gramer konusunu, cümleleri doğru kurabilmeniz için özetliyorum. Vaktin çoğunu sizi konuşturmaya ve yeni cümleler kurdurmaya ayırıyoruz.",
                              "Ja, aber kurz: Ich fasse das Grammatikthema der Woche zusammen, damit Sie Sätze korrekt bilden. Die meiste Zeit gehört dem Sprechen und neuen Sätzen."),
                            new[] { "gramer", "grammar", "dilbilgisi", "grammatik" }, Array.Empty<FaqAction>()),
                        new FaqItem("private", T("Birebir ders de var mı?", "Gibt es auch Einzelunterricht?"),
                            T("Evet. Konuşma kulüplerinin yanında birebir konuşma dersleri de veriyorum. Uygun saatler ve ayrıntılar için WhatsApp'tan yazabilirsiniz.",
                              "Ja. Neben den Gesprächskreisen biete ich auch Einzelstunden an. Für Termine und Details schreiben Sie mir einfach per WhatsApp."),
                            new[] { "birebir", "özel", "bire bir", "kişisel", "einzel", "privat", "einzelunterricht" }, new[] { whatsapp }),
                        new FaqItem("online", T("Konuşma kulübü online mı?", "Findet der Gesprächsclub online statt?"),
                            T("Evet, konuşma kulübü online yapılıyor; nerede olursanız olun katılabilirsiniz.",
                              "Ja, der Gesprächsclub findet online statt – Sie können von überall teilnehmen."),
                            new[] { "online", "uzaktan", "internet", "zoom", "yüz yüze", "ort", "präsenz" }, Array.Empty<FaqAction>()),
                        new FaqItem("topics", T("Derslerde hangi konular konuşuluyor?", "Über welche Themen wird gesprochen?"),
                            T("Ders içerikleri hem günlük hem de mesleki konuşma konularından oluşuyor. İhtiyaç duyduğunuzda gramer için de tavsiyelerde bulunuyorum.",
                              "Die Inhalte umfassen alltägliche und berufliche Gesprächsthemen. Bei Bedarf gebe ich auch Grammatiktipps."),
                            new[] { "konu", "içerik", "günlük", "mesleki", "themen", "inhalt", "beruf", "alltag" }, Array.Empty<FaqAction>())
                    }),

                new FaqTopic("level", "fa-solid fa-graduation-cap",
                    T("Seviye ve sınavlar", "Niveau & Prüfungen"),
                    T("Seviye belirleme ve online sınavlar hakkında:", "Zur Niveaubestimmung und den Online-Prüfungen:"),
                    new[]
                    {
                        new FaqItem("find-level", T("Seviyemi nasıl öğrenirim?", "Wie finde ich mein Niveau heraus?"),
                            T("Sitedeki online seviye sınavlarını çözebilirsiniz: süreli, tam ekran ve otomatik kayıtlı, gerçek bir sınav deneyimi. Bitirdiğinizde puanınızı ve soru soru değerlendirmenizi görürsünüz.",
                              "Legen Sie die Online-Einstufungstests auf der Website ab: mit Zeitlimit, im Vollbild und automatisch gespeichert – wie eine echte Prüfung. Danach sehen Sie Ihr Ergebnis und die Auswertung jeder Frage."),
                            new[] { "seviye", "sınav", "test", "a1", "a2", "b1", "b2", "c1", "niveau", "einstufung", "prüfung" }, new[] { toQuizzes }),
                        new FaqItem("free", T("Seviye sınavları ücretli mi?", "Kosten die Einstufungstests etwas?"),
                            T("Hayır, sitedeki seviye sınavları ücretsiz. Sonuçlarınızın kaydedilmesi için ücretsiz bir hesap oluşturmanız yeterli.",
                              "Nein, die Einstufungstests sind kostenlos. Damit Ihre Ergebnisse gespeichert werden, genügt ein kostenloses Konto."),
                            new[] { "ücretsiz", "bedava", "para", "ücret", "kostenlos", "gratis", "kosten" }, new[] { toRegister }),
                        new FaqItem("placement", T("Gruplara nasıl yerleştiriliyorum?", "Wie werde ich einer Gruppe zugeordnet?"),
                            T("Önce size uygun seviyeyi belirliyoruz; ardından seviyenize uygun konuşma partnerleriyle oluşturulan bir gruba yerleşiyorsunuz.",
                              "Zuerst bestimmen wir Ihr passendes Niveau; danach kommen Sie in eine Gruppe mit Gesprächspartnern auf Ihrem Niveau."),
                            new[] { "yerleş", "grup", "partner", "eşleş", "zuordnung", "partner", "gruppe" }, new[] { toQuizzes, whatsapp }),
                        new FaqItem("rules", T("Online sınavın kuralları neler?", "Welche Regeln gelten bei der Online-Prüfung?"),
                            T("Süre, Başlat'a bastığınızda başlar ve sınav tam ekranda yapılır. Sekme değiştirmek, kopyalamak veya yapıştırmak ihlal sayılır; 3. ihlalde sınav o ana kadarki cevaplarla gönderilir. Cevaplarınız otomatik kaydedilir.",
                              "Die Zeit läuft ab „Starten“, die Prüfung findet im Vollbild statt. Tab-Wechsel, Kopieren oder Einfügen gelten als Verstoß; beim 3. Verstoß wird mit den bisherigen Antworten abgegeben. Antworten werden automatisch gespeichert."),
                            new[] { "kural", "tam ekran", "ihlal", "süre", "regel", "vollbild", "verstoß", "zeit" }, new[] { toQuizzes })
                    }),

                new FaqTopic("clinical", "fa-solid fa-stethoscope",
                    T("Klinik Almanca", "Klinisches Deutsch"),
                    T("Sağlık çalışanlarına yönelik çalışmalar hakkında:", "Zu den Angeboten für Gesundheitsberufe:"),
                    new[]
                    {
                        new FaqItem("health", T("Sağlık çalışanlarıyla çalışıyor musunuz?", "Arbeiten Sie mit Gesundheitsfachkräften?"),
                            T("Evet. Sağlık Almancası eğitimlerinde sık karşılaşılan bir eksiklik var: bilgi var, fakat aktif kullanım ve özgüven yeterince gelişmiyor. Çalışmalarımı tam da bu noktaya odaklıyorum.",
                              "Ja. In der medizinischen Sprachausbildung fehlt oft eines: Das Wissen ist da, aber aktive Anwendung und Selbstvertrauen kommen zu kurz. Genau darauf konzentriere ich mich."),
                            new[] { "sağlık", "doktor", "hekim", "hemşire", "tıp", "klinik", "arzt", "pflege", "medizin", "klinik" }, new[] { whatsapp }),
                        new FaqItem("clinical-pages", T("Sitede hangi klinik konular var?", "Welche klinischen Themen gibt es auf der Website?"),
                            T("Hekimler için anamnez ve doktor mektubu (Arztbrief), hemşireler için hasta iletişimi ve hasta devri (Patientenübergabe) gibi konuları örnek diyaloglarla anlatan sayfalar var.",
                              "Es gibt Seiten mit Beispieldialogen – für Ärztinnen und Ärzte zu Anamnese und Arztbrief, für Pflegekräfte zu Pflegekommunikation und Patientenübergabe."),
                            new[] { "anamnez", "anamnese", "arztbrief", "patientenübergabe", "hasta devri", "kaynak", "material" },
                            new[]
                            {
                                new FaqAction(T("Hekim Almancası", "Ärztedeutsch"), doctors, "fa-solid fa-user-doctor", "primary"),
                                new FaqAction(T("Hemşirelik Almancası", "Pflegedeutsch"), nurses, "fa-solid fa-user-nurse")
                            })
                    }),

                new FaqTopic("about", "fa-regular fa-user",
                    T("Suna hakkında", "Über Suna"),
                    T("Biraz kendimden bahsedeyim:", "Ein wenig über mich:"),
                    new[]
                    {
                        new FaqItem("who", T("Suna kimdir?", "Wer ist Suna?"),
                            T("Almanya'nın Bremen şehrinde doğup büyüdüm; eğitimimi ve çalışma hayatımın ilk yıllarını orada geçirdim. Türkiye'ye yerleştikten sonra devlet okulunda ve özel eğitim kurumlarında çalıştım; pandemiyle birlikte birebir konuşma koçluğuna odaklandım.",
                              "Ich bin in Bremen aufgewachsen und habe dort meine Ausbildung und die ersten Berufsjahre verbracht. In der Türkei habe ich an staatlichen Schulen und privaten Bildungseinrichtungen gearbeitet; seit der Pandemie konzentriere ich mich auf Einzel-Sprechcoaching."),
                            new[] { "kim", "suna", "bremen", "deneyim", "öğretmen", "wer", "erfahrung", "lehrerin" }, Array.Empty<FaqAction>()),
                        new FaqItem("why-speaking", T("Neden konuşma odaklı çalışıyorsunuz?", "Warum der Fokus auf das Sprechen?"),
                            T("Almanca ana dilim ve eğitim sistemimizde konuşma pratiği maalesef yeterince yer almıyor. Almancayı anlayıp konuşurken zorlanan pek çok öğrenci var; en çok keyif aldığım şey de öğrencilerimi motive edip konuşturmak.",
                              "Deutsch ist meine Muttersprache, und Sprechpraxis kommt im Unterricht oft zu kurz. Viele verstehen Deutsch, tun sich aber beim Sprechen schwer – und am meisten Freude macht es mir, sie zu motivieren und zum Sprechen zu bringen."),
                            new[] { "neden", "konuşma", "zorlan", "anlıyorum", "warum", "sprechen", "verstehe" }, Array.Empty<FaqAction>()),
                        new FaqItem("values", T("Derslerinizde neye önem veriyorsunuz?", "Was ist Ihnen im Unterricht wichtig?"),
                            T("Anında geri bildirim, düzenli destek ve yargılanmadan pratik yapabileceğiniz bir ortam. Benim için dil öğrenmek yalnızca sınava hazırlanmak değil; yeni hayatınıza daha güçlü ve hazır hissetmeniz demek.",
                              "Sofortiges Feedback, regelmäßige Unterstützung und eine Atmosphäre, in der Sie ohne Bewertung üben können. Sprachenlernen heißt für mich nicht nur Prüfungsvorbereitung, sondern sich im neuen Leben stärker und bereiter zu fühlen."),
                            new[] { "önem", "geri bildirim", "destek", "özgüven", "wichtig", "feedback", "selbstvertrauen" }, Array.Empty<FaqAction>())
                    }),

                new FaqTopic("contact", "fa-regular fa-calendar-check",
                    T("Kayıt, ücret ve iletişim", "Anmeldung, Preise & Kontakt"),
                    T("Başlamak için bilmeniz gerekenler:", "Was Sie zum Start wissen müssen:"),
                    new[]
                    {
                        new FaqItem("join", T("Nasıl başlarım?", "Wie fange ich an?"),
                            T("En kolayı: önce ücretsiz seviye sınavını çözün, sonra WhatsApp'tan ya da iletişim formundan yazın. Seviyenize ve programınıza uygun grubu birlikte belirleyelim.",
                              "Am einfachsten: Legen Sie zuerst den kostenlosen Einstufungstest ab und schreiben Sie mir dann per WhatsApp oder über das Kontaktformular. Gemeinsam finden wir die passende Gruppe."),
                            new[] { "kayıt", "başla", "katıl", "anmeld", "anfang", "start", "teilnehmen" }, new[] { toQuizzes, whatsapp }),
                        new FaqItem("price", T("Ücretler ne kadar?", "Was kostet der Unterricht?"),
                            T("Ücretler ders türüne (grup ya da birebir) ve programa göre değişiyor. Güncel bilgi için WhatsApp'tan yazmanız yeterli.",
                              "Die Preise hängen von der Unterrichtsform (Gruppe oder Einzel) und dem Programm ab. Für aktuelle Informationen schreiben Sie mir einfach per WhatsApp."),
                            new[] { "ücret", "fiyat", "para", "kaç lira", "euro", "preis", "kosten", "gebühr" }, new[] { whatsapp }),
                        new FaqItem("reach", T("Size nasıl ulaşabilirim?", "Wie erreiche ich Sie?"),
                            T("WhatsApp: +49 160 3866222 · E-posta: " + Email + " · Ya da bu sayfadaki iletişim formunu kullanabilirsiniz.",
                              "WhatsApp: +49 160 3866222 · E-Mail: " + Email + " · Oder nutzen Sie das Kontaktformular auf dieser Seite."),
                            new[] { "iletişim", "ulaş", "telefon", "whatsapp", "mail", "kontakt", "erreichen", "telefon" }, new[] { whatsapp, toMail, toForm })
                    })
            };
        }
    }
}
