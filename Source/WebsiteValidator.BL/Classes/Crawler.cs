using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using HtmlAgilityPack;
using WebsiteValidator.BL.ExtensionMethods;
using WebsiteValidator.BL.Interfaces;

namespace WebsiteValidator.BL.Classes
{
    public class Crawler
    {
        private readonly IDownloadAWebpage _downloadWebpage;
        private readonly IOutputHelper _outputHelper;
        private readonly int _limit;
        private readonly Dictionary<string, bool> _urlsWithScrapedStatus = new Dictionary<string, bool>();
        private readonly List<IUrlInformation> _scrapeResults = new List<IUrlInformation>();
        private string _baseUrl;
        
        public Crawler(string url, IDownloadAWebpage downloadWebpage, IOutputHelper outputHelper, int limit,
            string[] additionalKnownLinks)
        {
            _downloadWebpage = downloadWebpage;
            _outputHelper = outputHelper;
            _limit = limit;
            _urlsWithScrapedStatus.Add(url, false);

            foreach (var link in additionalKnownLinks)
            {
                if (!string.IsNullOrWhiteSpace(link.Trim()))
                {
                    if (!_urlsWithScrapedStatus.ContainsKey(link))
                        _urlsWithScrapedStatus.Add(link, false);
                }
            }
            
            _baseUrl = url;
        }

        public IUrlInformation[] CrawlEverything()
        {
            var nextUrl = GetNextUrlToCrawl();
            var scrapedUrls = 0;

            while (!string.IsNullOrWhiteSpace(nextUrl))
            {
                _urlsWithScrapedStatus[nextUrl] = true;
                StatusReport(scrapedUrls);
                scrapedUrls += 1; 
                Console.WriteLine($" - processing {nextUrl} ...");

                try
                {
                    var download = _downloadWebpage.Download(nextUrl); 
                    
                    //if (((int)download.Result.HttpCode < 200) || ((int)download.Result.HttpCode > 299))
                    if ((int)download.Result.HttpCode == 500)
                    {
                            // Take another try..
                            download = _downloadWebpage.Download(nextUrl);
                    }
                    var links = download
                        .ExtractUrls()
                        .ToAbsoluteUrls(_baseUrl);

                    // wir wollen nicht den Inhalt von JPG/PDF/etc. Dateien im JSON haben
                    string bereinigterRawContent = download.Result.RawContent;
                    if (   nextUrl.EndsWith(".jpg")
                        || nextUrl.EndsWith(".jpeg")
                        || nextUrl.EndsWith(".png")
                        || nextUrl.EndsWith(".webp")
                        || nextUrl.EndsWith(".pdf")
                        || nextUrl.EndsWith(".mp3")
                        || nextUrl.EndsWith(".m4r")
                        || nextUrl.EndsWith(".exe")
                        || nextUrl.EndsWith(".zip")
                        || nextUrl.EndsWith(".svg")
                        || nextUrl.EndsWith(".ico")
                       )
                    {
                        bereinigterRawContent = "";
                    }
                    _scrapeResults.Add(new UrlInformation(
                        nextUrl, 
                        links, 
                        download.Result.HttpCode,
                        bereinigterRawContent,
                        ExtractInnerText(bereinigterRawContent),
                        download.Result.RawContent.Length
                        ));
                    
                    foreach (var link in links)
                    {
                        if (!_urlsWithScrapedStatus.ContainsKey(link))
                        {
                            _urlsWithScrapedStatus.Add(link, false);
                            //Console.WriteLine($"     + {link}");
                        }
                        else
                        {
                            //Console.WriteLine($"     / {link} already known");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                nextUrl = GetNextUrlToCrawl();

                // if limit given, we only crawl n pages
                if (_limit > 0)
                {
                    if (scrapedUrls > _limit)
                    {
                        break;
                    }
                }
            }

            return _scrapeResults.ToArray();
        }

        private string ExtractInnerText(string resultRawContent)
        {
            if (!resultRawContent.ToLower().Contains("html"))
            {
                return "";
            }

            try
            {
                var blacklist = new List<string>();
                blacklist.Add("script");
                blacklist.Add("style");
                blacklist.Add("head");
                var document = new HtmlDocument();
                document.LoadHtml(resultRawContent);
                var result = new StringBuilder();

                foreach(HtmlNode node in document.DocumentNode.SelectNodes("//text()"))
                {
                    if (node.InnerText.Trim() != "" &&
                        !blacklist.Contains(node.ParentNode.Name)) {
                        //Console.WriteLine(node.ParentNode.Name + ":");
                        //Console.WriteLine("text=" + node.InnerText);
                        result.AppendLine(node.InnerText);
                        //Console.ReadKey();
                    }
                }
                
                return HttpUtility.HtmlDecode(result.ToString());
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private void StatusReport(int scrapedUrls)
        {
            var potenialUrlsToCrawl = 
                _urlsWithScrapedStatus.
                    Where(x => 
                        x.Value == false
                        && x.Key.StartsWith(_baseUrl)).ToArray();
            
            Console.WriteLine($"## STATUS : {scrapedUrls} urls scraped, {_urlsWithScrapedStatus.Count} urls known, {potenialUrlsToCrawl.Length} urls to crawl remaining");
        }

        private string GetNextUrlToCrawl()
        {
            var potenialUrlsToCrawl = 
                _urlsWithScrapedStatus.
                    FirstOrDefault(x => 
                        x.Value == false
                        && x.Key.StartsWith(_baseUrl)).Key;

            return potenialUrlsToCrawl;
        }
    }
}