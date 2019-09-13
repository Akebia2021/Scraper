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
        public static List<Author> Authors;
        public static List<Blog> Blogs = new List<Blog>();
        public static List<Category> Categories;
        static List<string> KnownUrls = new List<string>();
        static List<string> CategoryPaths = new List<string>();

        private static int urlAddedCount = 0;
        private static readonly int urlAddedCountMax = 25;


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
         
            
             await DemoSimpleCrawler(uri);


            DaoUrl.TruncateUrlTable();
            DaoUrl.InsertUrls(KnownUrls);

        }

        private static void InitProgram()
        {
            //既知のURLをデータベースからロード
            DaoUrl.GetAllKnownUrls(ref KnownUrls);
            Debug.WriteLine("Known urls are loaded from the database");

            //既知のAuthorをデータベースからロード
            Authors = DaoAuthor.GetAllAuthors();

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

        private static async Task DemoSimpleCrawler(string uri)
        {
            var config = new CrawlConfiguration
            {
                MaxPagesToCrawl = 100,          
                MinCrawlDelayPerDomainMilliSeconds = 1000,
                CrawlTimeoutSeconds = 500,
                MaxConcurrentThreads = 10,
                MaxCrawlDepth = 10,
            //crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue1", "1111"),
            //crawlConfig.ConfigurationExtensions.Add("SomeCustomConfigValue2", "2222"),

            };
            var crawler = new PoliteWebCrawler(config);
       


           
            crawler.PageCrawlCompleted += crawler_ProcessPageCrawlCompleted;
          
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

            //if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
            //{
            //    Console.WriteLine("Crawl of page failed {0}", crawledPage.Uri.AbsoluteUri);

            //}
            //else
            //{
            Console.WriteLine("Crawl of page succeeded {0}", crawledPage.Uri.AbsoluteUri);
            var absolutePath = e.CrawledPage.Uri.AbsolutePath.ToString();

            var doc = e.CrawledPage.AngleSharpHtmlDocument;

            //既出のURLである場合は処理をスキップ
            if (KnownUrls.Contains(absolutePath))
            {
                Log.Logger.Information($"It is already known url:{absolutePath}");
            }
            else
            {
                KnownUrls.Add(absolutePath);
                urlAddedCount++;



                //ArticleのScrapingを始める条件は、Pagenationの1ページ目であり、カテゴリかブログに所属していること。
                if (IfArticleAndIfFirstPage(e.CrawledPage, doc)
                    &&( CanCategorize(CategoryPaths, absolutePath)
                    || IsBlogEntry(Blogs, absolutePath)))
                {
                    //執筆者名から既出判定に従いデータベースを更新
                    string authorName = Scraper.ScrapeAuthorName(e.CrawledPage);
                    if (IsNewAuthor(Authors, authorName))
                    {
                        Author author = new Author() { AuthorName = authorName };
                        DaoAuthor.InsertAuthor(author);
                        Authors = DaoAuthor.GetAllAuthors();
                    }
                    
                    Article article = Scraper.ScrapeArticle(absolutePath);

                    var authorIndex = Authors.FindIndex(n => n.AuthorName.Equals(authorName));
                    article.AuthorId = Authors[authorIndex].AuthorId;                    
                }

                if(urlAddedCount >= urlAddedCountMax)
                {
                    DaoArticle.InsertArticles(Articles);
                    DaoUrl.InsertUrls(KnownUrls);
                    
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
        
        //public static string ExtractSecondLevelString(string url, string rootUrl)
        //{
        //    string category;
        //    //Root直下の
        //    Regex reg = new Regex(@"(?<=https://www.newsweekjapan.jp/)[a-z]+");
        //    category = reg.Match(url).ToString();
        //    //URLに従ってファーストカテゴリを決める
        //    return url;
        //}

        
        /// <summary>
        /// .phpファイルでありなおかつ li class='prev' を持たないことで、記事の Pagination 1ページ目であることを確認する。
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static bool IfArticleAndIfFirstPage(CrawledPage crawledPage, AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            
            if (crawledPage.Uri.AbsolutePath.EndsWith(".php") 
                && doc.Body.SelectSingleNode("//li[@class = 'prev']") == null)
            {
                
                return true;
            }
            else return false;

        }

        public static bool CanCategorize(List<string> categories, string path)
        {
            if (categories.Contains(path))
            {
                return true;
            }
            return false;
        }

        public static bool IsBlogEntry(List<Blog> blogs, string path)
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

        public static bool IsNewAuthor(List<Author> authors, string name)
        {
            foreach(Author author in authors)
            {
                if (author.AuthorName.Equals(name)) return false;
            }
            return true;
        }
                

     
        public static string GetNextPageUrl(string page)
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
                
                //次ページへのリンクが https で始まっていない場合は末尾の置換が必要
                if (!page.StartsWith("http"))
                {
                    Regex reg = new Regex(@"_\d{1,2}.php");
                    Match match = reg.Match(page);
                    reg = new Regex(@"(_\d{1,2}.php)|.php");
                    formerUrl = reg.Replace(formerUrl, "");

                    return formerUrl + match.Value;
                   
                }
                else
                {
                    return page;
                }
                
            }
            else return null;
                       

            
        }

        //Urlが50個になったら一度データベースに入れる
        public static void UpdateArticleDatabase(int count)
        {

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


