using System;
using System.Collections.Generic;
using System.Text;

namespace PaperFlow
{
    public interface FileUnlocker
    {
        void RequestUnlock(string file);
    }
}
