using PaperFlow.Markdown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PaperFlow.MarkDown
{
    public class PandocMarkdown
    {
        public static string BreakSentences(string fileIn)
        {
            FileInfo fin = new FileInfo(fileIn);
            var p = new PandocMarkDownReader(fin);
            foreach (var fragment in p.GetFragments())
            {
                
            }
            return "";
        }
    }
}
