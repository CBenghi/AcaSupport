using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PaperFlow.Markdown
{
    class PandocMarkDownImages
    {
        public static IEnumerable<FileInfo> GetImages(FileInfo pandocFile)
        {
            var dir = pandocFile.Directory;
            var r = new Regex(@"!\[.*?\]\((?<imageSource>.*?)\)");

            using (var read = pandocFile.OpenText())
            {
                var body = read.ReadToEnd();
                foreach (Match m in r.Matches(body))
                {
                    var sourceName = m.Groups["imageSource"].Value;
                    var combinedSource = Path.Combine(dir.FullName, sourceName);
                    yield return new FileInfo(combinedSource);                    
                }
            }
        }
    }
}
