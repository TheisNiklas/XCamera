﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace XCamera.iOS
{
    class ExifiOS : XCamera.IExif
    {
        public async Task<string> GetComment(string filename)
        {
            string basedir = Path.GetDirectoryName(filename);
            string basename = Path.GetFileName(filename);


            return "";
        }
        public async Task<string> SetComment(string filename, string newComment)
        {
            string basename = Path.GetFileName(filename);


            return "";
        }
    }
}