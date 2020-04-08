﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISMP_Pluging.ViewChildren.Util;

namespace ISMP_Pluging.Class
{
    public static class ResetPBScript
    {
        public const long Id = -2;
        public const string Name = "Reset Programmable Block";
        public const string Code = "public Program()\r\n{{\r\nStorage = \"\";\r\n}}\r\n\r\npublic void Save()\r\n{{\r\n\r\n}}\r\n\r\npublic void Main(string argument, UpdateType updateSource)\r\n{{\r\n\r\n}}\r\n";
        public static readonly string MD5 = Utility.GetMD5Hash(Code);

    }
}
