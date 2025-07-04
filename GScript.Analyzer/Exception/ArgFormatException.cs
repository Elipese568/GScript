﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer.Exception
{
    public class ArgFormatException : System.Exception
    {
        public ArgFormatException() : base() { }
        public ArgFormatException(string message) : base(message) { }
    }
}
