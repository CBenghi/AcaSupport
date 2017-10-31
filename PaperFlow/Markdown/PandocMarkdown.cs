using PaperFlow.Markdown;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PaperFlow.MarkDown
{
    public class PandocMarkdown
    {
        public static string BreakSentences(PandocMarkDownReader pandocReader)
        {
            var reBreak = new Regex(
                @"(?<punctuation>[\.\?!])" + // a captured punctuation
                @" +" + // one or more spaces
                @"(?<capital>[A-Z])" // a capital letter
                );
            var sb = new StringBuilder();
            
            foreach (var fragment in pandocReader.GetFragments())
            {
                // breaking only applies to normal text
                if (fragment.Type== PandocMarkDownFragment.FragmentType.Default)
                {
                    var line = reBreak.Replace(fragment.Content, "$1\r\n$2");
                    if (line != fragment.Content)
                    {

                    }
                    sb.AppendLine(line);
                    continue;
                }
                sb.AppendLine(fragment.Content);
            }
            return sb.ToString();
        }

        public static string JoinSentences(PandocMarkDownReader pandocReader)
        {
            throw new NotImplementedException();

            var sb = new StringBuilder();
            var buffer = new StringBuilder();
            
            foreach (var fragment in pandocReader.GetFragments())
            {
                // todo... add to the buffer until there's the end of a paragraph.

                // joining only applies to normal text
                if (fragment.Type == PandocMarkDownFragment.FragmentType.Default)
                {
                    

                    break;
                }
                // if buffer is not null then close the previous text and send the new data
                if (buffer.Length > 0)
                {
                    sb.AppendLine(buffer.ToString());
                    buffer = new StringBuilder();
                }
                sb.AppendLine(fragment.Content);
            }
            return sb.ToString();
        }
    }
}
