using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AbotTest
{
  

    public class Program
    {

        static List<Article> Articles = new List<Article>();
        static List<Author> Authors = new List<Author>();
        static List<Blog> Blogs = new List<Blog>();
        static List<string> KnownUrls = new List<string>();
        static List<string> FreshUrls = new List<string>();
        static List<string> Categories = new List<string>();
        


        static async Task Main(string[] args)
        {
            Console.WriteLine("specify root URI...");
            string uri = Console.ReadLine();
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Demo starting up!");

            DaoUrl.GetAllKnownUrls(ref KnownUrls);
            Log.Logger.Information("known urls was loaded from the database");

            var blogs =  UtilityForNewsWeek.ScrapeBlogs("https://www.newsweekjapan.jp/column/");
            foreach (Blog blog in blogs) Console.WriteLine(blog.BlogName);


            //データベースへのアクセスロジックが漏れてしまうのでこれはだめ
            //AccessDB();
            

           // await CrawlerForNewsWeek(uri);
            //await DemoSinglePageRequest();

           

        }

        

        private static async Task CrawlerForNewsWeek(string uri)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 100, //Only crawl 10 pages
                MinCrawlDelayPerDomainMilliSeconds = 1000, //Wait this many millisecs between requests
                CrawlTimeoutSeconds = 500,
                MaxConcurrentThreads = 10,
                MaxCrawlDepth = 1,
            //crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue1", "1111"),
            //crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue2", "2222"),

            };
            var crawler = new PoliteWebCrawler();
       


            crawler.PageCrawlStarting += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompleted += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowed += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowed += crawler_PageLinksCrawlDisallowed;

            var crawlResult = await crawler.CrawlAsync(new Uri(uri));

            

        }
        
      
        private static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;

            if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);
            ////////////////////////////////////////////////////////////////////////////////
            //////////////////////Article に追加//////////////////////
            else
            {
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);
                var absolutePath = e.CrawledPage.Uri.AbsolutePath.ToString();

                //AngleSharp
                var doc = e.CrawledPage.AngleSharpHtmlDocument;
                var node = doc.Body.

                if (KnownUrls.Contains(absolutePath))
                {
                    Debug.WriteLine($"Known url:{absolutePath}");
                }
                else
                {
                    KnownUrls.Add(absolutePath);
                }

            }
            //////////////////////////////////////////////////////////////////////////////

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);

           
            var htmlAgilityPackDocument = crawledPage.AngleSharpHtmlDocument; //Html Agility Pack parser
            var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
            
        }



        private static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("About to crawl link {0} which was found on page {1}", pageToCrawl.Uri.AbsoluteUri, pageToCrawl.ParentUri.AbsoluteUri);
        }

        private static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine("Did not crawl the links on page {0} due to {1}", crawledPage.Uri.AbsoluteUri, e.DisallowedReason);
        }

        private static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine("Did not crawl page {0} due to {1}", pageToCrawl.Uri.AbsoluteUri, e.DisallowedReason);
        }

    }



   


    

    /// <summary>
    /// 各種ツールとなるMethodをまとめたもの
    /// </summary>
    public static class UtilityForNewsWeek
    {
        
        public static string ExtranctSecondLevelString(string url, string rootUrl)
        {
            string category;
            //Root直下の
            Regex reg = new Regex(@"(?<=https://www.newsweekjapan.jp/)[a-z]+");
            category = reg.Match(url).ToString();
            //URLに従ってファーストカテゴリを決める
            return url;
        }

        //Because the article table only needs .php link
        public static bool CheckIfArticleFirstPage(CrawledPage crawledPage, AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            if (crawledPage.Uri.AbsolutePath.EndsWith(".php") 
                && doc.Body.
            {
                
                return true;
            }
            else return false;

        }

        public static bool HasCategory(List<string> categories, string path)
        {
            if (categories.Contains(path))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 一つの記事について次のページを取得
        /// 最後のページ(次がない)ならNullを返す
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        public static string GetNextURL(string page)
        {
            HtmlWeb web = new HtmlWeb();           
            var doc = web.Load(page);
            var nextUrlNode = doc.DocumentNode.SelectSingleNode("//li[@class = 'next']");
                        
            if (nextUrlNode == null)
            {
                page = null;
            }
                       
            else if (nextUrlNode.FirstChild.NextSibling.Name == "a")
            {
                string formerUrl = page;
                page = nextUrlNode.FirstChild.NextSibling.GetAttributeValue("href", null);
                //httpで始まっていない場合は
                if (!page.StartsWith("http"))
                {
                    Regex reg = new Regex(@"_\d{1,2}.php");
                    Match match = reg.Match(page);
                    reg = new Regex(@"_\d{1,2}.php");
                    formerUrl = reg.Replace(formerUrl, "");
                    page = formerUrl + match.Value;

                }
            }
            else page = null;

            Debug.WriteLine(page);

            return page;
        }

        public static bool CheckIfFirstPage(HtmlDocument doc)
        {
            if (doc.DocumentNode.SelectSingleNode("//li[@class = 'prev']") != null)
            {
                return true;
            }
            return false;
        }


        public static List<Category> InitCategoryList()
        {
            List<Category> categories = new List<Category>();

            #region
            categories.Add(new Category() { Name = "ワールド", Relative_Path = "/stories/world/" });
            categories.Add(new Category() { Name = "ビジネス", Relative_Path = "/stories/business/" });
            categories.Add(new Category() { Name = "テクノロジー", Relative_Path = "/stories/technology/" });
            categories.Add(new Category() { Name = "カルチャー", Relative_Path = "/stories/culture/" });
            categories.Add(new Category() { Name = "ライフスタイル", Relative_Path = "/stories/lifestyle/" });
            categories.Add(new Category() { Name = "キャリア", Relative_Path = "/stories/carrier/" });
            categories.Add(new Category() { Name = "for Woman", Relative_Path = "/stories/woman/" });
            categories.Add(new Category() { Name = "コラム(ブログ)", Relative_Path = "/column/" });
            categories.Add(new Category() { Name = "ニュース速報", Relative_Path = "/headlines/" });
            categories.Add(new Category() { Name = "ニュース速報(ワールド)", Relative_Path = "/headlines/world/" });
            categories.Add(new Category() { Name = "ニュース速報(ビジネス)", Relative_Path = "/headlines/business/" });

            #endregion

            return categories;

        }


        public static List<Blog> ScrapeBlogs(string url)
        {
            List<Blog> blogs = new List<Blog>();

            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(url);
            var blogsNodes = doc.DocumentNode.SelectNodes("//*[@class = 'author short']");

            foreach(HtmlNode node in blogsNodes)
            {
                Blog blog = new Blog();

                blog.BlogName = node.SelectSingleNode(".//div[@class='entryAuthor']").FirstChild.InnerText + " / "
                    + node.SelectSingleNode(".//div[@class='entryAuthor']").FirstChild.FirstChild.InnerText;
                blog.Relative_Path = node.FirstChild.FirstChild.GetAttributeValue("href", "");
                             
                blogs.Add(blog);
               
            }
            return blogs;
            
        }




    }







}


