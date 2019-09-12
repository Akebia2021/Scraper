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
using static AbotTest.UtilityForNewsWeek;


namespace AbotTest
{


    public class Program
    {
        public const string PAGEURL = "https://www.newsweekjapan.jp";

        public static List<Article> Articles = new List<Article>();
        static List<Author> Authors = new List<Author>();
        public static List<Blog> Blogs = new List<Blog>();
        public static List<Category> Categories;
        static List<string> KnownUrls = new List<string>();
        static List<string> CategoryPaths = new List<string>();




        static async Task Main(string[] args)
        {
            Console.WriteLine("specify root URI...");
            string uri = Console.ReadLine();
            
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateLogger();

            Log.Logger.Information("Demo starting up!");

            InitProgram();

          

           Article article =  Scraper.ScrapeContents("/stories/world/2019/09/post-12983.php");
            // foreach (Article article in Articles) Console.WriteLine(article.Contents);

            //await CrawlerForNewsWeek(uri);
            //await DemoSinglePageRequest();

            Console.ReadLine();

        }

        private static void InitProgram()
        {
            //既知のURLをデータベースからロード
            DaoUrl.GetAllKnownUrls(ref KnownUrls);
            Debug.WriteLine("Known urls are loaded from the database");

            var blogs = DaoBlog.GetAllBlogs();
            if (blogs.Count == 0)
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

        private static async Task CrawlerForNewsWeek(string uri)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 10,          
                MinCrawlDelayPerDomainMilliSeconds = 1000,
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
        
        
        /// <summary>
        /// 読み込んだページの処理はここで完結させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                                
                var doc = e.CrawledPage.AngleSharpHtmlDocument;
                

                if (KnownUrls.Contains(absolutePath))
                {
                    Debug.WriteLine($"It is already known url:{absolutePath}");
                }
                else
                {
                    KnownUrls.Add(absolutePath);

                    //本文の取得を始めるには、一番最初のページであり、カテゴリもしくは（ブログ）に所属し、.phpファイルである必要がある。
                    if (CheckIfArticleAndFirstPage(e.CrawledPage, doc)
                        && WithinCategory(CategoryPaths, absolutePath)
                        || WithinBlog(Blogs, absolutePath))
                    {
                        //Article article = Scraper.ScrapeContents(e.CrawledPage.Uri.ToString());

                        
                    }
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
        
        public static string ExtractSecondLevelString(string url, string rootUrl)
        {
            string category;
            //Root直下の
            Regex reg = new Regex(@"(?<=https://www.newsweekjapan.jp/)[a-z]+");
            category = reg.Match(url).ToString();
            //URLに従ってファーストカテゴリを決める
            return url;
        }

        
        /// <summary>
        /// .phpファイルでありなおかつ li class='prev' を持たないことで、記事の Pagination 1ページ目であることを確認する。
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static bool CheckIfArticleAndFirstPage(CrawledPage crawledPage, AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            
            if (crawledPage.Uri.AbsolutePath.EndsWith(".php") 
                && doc.Body.SelectSingleNode("//div[@class = 'contentPanes']//li[@class = 'prev']") == null)
            {
                
                return true;
            }
            else return false;

        }

        public static bool WithinCategory(List<string> categories, string path)
        {
            if (categories.Contains(path))
            {
                return true;
            }
            return false;
        }

        public static bool WithinBlog(List<Blog> blogs, string path)
        {
            foreach(Blog blog in blogs)
            {
                if (path.StartsWith(blog.Relative_Path))
                {
                    return false;
                   
                }
            }
            return true;
        }


     
        public static string GetNextURLofPage(string page)
        {
            HtmlWeb web = new HtmlWeb();           
            var doc = web.Load(page);
            var nextUrlNode = doc.DocumentNode.SelectSingleNode("//li[@class = 'next']");
                        
            if (nextUrlNode == null)
            {
                return null;
                
            }                       
            else if (nextUrlNode.FirstChild.NextSibling.Name == "a")
            {
                string formerUrl = page;
                page = nextUrlNode.FirstChild.NextSibling.GetAttributeValue("href", null);
                //次ページへのリンクが https で始まっていない場合は末尾を書き換える必要がある。
                if (!page.StartsWith("http"))
                {
                    Regex reg = new Regex(@"_\d{1,2}.php");
                    Match match = reg.Match(page);
                    reg = new Regex(@"(_\d{1,2}.php)|.php");
                    formerUrl = reg.Replace(formerUrl, "");
                    page = formerUrl + match.Value;
                    return page;

                }
                else
                {
                    return page;
                }
                
            }
            else return null;
                       

            
        }

    

        public static List<Category> CreateCategoryList()
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







    }







}


