using System.Net;
using System.Text.RegularExpressions;

namespace EmailClassification.Application.Helpers
{
    public class HtmlHelper
    {
        public static string StripHtmlTags(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var rawText = WebUtility.HtmlDecode(doc.DocumentNode.InnerText);
            var cleaned = Regex.Replace(rawText, @"\s+", " ").Trim();
            return cleaned;
        }
    }
}
