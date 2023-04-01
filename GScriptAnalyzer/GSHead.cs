using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptAnalyzer
{
    public class GSHead
    {
        private DateTime? time;
        private string? author;
        private string? name;
        public string? Name { get => name; set => name = value; }
        public DateTime? Time { get => time; set => time = value; }
        public string? Author { get => author; set => author = value; }

        public GSHead() { }

        public GSHead(DateTime? atime, string? aauthor)
        {
            time = atime;
            author = aauthor;
        }
    }
}
