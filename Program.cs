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
using AngleSharp.XPath;
using static WebScraper.UtilityForNewsWeek;
using System.Configuration;
using static WebScraper.Utility;

namespace WebScraper
{


    public class Program
    {
        public const string PAGEURL = "https://www.newsweekjapan.jp";

        public static string LastParentUrl { get; set; } = PAGEURL;

        public static List<Article> Articles = new List<Article>();
        public static List<Author> Authors;
        public static List<Blog> Blogs;
        public static List<Category> Categories;    
        static List<string> CategoryPaths = new List<string>();

        //既知のURLを格納するリスト。Program起動時にDBのurlテーブルをそのままこれに格納
        static List<string> KnownUrls = new List<string>();
   

        //新しいURLを指定した回数取得する度にデータベースへ接続し、Articles リストをDBに格納
        //毎回DBにに接続すると負荷が高いので。
        //private static int NewUrlsCount = 0;
        //private static readonly int MaxUrlsCountAddOnce = 5;


        static async Task Main(string[] args)
        {
         
            //Log.Logger = new LoggerConfiguration()
            //    .MinimumLevel.Information()
            //    .WriteTo.Console()
            //    .CreateLogger();

            InitProgram();
            string uri = Presentation.DecideStartUrl();
            await DemoSimpleCrawler(uri);


            RefreshUrlTable(ref KnownUrls);
            //await Test.TestCode();

        }

        private static void InitProgram()
        {
            //既知のURLをデータベースからロード
            DaoUrl.GetAllKnownUrls(ref KnownUrls);
            Debug.WriteLine("Known urls are loaded from the database");

            //既知のAuthorをデータベースからロード. No name がなければテーブルに追加           
            Authors = DaoAuthor.GetAllAuthors();
            if (Authors == null) Authors.Add(new Author() { AuthorName = "" });
            var q = from x in Authors
                    where x.AuthorName.Equals("No Name")
                    select x;
            if(q.Count() == 0)
            {
                DaoAuthor.InsertAuthor(new Author() { AuthorName = "No Name" });
            }

            Blogs = DaoBlog.GetAllBlogs();
            if (Blogs.Count == 0)
            {
                Debug.WriteLine("Blog table is empty");
                Blogs = Scraper.ScrapeBlogs("https://www.newsweekjapan.jp/column/");
                DaoBlog.InitBlogDB(Blogs);
                Debug.WriteLine("Blog table was initialized");
            }
            else
            {
                Debug.WriteLine("Blog table is already filled");
            }

            Categories = DaoCategory.GetAllCategory();
            if(Categories.Count == 0)
            {
                Debug.WriteLine("category table is empty");
                DaoCategory.InitCategoryDB(CreateCategoryList());
                Debug.WriteLine("Category table was initialized");
                Categories = CreateCategoryList();
            }
            else
            {
                Debug.WriteLine("Category table is already filled");
                
            }
        }

        private static async Task DemoSimpleCrawler(string uri)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 200,          
                MinCrawlDelayPerDomainMilliSeconds = 1000,
                CrawlTimeoutSeconds = 500,
                MaxConcurrentThreads = 2,
                MaxCrawlDepth = 1000,
            //crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue1", "1111"),
            //crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue2", "2222"),

            };
            var crawler = new PoliteWebCrawler(config);
       


           
            crawler.PageCrawlCompleted += crawler_ProcessPageCrawlCompleted;
          
            var crawlResult = await crawler.CrawlAsync(new Uri(uri));
            Log.Logger.Information("Elapsed time total : " + crawlResult.Elapsed.TotalHours.ToString());
                        
        }
        
        
        /// <summary>
        /// 読み込んだページの処理はここで完結させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            

            CrawledPage crawledPage = e.CrawledPage;
            bool IsNewUrl = false;

            //一層下に潜るごとにKnownUrlsをDBに書き込み、ArticlesをDBに書き込む            
            if (LastParentUrl != crawledPage.ParentUri.ToString())
            {
                LastParentUrl = crawledPage.ParentUri.ToString();
                DaoUrlLastVisited.TruncateUrlLastVisited();
                DaoUrlLastVisited.InsertUrl(LastParentUrl);

                RefreshUrlTable(ref KnownUrls); //KnowUrlsは差分ではなく全体を持っているので、初期化する必要はない

                
                //Listの途中まで追加したところで失敗になることがある（要改良）
                //一旦コメントアウト、一つづつ追加することにする
                //if (DaoArticle.InsertArticles(Articles))
                //{
                //    Articles = new List<Article>();
                //    Console.WriteLine("DBへArticle listを追加しました");
                //}
                //else
                //{
                //    Console.WriteLine("DBへのArticle listの追加に失敗しました");
                //}
            }

            

            if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);

            }
            else
            {
                Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);
                var absolutePath = e.CrawledPage.Uri.AbsolutePath.ToString();
                var doc = e.CrawledPage.AngleSharpHtmlDocument;

                //既出のURLである場合は処理をスキップ
                if (KnownUrls.Contains(absolutePath))
                {
                    Log.Logger.Information($"already known url　:　{PAGEURL}{absolutePath}");
                }
                else    IsNewUrl = true;
                

                //未知のURLであればスクレイピングにはいる
                if(IsNewUrl)
                {
                    //KnownUrls.Add(absolutePath);
                    DaoUrl.InsertUrl(crawledPage.Uri.ToString());

                    //判定が甘い可能性がある
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
                        //リストに一時保存する方式
                        //Articles.Add(Scraper.ScrapeArticle(crawledPage, Program.Categories, Program.Blogs, Program.Authors));

                        //直接Insertする方式
                        Article article = Scraper.ScrapeArticle(crawledPage, Program.Categories, Program.Blogs, Program.Authors);
                        if (DaoArticle.InsertArticle(article)) Console.WriteLine("an article added to db"); ;
                    }
                }
            }
    
            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine("Page had no content {0}", crawledPage.Uri.AbsoluteUri);

                     
            //var htmlAgilityPackDocument = crawledPage.AngleSharpHtmlDocument; //Html Agility Pack parser
            //var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
            
        }
                            
    }



   


    

    /// <summary>
    /// 各種ツールとなるMethodをまとめたもの
    /// </summary>
    public static class UtilityForNewsWeek
    {
       

    }


}


