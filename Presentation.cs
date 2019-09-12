using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbotTest
{
    class Presentation
    {

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

    }
}
