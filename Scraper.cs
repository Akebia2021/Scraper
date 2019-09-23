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
using static AbotTest.UtilityForNewsWeek;

namespace AbotTest
{
     public static class Scraper
    {


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
        public static Article ScrapeArticle(string absolutePath)
        {
            string url = AbotTest.Program.PAGEURL + absolutePath;
            Article article = new Article();


            HtmlWeb web = new HtmlWeb();
            var node = web.Load(url).DocumentNode;
            
            //extract title
            article.Title = node.SelectSingleNode("/html/head/meta[@property = 'og:title'][@content]")
                .GetAttributeValue("content", "no title");
            Console.WriteLine(article.Title + " ：：： 記事のタイトルを表示しています(ScrapeArticle Method)");

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
            string dateSource = node.SelectSingleNode("//div[@class = 'entryDetailData']/div[@class = 'date']")
                .InnerText;
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
            DeterminCategoryIdOrBlogId(absolutePath, ref AbotTest.Program.Categories, ref AbotTest.Program.Blogs,  ref article);

            //extract contents
            string result = "";
            while (url != null)
            {

                web = new HtmlWeb();
                var doc = web.Load(url);
                node = doc.DocumentNode.SelectSingleNode("//div[@class = 'entryDetailBodyBlock']");



                var nodes = node.SelectNodes("p | h4");
                foreach (HtmlNode p in nodes)
                {
                    result += p.InnerText;
                    result += "\n";
                }
                url = GetNextPageUrl(url);


            }
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

      


        //記事のカテゴリidを決定する（refで渡す）。カテゴリがコラムの場合はblogIdも決定
        public static void DeterminCategoryIdOrBlogId(string absolutePath, ref List<Category> categories, ref List<Blog> blogs, ref Article article)
        {
            Regex reg = new Regex(@"(?!.*/(2))(.+.php)");
            var categoryPath = reg.Replace(absolutePath, "");
            var categoryIndex = categories.FindIndex(n => n.Relative_Path.Equals(categoryPath));
            if(categoryIndex >= 0)
            {
                article.CategoryId = categories[categoryIndex].CategoryId;
            }
            else
            {
                
                var blogIndex = blogs.FindIndex(n => n.Relative_Path.Equals(categoryPath));
                Debug.WriteLine($"absolute path is : {categoryPath}");
                Debug.WriteLine($"blogIndex is : {blogIndex}");
                if (blogIndex >= 0)
                {
                    //カテゴリテーブル内の”コラム”の行のIDを取得するため
                    var indexForColumn = categories.FindIndex(n => n.Relative_Path.Equals("/column/"));
                    article.CategoryId = categories[indexForColumn].CategoryId;

                    article.BlogId = blogs[blogIndex].BlogId;
                }
                else
                {
                    Console.WriteLine($" {categoryPath} はブログにもカテゴリにも属していないようです！");
                }
            }  
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
