using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PaperFlow.Markdown
{
    public class PandocMarkDownReader : IDisposable
    {
        TextReader _reader;

        public PandocMarkDownReader(FileInfo fin)
        {
            _reader = fin.OpenText();
        }

        public PandocMarkDownReader(string markDownString)
        {
            _reader = new StringReader(markDownString);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        internal IEnumerable<PandocMarkDownFragment> GetFragments()
        {
            return GetFragments(_reader);
        }

        internal static IEnumerable<PandocMarkDownFragment> GetFragments(TextReader reader)
        {
            var codeblockEndingSequence = "";

            var status = PandocMarkDownFragment.FragmentType.Default;
            var cbRe = new Regex(@"^([`{3,}|~{3,}])");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                switch (status)
                {
                    case PandocMarkDownFragment.FragmentType.CodeBlock:
                        // check exit clause
                        yield return new PandocMarkDownFragment(line, status);
                        if (line.StartsWith(codeblockEndingSequence))
                        {
                            status = PandocMarkDownFragment.FragmentType.Default;
                        }
                        break;
                    case PandocMarkDownFragment.FragmentType.Default:
                        // codeblock found?
                        var cbm = cbRe.Match(line);
                        if (cbm.Success)
                        {
                            status = PandocMarkDownFragment.FragmentType.CodeBlock;
                            codeblockEndingSequence = cbm.Groups[1].Value;
                            yield return new PandocMarkDownFragment(line, status);
                            break;
                        }
                        else
                        {
                            yield return new PandocMarkDownFragment(line, status);
                            break;
                        }   
                
                }
            }
        }
    }
}