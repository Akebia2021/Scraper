using Abot2.Poco;
using WebScraper;
using AngleSharp.XPath;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebScraper
{
    public static class Utility
    {


        //このチェックはちょっと甘いかも
        public static bool IsArticleFirstPage(CrawledPage crawledPage)
        {
            string title = Scraper.ScrapeTitle(crawledPage);
            if (IsFirstPage(crawledPage) && title != null)
            {
                return true;
            }
            else return false;
        }
        
        public static bool IsPHP(CrawledPage crawled)
        {
            if (crawled.Uri.AbsolutePath.EndsWith(".php"))
            {
                Debug.WriteLine("page is .php");
                return true;
            }

            else return false;
        }
        public static bool IsFirstPage(CrawledPage crawled)
        {
            if (crawled.AngleSharpHtmlDocument.Body.SelectSingleNode("//li[@class = 'prev']") == null)
            {
                Debug.WriteLine("page doesn't contain class='prev' ");
                return true;
            }

            else return false;
            
        }

               
        /// <summary>
        /// .phpファイルでありなおかつ li class='prev' を持たないことで、記事の Pagination 1ページ目であることを確認する。
        /// </summary>
        /// <param name="crawledPage"></param>
        /// <param name="doc"></param>
        /// <returns></returns>
        /// 
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
            foreach (Blog blog in blogs)
            {
                if (path.StartsWith(blog.Relative_Path))
                {
                    return false;

                }
            }
            return true;
        }

        public static bool IsNewAuthor(ref List<Author> authors, string name)
        {

            var authorIndex = authors.FindIndex(n => n.AuthorName.Equals(name));
            if (authorIndex > 0)
            {
                return true;
            }
            else return false;
        }

        public static int GetAuthorIndex(ref List<Author> authors, string name)
        {
            return authors.FindIndex(n => n.AuthorName.Equals(name));
        }



        public static string GetNextPageUrl(string page)
        {
            HtmlWeb web = new HtmlWeb();
            var doc = web.Load(page);
            var nextUrlNode = doc.DocumentNode.SelectSingleNode("//*[@id='content']//li[@class='next']/a");

            if (nextUrlNode != null)
            {
                string formerUrl = page;
                page = nextUrlNode.GetAttributeValue("href", null);

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



        /// <summary>
        /// urlテーブルを一度削除して、新たに更新する
        /// </summary>
        /// <param name="urls"></param>
        public static void RefreshUrlTable(ref List<string> urls)
        {
            DaoUrl.TruncateUrlTable();

            var noDupes = urls.Distinct().ToList();

            if (DaoUrl.InsertUrls(noDupes)) Console.WriteLine("新たにURLをDBに追加しました。");
            else Console.WriteLine("DBへのURLの追加に失敗しました。");
        }

    }
}
