using Abot2.Poco;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebScraper;
using static WebScraper.UtilityForNewsWeek;

namespace WebScraper
{
     public static class Scraper
    {
        public static void ScrapePage(Predicate<CrawledPage> predicate)
        {
            
        }


        //Blogテーブルを初期化するのに必要なメソッド（URL構造が特殊なため）
        public static List<Blog> ScrapeBlogs(string url)
        {
            List<Blog> blogs = new List<Blog>();

            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(url);
            var blogsNodes = doc.DocumentNode.SelectNodes("/[@class = 'author short']");

            foreach (HtmlNode node in blogsNodes)
            {
                Blog blog = new Blog();

                blog.BlogName = node.SelectSingleNode(".//div[@class='entryAuthor']").FirstChild.InnerText + " / "
                    + node.SelectSingleNode(".//div[@class='entryAuthor']").FirstChild.FirstChild.InnerText;
                blog.Relative_Path = node.FirstChild.FirstChild.GetAttributeValue("href", "");

                blogs.Add(blog);

            }
            return blogs;

        }

        /// <summary>
        /// 記事のurl (.php)から記事、著者名などを取得。
        /// 一番最初のページであることを確認し、最後のページまでたどっていく。
        /// </summary>
        /// <param name="absolutePath"></param>
        /// <returns></returns>
        public static Article ScrapeArticle(CrawledPage crawled)
        {
           
            Article article = new Article();

            //extract title
            article.Title = ScrapeTitle(crawled);

            ////extract author and set AuthorId(nullable)
            //string authorName = Scraper.ScrapeAuthorName(node);
            //if (authorName == null) article.AuthorId = null;
            //else
            //{
            //    var authorIndex = GetAuthorIndex(ref Program.Authors, authorName);
            //    if (authorIndex > 0) article.AuthorId = Program.Authors[authorIndex].AuthorId;
            //    Debug.WriteLine("作者がAuthor Listにはありません");
            //}
            //var authorName = ScrapeAuthorName(node);
            //if(IsNewAuthor(AbotTest.Program.Authors, authorName))


            //extract article's date
            string dateSource = crawled.AngleSharpHtmlDocument.Body.SelectSingleNode("//div[@class = 'entryDetailData']/div[@class = 'date']")
                ?.TextContent;
            DateTime publishedTime;
            if (FormatToDateTime(dateSource, out publishedTime)){ }
            else
            {
                Console.WriteLine("ScrapeArticle Method was turminated");
                return null;
            }
            article.PublishDate = publishedTime;           
                    //publishとmodifiedの情報を持たない記事があるので
                    //extract publish date
                    //article.PublishDate = FormatToDateTime(node.SelectSingleNode("html/head/meta[@property= 'article:published_time']")
                    //    .GetAttributeValue("content", "no datetime"));

                    ////extract modified date
                    //article.ModifiedDate = FormatToDateTime(node.SelectSingleNode("html/head/meta[@property= 'article:modified_time']")
                    //    .GetAttributeValue("content", "no datetime"));

            //determine category or blog
            DeterminCategoryIdOrBlogId("", ref WebScraper.Program.Categories, ref WebScraper.Program.Blogs,  ref article);

           
            //extract contents
            string result = "";
            //while (url != null)
            //{

            //    web = new HtmlWeb();
            //    var doc = web.Load(url);
            //    node = doc.DocumentNode.SelectSingleNode("//div[@class = 'entryDetailBodyBlock']");



            //    var nodes = node.SelectNodes("p | h4");
            //    foreach (HtmlNode p in nodes)
            //    {
            //        result += p.InnerText;
            //        result += "\n";
            //    }
            //    url = Utility.GetNextPageUrl(url);


            //}
            article.Contents = result;
                                          
            return article;
            
        }

        public static string ScrapeAuthorName(CrawledPage crawledPage)
        {
            var doc = crawledPage.AngleSharpHtmlDocument;
            var node = doc.QuerySelector("div.author");
            if (node == null) return null;
            else return node.TextContent;
            
        }
        public static string ScrapeAuthorName(HtmlNode node)
        {
            node = node.SelectSingleNode("//div[@class = 'author']");
            if (node == null)
            {
                return null;
            }
            else return node.InnerText;
        }

        /// <summary>
        /// 要素がなければNullを返す
        /// </summary>     
        public static string ScrapeTitle(CrawledPage crawled)
        {

            var node = crawled.AngleSharpHtmlDocument.Body.SelectSingleNode("//*[@id='content']/div[3]/div[2]/div[1]/div[1]/h3");
            if (node != null) return node.TextContent;

            return crawled.AngleSharpHtmlDocument.QuerySelector("div.entryDetailHeadline.border_btm.clearfix > h3")?.TextContent;
        }

      

        /// <summary>
        /// IDが見つからない場合はDebugメッセージを出し Nullを返す
        /// </summary>        
        public static int? CalculateCategoryId(CrawledPage crawled, List<Category> categories, List<Blog> blogs)
        {
            var absPath = crawled.Uri.AbsolutePath;
            
            Regex reg = new Regex(@"(?!.*/(2))(.+.php)");
            var categoryPath = reg.Replace(crawled.Uri.AbsolutePath, "");
            var categoryIndex = categories.FindIndex(x => x.Relative_Path.Equals(categoryPath));

            //カテゴリに属している場合
            if (categoryIndex >= 0)
            {
                return categories[categoryIndex].CategoryId;
            }
            //カテゴリが無いもしくはコラムに属する場合
            else
            {
                var q = from b in blogs
                        where b.Relative_Path.Equals(categoryPath)
                        select b;
                //クエリ結果の有無は Count() で確かめる
                if (q.Count() == 1)
                {
                    var i = from c in categories
                            where c.Relative_Path.Equals("/column/")
                            select c.CategoryId;
                    return i.First<int>();
                }
                else
                {
                    Debug.WriteLine("Couldn't find category ID!!!");
                    return null;
                }               
            }          
        }

        /// <summary>
        /// IDが見つからなければNullを返す
        /// </summary>       
        public static int? CalculateBlogId(CrawledPage crawled, List<Blog> blogs)
        {
            var absPath = crawled.Uri.AbsolutePath;
            Regex reg = new Regex(@"(?!.*/(2))(.+.php)");
            var blogPath = reg.Replace(absPath, "");
            var q = from b in blogs
                    where b.Relative_Path.Equals(blogPath)
                    select b;
            if (q.Count() == 1) return q.First().BlogId;
            else return null;
        }
                      


        /// <summary>
        /// 2019年9月17日（火）19時15分 のフォーマットをDateTimeに変換
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>       
        private static bool FormatToDateTime(string source, out DateTime date)
        {
            CultureInfo jaJP = new CultureInfo("ja-JP");
            Regex reg = new Regex("（.）");
            source = reg.Replace(source, "");          

            string format = "yyyy年M月dd日HH時mm分";
            
            if (DateTime.TryParseExact(source, format, jaJP, DateTimeStyles.None, out date))
            {
                Debug.WriteLine("DateTime parsing was suceeded.");
                return true;
            }
            else
            {
                Debug.WriteLine("DateTime parsing was failed.");
                return false;
              
            }
        
        }

    }
}
