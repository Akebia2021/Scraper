using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WebScraper.Utility;
using static WebScraper.Scraper;
using Abot2.Poco;
using Abot2.Crawler;
using System.Net;
using Abot2.Core;
using Serilog;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WebScraper
{
    public class Test
    {
        public static async Task TestCode()
        {
            List<string> urls = new List<string>();
            urls.Add("https://www.newsweekjapan.jp/stories/world/2019/09/post-12964_2.php");
            urls.Add("https://www.newsweekjapan.jp/stories/world/2019/09/post-12964_1.php");
            urls.Add("https://www.newsweekjapan.jp/magazine/245961.php");
            urls.Add("https://www.newsweekjapan.jp/satire_usa/2019/09/post-20.php");
            urls.Add("https://www.newsweekjapan.jp/stories/technology/2019/09/post-12935.php");
            urls.Add("https://www.newsweekjapan.jp/stories/english/");
            urls.Add("https://www.newsweekjapan.jp/stories/carrier/2018/03/post-9790.php");
            urls.Add("https://www.newsweekjapan.jp/stories/woman/2019/09/sns-3_2.php");


           // await DemoSimpleCrawler(urls[0]);


            foreach(string s in urls)
            {
                CrawledPage crawledPage = await DemoSinglePageRequest(s);
                Console.WriteLine(ScrapeTitle(crawledPage));
                Console.WriteLine(IsArticleFirstPage(crawledPage));
                Console.WriteLine("Category id is : " + Scraper.CalculateCategoryId(crawledPage, Program.Categories, Program.Blogs));
                Console.WriteLine("Blog id is : " + Scraper.CalculateBlogId(crawledPage, Program.Blogs));

                

            }


          
        }

        private static async Task<CrawledPage> DemoSinglePageRequest(string uri)
        {
            var pageRequester = new PageRequester(new CrawlConfiguration(), new WebContentExtractor());
            var crawledPage = await pageRequester.MakeRequestAsync(new Uri(uri));
            return crawledPage;
        }





        private static async Task DemoSimpleCrawler(string uri)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 50,
                MinCrawlDelayPerDomainMilliSeconds = 1000,
                CrawlTimeoutSeconds = 500,
                MaxConcurrentThreads = 2,
                MaxCrawlDepth = 1000,
          
            };
            var crawler = new PoliteWebCrawler(config);
            crawler.PageCrawlCompleted += crawler_ProcessPageCrawlCompleted;
            var crawlResult = await crawler.CrawlAsync(new Uri(uri));
        }




        private static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;        
            if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            }
            else
            {
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);
                Console.WriteLine(ScrapeTitle(crawledPage));
                Console.WriteLine(IsArticleFirstPage(crawledPage));
                Console.WriteLine(crawledPage.ParentUri);
                Console.WriteLine(Program.PAGEURL + crawledPage.ParentUri);

            }
        }

    }
}