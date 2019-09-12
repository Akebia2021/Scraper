using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbotTest
{

    public class Article
    {
        public int ArticleId { get; set; }
        public string Title { get; set; }
        public string Contents { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Url { get; set; }

        public int CategoryId { get; set; }
        public int AuthorId { get; set; }
        public int BlogId { get; set; }

    }

    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Relative_Path { get; set; }
    }


    public class Author
    {
        public int AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string AuthorDescription { get; set; } //nullあり
        public string BlogId { get; set; } //nullあり
    }

    public class Blog
    {
        public int BlogId { get; set; }
        public string BlogName { get; set; }
        public string Relative_Path { get; set; }
    }
}
