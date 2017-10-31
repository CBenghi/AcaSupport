using System;
using System.Collections.Generic;
using System.Text;

namespace PaperFlow.Markdown
{
    public class PandocMarkDownFragment
    {
        public string Content;
        public FragmentType Type;

        public PandocMarkDownFragment(string content, FragmentType type)
        {
            Content = content;
            Type = type;
        }

        public enum FragmentType
        {
            CodeBlock,
            Default
        }
    }
}
