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
            urls.Add("https://www.newsweekjapan.jp/stories/world/2019/09/post-13033.php");
            urls.Add("https://www.newsweekjapan.jp/stories/world/2019/09/post-13026.php");
            urls.Add("https://www.newsweekjapan.jp/stories/woman/2019/09/sns-3.php");


            // await DemoSimpleCrawler(urls[0]);

            List<Article> articles = new List<Article>();

            foreach(string s in urls)
            {
                CrawledPage crawledPage = await DemoSinglePageRequest(s);

                if (IsArticleFirstPage(crawledPage))
                {
                    Console.WriteLine("記事のスクレイピングを初めます");
                    var authorName = Scraper.ScrapeAuthorName(crawledPage);
                    if (authorName == null) authorName = "No Name";
                    Console.WriteLine("Author Name is ： " + authorName);
                    if (IsNewAuthor(ref Program.Authors, authorName))
                    {
                        Program.Authors.Add(new Author() { AuthorName = authorName });
                        DaoAuthor.InsertAuthor(new Author() { AuthorName = authorName });
                    }
                    articles.Add(Scraper.ScrapeArticle(crawledPage, Program.Categories, Program.Blogs, Program.Authors));
                }
            }
            foreach (Article a in articles) Console.WriteLine($"title {a.Title},author id {a.AuthorId},  category id {a.CategoryId}, date {a.PublishDate}, url {a.Url}");

          


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