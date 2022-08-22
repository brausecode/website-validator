using System.Collections.Generic;
using HtmlAgilityPack;
using WebsiteValidator.BL.Interfaces;

namespace WebsiteValidator.BL.Classes
{
    public class HtmlAgilityBasedUrlExtractor : IUrlExtractor
    {
    
        private string[] ExtractUrlsByTagAndAttribute(string pageContents, string tag, string attribute) {
            var result = new List<string>();

            var document = new HtmlDocument();
            document.LoadHtml(pageContents);
            var linkNodes = document.DocumentNode.SelectNodes($"//{tag}[@{attribute}]");

            if (linkNodes != null)
            {
                foreach (var link in linkNodes)
                {
                    var linkUrl = link.GetAttributeValue(attribute, "");
                    if (!string.IsNullOrWhiteSpace(linkUrl))
                    {
                        result.Add(linkUrl);
                    }
                }
            }

            return result.ToArray();
        }
    
        public string[] ExtractUrls(string pageContents)
        {
            var result = new List<string>();

            result.AddRange(ExtractUrlsByTagAndAttribute(pageContents, "a", "href"));
            result.AddRange(ExtractUrlsByTagAndAttribute(pageContents, "img", "src"));

            return result.ToArray();
        }
    }
}
