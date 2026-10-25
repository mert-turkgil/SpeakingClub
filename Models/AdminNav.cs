using System.Collections.Generic;
using System.Linq;

namespace SpeakingClub.Models
{
    // The admin panel's sections: the sidebar, the dashboard shortcuts and each section page's header
    // and "How do I…?" guide are all built from this one list. Written for people who are not at
    // home with computers: short sentences, one action per step, the button names exactly as they
    // appear on screen.
    public record AdminGuide(string Title, string[] Steps, string? Tip = null);

    public record AdminSection(
        string Key,
        string Group,
        string Controller,
        string Action,
        string Icon,
        string Title,
        string Lead,
        string? CreateAction,
        string? CreateLabel,
        bool AdminOnly,
        AdminGuide Guide);

    public static class AdminNav
    {
        public const string GroupContent = "content";
        public const string GroupOrganize = "organize";
        public const string GroupPeople = "people";
        public const string GroupSite = "site";

        public static string GroupTitle(string group, bool de) => group switch
        {
            GroupContent => de ? "Inhalte" : "İçerik",
            GroupOrganize => de ? "Ordnen" : "Düzenleme",
            GroupPeople => de ? "Lernende & Team" : "Öğrenciler ve ekip",
            GroupSite => de ? "Website" : "Site",
            _ => group
        };

        public static IReadOnlyList<AdminSection> Build(bool de)
        {
            string T(string tr, string deText) => de ? deText : tr;

            return new List<AdminSection>
            {
                new("blogs", GroupContent, "Admin", "Blogs", "fa-solid fa-feather-pointed",
                    T("Blog yazıları", "Blogbeiträge"),
                    T("Sitedeki yazılarınız. Yeni yazı ekleyin, var olanı düzeltin ya da yayından kaldırın.",
                      "Ihre Beiträge auf der Website. Neue Beiträge schreiben, bestehende bearbeiten oder offline nehmen."),
                    "BlogCreate", T("Yeni yazı", "Neuer Beitrag"), false,
                    new(T("Yeni bir yazı nasıl eklenir?", "Wie füge ich einen neuen Beitrag hinzu?"), new[]
                    {
                        T("Sağ üstteki <b>Yeni yazı</b> düğmesine basın.", "Klicken Sie oben rechts auf <b>Neuer Beitrag</b>."),
                        T("<b>Başlık</b> ve <b>Yazar</b> kutularını doldurun. Bu ikisi zorunludur.", "Füllen Sie <b>Titel</b> und <b>Autor</b> aus. Diese beiden sind Pflicht."),
                        T("<b>İçerik</b> sekmesine geçip yazınızı yazın. Word'den kopyalayıp yapıştırabilirsiniz.", "Wechseln Sie zum Reiter <b>Inhalt</b> und schreiben Sie den Text. Sie können ihn auch aus Word kopieren."),
                        T("<b>Görseller ve dosyalar</b> sekmesinden bir kapak fotoğrafı seçin.", "Wählen Sie im Reiter <b>Bilder & Dateien</b> ein Titelbild."),
                        T("En alttaki <b>Kaydet</b> düğmesine basın. Yazı hemen sitede görünür.", "Klicken Sie unten auf <b>Speichern</b>. Der Beitrag erscheint sofort auf der Website.")
                    }, T("Yazıyı henüz göstermek istemiyorsanız <b>Yayında</b> anahtarını kapatın; yazı taslak olarak saklanır.",
                         "Soll der Beitrag noch nicht erscheinen, schalten Sie <b>Veröffentlicht</b> aus – er bleibt als Entwurf gespeichert."))),

                new("quizzes", GroupContent, "Admin", "Quizzes", "fa-solid fa-clipboard-check",
                    T("Sınavlar", "Prüfungen"),
                    T("Öğrencilerin çözdüğü seviye sınavları ve testler.", "Die Einstufungstests und Prüfungen, die Lernende ablegen."),
                    "QuizCreate", T("Yeni sınav", "Neue Prüfung"), false,
                    new(T("Yeni bir sınav nasıl hazırlanır?", "Wie erstelle ich eine neue Prüfung?"), new[]
                    {
                        T("<b>Yeni sınav</b> düğmesine basın.", "Klicken Sie auf <b>Neue Prüfung</b>."),
                        T("Sınavın adını ve kısa bir açıklamasını yazın, bir kategori seçin.", "Geben Sie einen Namen und eine kurze Beschreibung ein und wählen Sie eine Kategorie."),
                        T("<b>Yeni soru ekle</b> ile soruları yazın. Her sorunun cevaplarını girin ve doğru cevabın yanındaki <b>Doğru</b> anahtarını açın.", "Fügen Sie mit <b>Neue Frage</b> die Fragen hinzu. Tragen Sie die Antworten ein und schalten Sie bei der richtigen Antwort <b>Richtig</b> ein."),
                        T("Süre kutusunu boş bırakırsanız süre soru sayısına göre kendiliğinden hesaplanır.", "Lassen Sie das Zeitfeld leer, wird die Zeit automatisch aus der Zahl der Fragen berechnet."),
                        T("<b>Kaydet</b> düğmesine basın.", "Klicken Sie auf <b>Speichern</b>.")
                    }, T("Sınavın seviyesini göstermek için <b>A1, A2, B1, B2, C1</b> veya <b>C2</b> adlı bir etiket ekleyin. Öğrenciler sınav listesinde seviyeye göre arayabilir.",
                         "Damit eine Prüfung ihr Niveau zeigt, geben Sie ihr ein Schlagwort <b>A1, A2, B1, B2, C1</b> oder <b>C2</b>. Lernende können dann nach Niveau filtern."))),

                new("questions", GroupContent, "Admin", "Questions", "fa-solid fa-circle-question",
                    T("Sorular", "Fragen"),
                    T("Tüm sınavların soruları tek listede. Bir soruyu düzeltmek için buradan da ulaşabilirsiniz.",
                      "Die Fragen aller Prüfungen in einer Liste. Auch von hier aus können Sie eine Frage korrigieren."),
                    "QuestionCreate", T("Yeni soru", "Neue Frage"), false,
                    new(T("Bir soru nasıl eklenir?", "Wie füge ich eine Frage hinzu?"), new[]
                    {
                        T("<b>Yeni soru</b> düğmesine basın.", "Klicken Sie auf <b>Neue Frage</b>."),
                        T("Soruyu yazın ve hangi sınava ait olduğunu seçin.", "Schreiben Sie die Frage und wählen Sie, zu welcher Prüfung sie gehört."),
                        T("Cevapları yazın ve doğru olanın <b>Doğru cevap</b> anahtarını açın. Birden fazla doğru cevap olabilir.", "Tragen Sie die Antworten ein und schalten Sie bei der richtigen <b>Richtige Antwort</b> ein. Es dürfen mehrere richtig sein."),
                        T("İsterseniz bir resim, ses dosyası veya YouTube bağlantısı ekleyin.", "Auf Wunsch fügen Sie ein Bild, eine Audiodatei oder einen YouTube-Link hinzu."),
                        T("<b>Kaydet</b> düğmesine basın.", "Klicken Sie auf <b>Speichern</b>.")
                    }, T("Bir soruyu silerseniz öğrencilerin daha önce aldığı puanlar değişmez.",
                         "Wenn Sie eine Frage löschen, bleiben die bisherigen Punktzahlen der Lernenden erhalten."))),

                new("words", GroupContent, "Admin", "Words", "fa-solid fa-spell-check",
                    T("Kelimeler", "Wörter"),
                    T("Sözlük sayfasında gösterilen kelimeler ve açıklamaları.", "Die Wörter und Erklärungen, die im Wörterbuch angezeigt werden."),
                    "WordCreate", T("Yeni kelime", "Neues Wort"), false,
                    new(T("Bir kelime nasıl eklenir?", "Wie füge ich ein Wort hinzu?"), new[]
                    {
                        T("<b>Yeni kelime</b> düğmesine basın.", "Klicken Sie auf <b>Neues Wort</b>."),
                        T("Kelimeyi ve anlamını yazın. Bu ikisi zorunludur.", "Geben Sie das Wort und seine Bedeutung ein. Beides ist Pflicht."),
                        T("Örnek cümle, okunuş ve eş anlamlılar isteğe bağlıdır.", "Beispielsatz, Aussprache und Synonyme sind freiwillig."),
                        T("<b>Kaydet</b> düğmesine basın.", "Klicken Sie auf <b>Speichern</b>.")
                    })),

                new("slides", GroupContent, "Admin", "Slides", "fa-solid fa-images",
                    T("Ana sayfa slaytları", "Startseiten-Slider"),
                    T("Ana sayfanın en üstünde kayan büyük görseller.", "Die großen Bilder, die oben auf der Startseite wechseln."),
                    "CarouselCreate", T("Yeni slayt", "Neues Bild"), false,
                    new(T("Yeni bir slayt nasıl eklenir?", "Wie füge ich ein neues Slider-Bild hinzu?"), new[]
                    {
                        T("<b>Yeni slayt</b> düğmesine basın.", "Klicken Sie auf <b>Neues Bild</b>."),
                        T("Bir başlık ve kısa bir açıklama yazın.", "Geben Sie einen Titel und eine kurze Beschreibung ein."),
                        T("Bir görsel yükleyin. Geniş, yatay bir fotoğraf en iyi sonucu verir.", "Laden Sie ein Bild hoch. Ein breites Querformat-Foto wirkt am besten."),
                        T("Türkçe ve Almanca metinleri doldurun, sonra <b>Kaydet</b> düğmesine basın.", "Füllen Sie die türkischen und deutschen Texte aus und klicken Sie dann auf <b>Speichern</b>.")
                    })),

                new("categories", GroupOrganize, "Admin", "Categories", "fa-solid fa-folder-open",
                    T("Kategoriler", "Kategorien"),
                    T("Yazıları ve sınavları gruplayan başlıklar (ör. Dilbilgisi, Sağlık).", "Überschriften, die Beiträge und Prüfungen gruppieren (z. B. Grammatik, Gesundheit)."),
                    "CategoryCreate", T("Yeni kategori", "Neue Kategorie"), false,
                    new(T("Bir kategori nasıl eklenir?", "Wie füge ich eine Kategorie hinzu?"), new[]
                    {
                        T("<b>Yeni kategori</b> düğmesine basın.", "Klicken Sie auf <b>Neue Kategorie</b>."),
                        T("Bir ad yazın.", "Geben Sie einen Namen ein."),
                        T("İsterseniz bu kategoriye girecek yazıları ve sınavları işaretleyin.", "Markieren Sie auf Wunsch die Beiträge und Prüfungen, die dazugehören."),
                        T("<b>Kaydet</b> düğmesine basın.", "Klicken Sie auf <b>Speichern</b>.")
                    }, T("Bir yazı ya da sınav yalnızca bir kategoriye ait olabilir.", "Ein Beitrag oder eine Prüfung gehört immer nur zu einer Kategorie."))),

                new("tags", GroupOrganize, "Admin", "Tags", "fa-solid fa-tags",
                    T("Etiketler", "Schlagwörter"),
                    T("Yazılara ve sınavlara eklenen kısa etiketler (ör. A1, kelime, dinleme).", "Kurze Schlagwörter für Beiträge und Prüfungen (z. B. A1, Wortschatz, Hören)."),
                    "TagCreate", T("Yeni etiket", "Neues Schlagwort"), false,
                    new(T("Bir etiket nasıl eklenir?", "Wie füge ich ein Schlagwort hinzu?"), new[]
                    {
                        T("<b>Yeni etiket</b> düğmesine basın.", "Klicken Sie auf <b>Neues Schlagwort</b>."),
                        T("Kısa bir ad yazın ve <b>Kaydet</b> düğmesine basın.", "Geben Sie einen kurzen Namen ein und klicken Sie auf <b>Speichern</b>."),
                        T("Etiketi bir yazıya ya da sınava, o yazının veya sınavın düzenleme sayfasından ekleyin.", "Weisen Sie das Schlagwort auf der Bearbeiten-Seite eines Beitrags oder einer Prüfung zu.")
                    }, T("<b>A1, A2, B1, B2, C1, C2</b> etiketleri özeldir: sınav listesinde sınavın seviyesi olarak görünür.",
                         "Die Schlagwörter <b>A1, A2, B1, B2, C1, C2</b> sind besonders: Sie erscheinen in der Prüfungsliste als Niveau."))),

                new("results", GroupPeople, "QuizMonitor", "Index", "fa-solid fa-chart-column",
                    T("Sınav sonuçları", "Prüfungsergebnisse"),
                    T("Öğrencilerin sınav sonuçları, kural ihlalleri ve canlı takip.", "Die Ergebnisse der Lernenden, Regelverstöße und Live-Verfolgung."),
                    null, null, false,
                    new(T("Sonuçlar nasıl okunur?", "Wie lese ich die Ergebnisse?"), new[]
                    {
                        T("Üstteki kutulardan bir öğrenci veya sınav seçerek listeyi daraltın.", "Wählen Sie oben eine Lernende oder eine Prüfung, um die Liste einzugrenzen."),
                        T("Bir satıra tıklayarak o denemenin ayrıntılarını açın.", "Klicken Sie auf eine Zeile, um die Details dieses Versuchs zu öffnen."),
                        T("<b>Excel</b> veya <b>PDF</b> düğmeleriyle listeyi bilgisayarınıza indirin.", "Laden Sie die Liste mit <b>Excel</b> oder <b>PDF</b> herunter.")
                    })),

                new("users", GroupPeople, "Admin", "Users", "fa-solid fa-user-group",
                    T("Kullanıcılar", "Benutzer"),
                    T("Siteye kayıtlı öğrenciler ve öğretmenler.", "Die registrierten Lernenden und Lehrkräfte."),
                    "UserCreate", T("Yeni kullanıcı", "Neuer Benutzer"), true,
                    new(T("Bir kullanıcıyı nasıl bulur ve düzenlerim?", "Wie finde und bearbeite ich einen Benutzer?"), new[]
                    {
                        T("Arama kutusuna kişinin adını veya e-postasını yazın.", "Geben Sie Namen oder E-Mail der Person in das Suchfeld ein."),
                        T("<b>Düzenle</b> düğmesiyle bilgilerini, rolünü veya şifresini değiştirin.", "Mit <b>Bearbeiten</b> ändern Sie Angaben, Rolle oder Passwort."),
                        T("<b>Yeni kullanıcı</b> ile elle hesap açabilirsiniz. <b>Şifre oluştur</b> düğmesi güvenli bir şifre üretir.", "Mit <b>Neuer Benutzer</b> legen Sie selbst ein Konto an. <b>Passwort erzeugen</b> erstellt ein sicheres Passwort.")
                    }, T("Bir öğrenciyi geçici olarak durdurmak için silmek yerine düzenleme sayfasındaki <b>Hesabı kilitle</b> seçeneğini kullanın.",
                         "Um jemanden vorübergehend zu sperren, nutzen Sie statt Löschen <b>Konto sperren</b> auf der Bearbeiten-Seite."))),

                new("roles", GroupPeople, "Admin", "Roles", "fa-solid fa-user-shield",
                    T("Roller", "Rollen"),
                    T("Kimin panelde neyi yapabileceğini belirleyen gruplar.", "Gruppen, die festlegen, wer im Panel was tun darf."),
                    "RoleCreate", T("Yeni rol", "Neue Rolle"), true,
                    new(T("Roller ne işe yarar?", "Wofür sind Rollen da?"), new[]
                    {
                        T("<b>Root</b> ve <b>Admin</b>: paneldeki her şeyi yapabilir.", "<b>Root</b> und <b>Admin</b>: dürfen im Panel alles."),
                        T("<b>Teacher</b> (öğretmen): yazı, sınav, soru, kelime ve slaytları yönetir.", "<b>Teacher</b> (Lehrkraft): verwaltet Beiträge, Prüfungen, Fragen, Wörter und Slider."),
                        T("<b>User</b>: normal öğrenci hesabı; panele giremez.", "<b>User</b>: normales Lernendenkonto, ohne Zugang zum Panel."),
                        T("Bir kişinin rolünü <b>Kullanıcılar</b> sayfasından, kişiyi düzenleyerek değiştirin.", "Die Rolle einer Person ändern Sie unter <b>Benutzer</b>, indem Sie die Person bearbeiten.")
                    }, T("Bu dört rol sitenin çalışması için gereklidir; adları değiştirilemez ve silinemez.",
                         "Diese vier Rollen braucht die Website; sie können nicht umbenannt oder gelöscht werden."))),

                new("downloads", GroupPeople, "Admin", "DownloadLogs", "fa-solid fa-file-arrow-down",
                    T("İndirme kayıtları", "Download-Protokoll"),
                    T("Blog yazılarındaki dosyaları kimin, ne zaman indirdiği.", "Wer wann Dateien aus Blogbeiträgen heruntergeladen hat."),
                    null, null, true,
                    new(T("Bu liste nasıl kullanılır?", "Wie nutze ich diese Liste?"), new[]
                    {
                        T("Bir kişiyi veya dosyayı bulmak için arama kutusuna yazın.", "Geben Sie in das Suchfeld eine Person oder Datei ein."),
                        T("Sıralamayı değiştirmek için yandaki seçimden birini seçin ve <b>Filtrele</b>ye basın.", "Wählen Sie daneben eine Sortierung und klicken Sie auf <b>Filtern</b>.")
                    })),

                new("texts", GroupSite, "Admin", "Texts", "fa-solid fa-language",
                    T("Site metinleri", "Website-Texte"),
                    T("Sitedeki sabit yazılar (ör. Hakkımda paragrafları, düğme yazıları). Türkçe ve Almanca hallerini buradan değiştirirsiniz.",
                      "Die festen Texte der Website (z. B. Über-mich-Absätze, Schaltflächen). Hier ändern Sie die türkische und die deutsche Fassung."),
                    null, null, true,
                    new(T("Sitedeki bir yazıyı nasıl değiştiririm?", "Wie ändere ich einen Text der Website?"), new[]
                    {
                        T("Değiştirmek istediğiniz yazının birkaç kelimesini, sitede gördüğünüz haliyle arama kutusuna yazın.", "Geben Sie ein paar Wörter des Textes so ein, wie Sie sie auf der Website sehen."),
                        T("Bulduğunuz satırdaki <b>Düzenle</b> düğmesine basın.", "Klicken Sie in der gefundenen Zeile auf <b>Bearbeiten</b>."),
                        T("Türkçe ve Almanca metni düzeltip <b>Kaydet</b> düğmesine basın.", "Korrigieren Sie den türkischen und den deutschen Text und klicken Sie auf <b>Speichern</b>."),
                        T("Değişiklik hemen yayına girer. Sitedeki sayfayı yenileyerek kontrol edin.", "Die Änderung ist sofort online. Laden Sie die Seite der Website neu, um sie zu prüfen.")
                    }, T("Metinde <b>&lt;strong&gt;</b> gibi işaretler varsa silmeyin; yazının kalın görünmesini sağlarlar.",
                         "Lassen Sie Zeichen wie <b>&lt;strong&gt;</b> im Text stehen – sie machen ihn fett.")))
            };
        }

        public static AdminSection? Find(string key, bool de) => Build(de).FirstOrDefault(s => s.Key == key);

        // Older links (and the old single-page panel) jump to a section with ?scrollTo=…Management.
        public static string? FromLegacyAnchor(string? anchor) => anchor switch
        {
            "RoleManagement" => "roles",
            "UserManagement" => "users",
            "ResourceManagement" => "texts",
            "WordsManagement" => "words",
            "QuizzesManagement" => "quizzes",
            "BlogManagement" => "blogs",
            "CategoryManagement" => "categories",
            "TagManagement" => "tags",
            "QuestionsManagement" => "questions",
            "SlideListManagement" => "slides",
            _ => null
        };
    }
}
