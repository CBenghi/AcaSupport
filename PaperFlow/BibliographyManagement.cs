using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        
        // function converted to returning data from a json bibliography format
        // exported by betterCSL
        public static Dictionary<string, string> BibliographyAsDictionary(FileInfo fullBib)
        {
            var avails = new Dictionary<string, string>();

            // read JSON directly from a file
            using (StreamReader file = File.OpenText(fullBib.FullName))
            using (JsonTextReader reader = new JsonTextReader(file))
            {
                JArray o2 = (JArray)JToken.ReadFrom(reader);

                foreach (var reference in o2.Children())
                {
                    var id = reference.SelectToken("id");

                    avails.Add(id.Value<string>(), reference.ToString());
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
                    List<string> thisKeyList = null;
                    var kRegex = new Regex(
                        // an open bracket not followed by a closed
                        @"(" + // capturing group
                        @"\[" + // open bracket
                        @"[^\]]*" + // anything but a closed one zero or more
                        @")?" + // end capturing group, optional
                        key + // the key
                        @"\b"+
                        @"(" + // capturing group
                        @"[^\[]*" + // anything but an open one zero or more
                        @"\]" + // then a closing
                        @")?" + // end capturing group, optional
                        @"" + // open bracket
                        @""
                        );                   
                    foreach (Match match in kRegex.Matches(markDown))
                    {
                        if (thisKeyList == null)
                        {
                            thisKeyList = new List<string>();
                            doneMatches.Add(key, thisKeyList);
                        }
                        thisKeyList.Add(match.Value);
                    }
                }
            }
            return doneMatches;
        }

        // this functions searches for caseInsensitive matches of available ref keys 
        // between an @ and a word boundary to attempt fixing when case is incorrect
        //
        public static string FixReferences(FileInfo mdSource, string[] keys)
        {
            using (var mdSourceS = mdSource.OpenText())
            {
                var markDown = mdSourceS.ReadToEnd();
                foreach (var key in keys)
                {
                    Regex search = new Regex("@" + key + @"\b", RegexOptions.IgnoreCase);
                    markDown = search.Replace(markDown, $"@{key}");
                }
                return markDown;
            }
        }   
    }
}
