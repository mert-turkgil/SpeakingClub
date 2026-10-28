using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SpeakingClub.Services.Reports
{
    /// <summary>
    /// CSV downloads of the admin reports, made for Excel: UTF-8 with BOM and ";" between columns (what Turkish and
    /// German Excel expect).
    /// </summary>
    public static class ReportCsv
    {
        public static StreamWriter Start(HttpResponse response, string fileName)
        {
            response.ContentType = "text/csv; charset=utf-8";
            response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}-{ReportTime.Today:yyyy-MM-dd}.csv\"";
            return new StreamWriter(response.Body, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        public static Task WriteRowAsync(StreamWriter writer, params string[] cells) => writer.WriteLineAsync(string.Join(';', cells.Select(Cell)));

        // A cell Excel reads as text: quoted when needed, and a leading = + - @ neutralised so a name can't run as a formula
        public static string Cell(string value)
        {
            if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
            return value.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0 ? "\"" + value.Replace("\"", "\"\"") + "\"" : value;
        }
    }
}
