using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer
{
    public class Head
    {
        private DateTime? time;
        private string? author;
        private string? name;
        public string? Name { get => name; set => name = value; }
        public DateTime? Time { get => time; set => time = value; }
        public string? Author { get => author; set => author = value; }

        public Head() { }

        public Head(DateTime? atime, string? aauthor)
        {
            time = atime;
            author = aauthor;
        }
    }
}
