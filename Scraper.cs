using Abot2.Poco;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static Article ScrapeArticle(string absolutePath)
        {
            string url = AbotTest.Program.PAGEURL + absolutePath;
            Article article = new Article();


            HtmlWeb web = new HtmlWeb();
            var node = web.Load(url).DocumentNode;
            
            //extract title
            article.Title = node.SelectSingleNode("/html/head/meta[@property = 'og:title'][@content]")
                .GetAttributeValue("content", "no title");
            Console.WriteLine(article.Title);

            //extract author
            //var authorName = ScrapeAuthorName(node);
            //if(IsNewAuthor(AbotTest.Program.Authors, authorName))

            //extract publish date
            article.PublishDate = FormatToDateTime(node.SelectSingleNode("html/head/meta[@property= 'article:published_time']")
                .GetAttributeValue("content", "no datetime"));

            //extract modified date
            article.ModifiedDate = FormatToDateTime(node.SelectSingleNode("html/head/meta[@property= 'article:modified_time']")
                .GetAttributeValue("content", "no datetime"));

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
            string name;
            var node = doc.QuerySelector("meta[property='cXenseParse:author']");
            if (node == null) { name = null; }
            else { name = node.GetAttribute("content"); }
            
            if(name == null)
            {
                Console.WriteLine("The extracted author name is null!!");
                return null;
            }
            return name;
            
        }

      


        //カテゴリになければコラム(ブログ)
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
                Debug.WriteLine($"category path is : {categoryPath}");
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
                    Console.WriteLine($"the category path {categoryPath} is unknown path!! Cannot determine categoryId!");
                }
            }  
        }


        /// <summary>
        /// 2019-09-12T18:30:00+09:00 のフォーマットをDateTimeに変換
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private static DateTime FormatToDateTime(string source)
        {
            Regex reg = new Regex(@"\+.*");
            string strTime = reg.Replace(source, "");
 
            string format = "yyyy-MM-ddTHH:mm:ss";

            return DateTime.ParseExact(strTime, format, null);  //自分でFormatを指定
        }

    }
}
