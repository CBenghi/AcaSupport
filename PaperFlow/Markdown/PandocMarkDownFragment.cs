using System;
using System.Collections.Generic;
using System.Text;

namespace PaperFlow.Markdown
{
    public class PandocMarkDownFragment
    {
        private string _content;
        private FragmentType _type;

        public PandocMarkDownFragment(string content, FragmentType type)
        {
            _content = content;
            _type = type;
        }

        public enum FragmentType
        {
            codeblock,
            undiscriminated
        }
    }
}
