using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PaperFlow.Markdown
{
    internal class PandocMarkDownReader : IDisposable
    {
        StreamReader _reader;
        
        internal PandocMarkDownReader(FileInfo fin)
        {
            _reader = fin.OpenText();
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        internal IEnumerable<PandocMarkDownFragment> GetFragments()
        {
            return GetFragments(_reader);
        }

        internal static IEnumerable<PandocMarkDownFragment> GetFragments(StreamReader reader)
        {
            var re = new Regex(@"^[`{3,}|`{3,}]");

            while (!reader.EndOfStream)
            {
                // todo discriminate code blocks
                //
                var line = reader.ReadLine();
                yield return new PandocMarkDownFragment(line, PandocMarkDownFragment.FragmentType.undiscriminated);
            }
        }
    }
}
