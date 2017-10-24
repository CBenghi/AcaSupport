using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcademicSupport
{
    public class WindowsFileUlocker : PaperFlow.FileUnlocker
    {
        public void RequestUnlock(string file)
        {
            var lockingProcesses = FileUtil.WhoIsLocking(file);
            foreach (var lockingProcess in lockingProcesses)
            {
                lockingProcess.CloseMainWindow();
                lockingProcess.WaitForExit(); // todo, this will have to change to a fixed amount of time
            }
        }
    }
}
