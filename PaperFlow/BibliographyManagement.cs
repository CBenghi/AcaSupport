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

        public static Dictionary<string, List<string>> GetUsage(FileInfo mdSource, IEnumerable<string> keys)
        {
            var doneMatches = new Dictionary<string, List<string>>();
            var reRefKey = new Regex("@[a-zA-Z0-9:_]+", RegexOptions.Compiled); // refkey in markdown 
            using (var mdSourceS = mdSource.OpenText())
            {
                var markDown = mdSourceS.ReadToEnd();
                
                foreach (var key in keys)
                {
                    List<string> thisKeyAdded = null;
                    var kRegex = new Regex(
                        // an open bracket not followed by a closed
                        @"(" + // capturing group
                        @"\[" + // open bracket
                        @"[^\]]*" + // anything but a closed one zero or more
                        @")?" + // end capturing group, optional
                        key + // the key
                        @"(" + // capturing group
                        @"[^\[]*" + // anything but an open one zero or more
                        @"\]" + // then a closing
                        @")?" + // end capturing group, optional
                        @"" + // open bracket
                        @""
                        );                   
                    foreach (Match match in kRegex.Matches(markDown))
                    {
                        if (thisKeyAdded == null)
                        {
                            thisKeyAdded = new List<string>();
                            doneMatches.Add(key, thisKeyAdded);
                        }
                        thisKeyAdded.Add(match.Value);
                    }
                }
            }
            return doneMatches;
        }
    }
}
