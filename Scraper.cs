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
        


        public static Article ScrapeArticle(CrawledPage crawled, List<Category> categories, List<Blog> blogs)
        {

            Article article = new Article();

            //extract title
            article.Title = ScrapeTitle(crawled);
            article.AuthorId = 232;
            //article.AuthorId = CaluculateAuthorId(crawled);
            article.CategoryId = CalculateCategoryId(crawled, categories, blogs);
            article.BlogId = CalculateBlogId(crawled, blogs);
            article.PublishDate = ScrapePublishDate(crawled);
            article.Contents = ScrapeContents(crawled);
            article.Url = crawled.Uri.ToString();


            return article;
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
               
        public static string ScrapeContents(CrawledPage crawled)
        {
            string url = crawled.Uri.ToString();
            HtmlWeb web;
            string result = null;
            while (url != null)
            {

                web = new HtmlWeb();
                var doc = web.Load(url);
                var node = doc.DocumentNode.SelectSingleNode("//div[@class = 'entryDetailBodyBlock']");
                //パースできない記事があったら　if を単に増やしていく
                if(node == null) node = doc.DocumentNode.SelectSingleNode("//div[@class = 'entryDetailBodyCopy']");
                if (node != null)
                {
                    var nodes = node.SelectNodes("p | h4");
                    foreach (HtmlNode p in nodes)
                    {
                        result += p.InnerText;
                        result += "\n";
                    }
                }
                url = Utility.GetNextPageUrl(url);
                
            }
            return result;
        }



        public static string ScrapeAuthorName(CrawledPage crawledPage)
        {
            var doc = crawledPage.AngleSharpHtmlDocument;
            var node = doc.QuerySelector("div.author");
            return node?.TextContent;
            
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
        /// パースに失敗したら DateTime.MaxValue を返す
        /// </summary>
        public static DateTime ScrapePublishDate(CrawledPage crawled)
        {
            DateTime date = DateTime.MaxValue;
            string dateSource = crawled.AngleSharpHtmlDocument.Body.SelectSingleNode("//div[@class = 'entryDetailData']/div[@class = 'date']")
                ?.TextContent.Trim();
            if(dateSource != null)
            {
                Regex reg = new Regex("（.）");
                var source = reg.Replace(dateSource, "");
                string format = "yyyy年M月d日HH時mm分";
                if (DateTime.TryParseExact(source, format, null, DateTimeStyles.None, out date)) { }
               
            }

            return date;
           

        }

        public static int CalcurateAuthorId(CrawledPage crawled, List<Author> authors)
        {
            string name = ScrapeAuthorName(crawled);

            return 1;
        }


    }
}
