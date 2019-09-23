using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static WebScraper.Program;

namespace WebScraper
{
    static class  Presentation
    {

        public static string DecideStartUrl()
        {
            Console.WriteLine("News Siteのスクレイピングを行うプログラムです。Abotクローラが全てのURLを走査し、記事をダウンロードします。" +
                "一度ダウンロードした記事を再度ダウンロードすることはありません。");
            if (LastParentUrl == null)
            {
                LastParentUrl = PAGEURL;
            }
            string uri;


            Console.WriteLine("直近のセッションの最後のURLのからクローリングを続ける : press Y \n " +
                "Root URLからクローリングを開始する ： press any key \n"  );
            if (Console.ReadLine() == "Y")
            {
                uri = Program.LastParentUrl;
            }           
            else uri = PAGEURL;

            Console.WriteLine($"{uri} からクローリングを開始します");

            return uri;
        }

        static private void SelectCategory()
        {
            throw new NotImplementedException();
        }

        public static void InitCategoryTable(List<Category> categories)
        {
            Console.WriteLine("カテゴリのテーブルを更新しますか(一度削除します) 了承する場合は y を入力");
            var confirmation = Console.ReadLine();
            if (confirmation.Equals("y"))
            {
                DaoCategory.InitCategoryDB(categories);
                Console.WriteLine("カテゴリテーブルが更新されました");
            }
        }

        public static void InitBlogTable()
        {
            ///Initialize blog table
        }

        public static void BeginCrawling()
        {

        }



    }
}
