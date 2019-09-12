using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbotTest
{
    class Presentation
    {

        public void DrawMenu()
        {
            Console.WriteLine("News Siteのスクレイピングを行うプログラムです。Abotクローラが全てのURLを走査し、記事をダウンロードします。" +
                "一度ダウンロードした記事を再度ダウンロードすることはありません。");
            Console.WriteLine("MENU");
            Console.WriteLine("Press \"s\" カテゴリを選択し記事一覧を取得");
            Console.WriteLine("Press \"b\" コラム(ブログ)タイトル一覧を初期化");
            Console.WriteLine("Press \"c\" カテゴリ一覧を初期化");

            string input = Console.ReadLine();
            switch (input)
            {
                case "s":
                    SelectCategory();
                    break;
                case "b":
                    InitBlogTable();
                    break;
                case "c":
//                    InitCategoryTable(AbotTest.Program.Blogs);
                    break;
                default:
                    break;
                
            }


        }

        private void SelectCategory()
        {
            throw new NotImplementedException();
        }

        public void InitCategoryTable(List<Category> categories)
        {
            Console.WriteLine("カテゴリのテーブルを更新しますか(一度削除します) 了承する場合は y を入力");
            var confirmation = Console.ReadLine();
            if (confirmation.Equals("y"))
            {
                DaoCategory.InitCategoryDB(categories);
                Console.WriteLine("カテゴリテーブルが更新されました");
            }
        }

        public void InitBlogTable()
        {
            ///Initialize blog table
        }

        public void BeginCrawling()
        {

        }



    }
}
