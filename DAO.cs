using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbotTest
{
   
    public class DaoArticle
    {
        
        public static bool InsertUrl(string url)
        {


            string connStr = "server=localhost;user=root;database=news_data_pool;port=3306;password=3056jjjjj";

            MySqlConnection conn = new MySqlConnection(connStr);

            try
            {
                conn.Open();
                string sql = string.Format($"INSERT INTO article (title) VALUE ('{url}')");
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                conn.Close();
                return true;
            }
            catch (Exception e)
            {
                conn.Close();
                Log.Logger.Information(e.ToString());
                return false;
            }


        }

        public List<Article> GetAllArticles()
        {
            List<Article> articles = new List<Article>();
            //string sql = "select * from article";
            //MySqlCommand cmd = new MySqlCommand(sql, conn);
            //MySqlDataReader reader = cmd.ExecuteReader();

            //while (reader.Read())
            //{
            //    articles.Add(new Article(){
            //        ArticleId = int.Parse(reader[0]),
            //        );
            //}
            //reader.Close();
            return articles;
        }



    }

    public class DaoUrl
    {

        public static bool InsertFreshUrls(List<string> freshUrls)
        {
            string connStr = "server=localhost;user=root;database=news_data_pool;port=3306;password=3056jjjjj";
            MySqlConnection conn = new MySqlConnection(connStr);

            try
            {
                conn.Open();
                foreach (string url in freshUrls)
                {
                    string sql = string.Format($"INSERT INTO url (url) VALUE ('{url}')");
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
                return true;
            }
            catch (Exception e)
            {
                conn.Close();
                Log.Logger.Information(e.ToString());
                return false;
            }


        }

        public static List<string> GetAllKnownUrls(ref List<string> urlList)
        {


            string connStr = "server=localhost;user=root;database=news_data_pool;port=3306;password=3056jjjjj";
            MySqlConnection conn = new MySqlConnection(connStr);

            try
            {
                conn.Open();
                string sql = string.Format("SELECT url FROM url");
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    urlList.Add(reader[0].ToString());
                }
                reader.Close();
                conn.Close();
            }
            catch (Exception e)
            {
                conn.Close();
                Log.Logger.Information(e.ToString());

            }

            return urlList;

        }
    }


    public class DaoCategory
    {
        public static bool InitCategoryDB(List<Category> categories)
        {
            string connStr = "server=localhost;user=root;database=news_data_pool;port=3306;password=3056jjjjj";
            MySqlConnection conn = new MySqlConnection(connStr);

            try
            {
                conn.Open();
                foreach (Category category in categories)
                {
                    string sql = string.Format($"INSERT INTO category (category_name, relative_path) VALUES ('{category.Name}', '{category.Relative_Path}')");
                    MySqlCommand cmd = new MySqlCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }

                conn.Close();
                return true;
            }
            catch (Exception e)
            {
                conn.Close();
                Log.Logger.Information(e.ToString());
                return false;

            }
        }




      




        //databaseへのアクセスロジックがはみ出してしまうのでこれはだめ
        //public class DataAccessObject
        //{
        //    static readonly string connStr = "server=localhost;user=root;database=news_data_pool;port=3306;password=3056jjjjj";
        //    static MySqlConnection conn;

        //    public static bool AccessDB()
        //    {
        //        conn = new MySqlConnection(connStr);
        //        try
        //        {
        //            conn.Open();
        //            return true;
        //        }
        //        catch (Exception e)
        //        {
        //            conn.Close();
        //            Log.Logger.Information(e.ToString());
        //            return false;
        //        }
        //    }

        //    public static void CloseDB()
        //    {
        //        if (conn != null)
        //        {
        //            conn.Close();
        //            Debug.WriteLine("DB was closed");
        //        }
        //        else
        //        {
        //            Debug.WriteLine("MySqlConnection is null");
        //        }
        //    }
        //}

    }

}
