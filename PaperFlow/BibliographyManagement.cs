using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcademicSupport
{
    public class BibliographyManagement
    {

        public static Regex reNewBib = new Regex("^@[a-z]+{(.+),$", RegexOptions.Compiled);
        
        public static Dictionary<string, string> BibliographyAsDictionary(FileInfo fullBib)
        {
            var avails = new Dictionary<string, string>();
            using (var mdSourceS = fullBib.OpenText())
            {
                string key = "";
                string val = "";
                string line;
                while ((line = mdSourceS.ReadLine()) != null)
                {
                    var testNewBib = reNewBib.Match(line);
                    if (testNewBib.Success)
                    {
                        key = "@" + testNewBib.Groups[1].Value;
                        val = line + "\r\n";
                    }
                    else if (line == "}")
                    {
                        val += line + "\r\n";
                        if (key != "")
                        {
                            avails.Add(key, val);
                        }
                        key = "";
                        val = "";
                    }
                    else
                    {
                        val += line + "\r\n";
                    }
                }
            }

            return avails;
        }

    }
}
