using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcademicSupport
{
    class PandocConversionResult
    {
        public FileInfo ConvertedFile;
        public int ExitCode;
        public string Report;
        public long Milliseconds;
    }
}
