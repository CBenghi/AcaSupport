using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AcademicSupport
{
    public class AcronymDesc
    {
        // (?<![@=]) is a lookbehind that excludes the @ for the cit references, and = for the aligns=LLR syntax
        static Regex _acronymMatch = new Regex(@"\b(?<![@=])([A-Z][A-Z1-9][0-9A-Za-z]*)\b");

        public int FirstBracketed => AllBracketed.FirstOrDefault();
        public int FirstUse => AllEntries.FirstOrDefault();

        public List<int> AllEntries;
        public List<int> AllBracketed;
        public bool Ignore = false;

        internal AcronymDesc(string tla, string doc)
        {
            var bracketedMatch = new Regex(@"\([\s\*_]*" + tla + @"[\s\*_]*\)");
            AllBracketed = bracketedMatch.Matches(doc).Cast<Match>().Select(x => x.Index).ToList();


            var acronymMatch = new Regex(@"\b" + tla + @"\b");
            AllEntries = acronymMatch.Matches(doc).Cast<Match>().Select(x => x.Index).ToList();

            if (FirstBracketed != 0)
            {
                var prec = doc.Substring(FirstBracketed - 1, 1);
                if (prec == "!")
                    Ignore = true;
            }
        }

        public string Status
        {
            get
            {
                if (FirstBracketed == 0)
                    return "<undefined>";
                if (FirstBracketed <= FirstUse)
                    return $"<ok>";
                return "<early>";
            }
        }

        public string ToS()
        {
            // return "First bracketed: " +  FirstBracketedMatch + " - All: " + string.Join(", ", AllEntries.ToArray());
            return $"{Status}\t{AllEntries.Count}\t{AllBracketed.Count}";
        }

        internal static void RemovePluralForms(Dictionary<string, AcronymDesc> acronymDictionary)
        {
            Regex endsInSmallS = new Regex("(.*)s$");
            var plurals = acronymDictionary.Keys.Where(x => endsInSmallS.IsMatch(x)).ToList();
            foreach (var plural in plurals)
            {
                var singular = endsInSmallS.Match(plural).Groups[1].Value;
                if (!acronymDictionary.ContainsKey(singular))
                    continue;
                acronymDictionary[singular].Merge(acronymDictionary[plural]);
                acronymDictionary.Remove(plural);
            }
        }

        private void Merge(AcronymDesc otherAcronym)
        {
            AllBracketed = AllBracketed.Union(otherAcronym.AllBracketed).ToList();
            AllEntries = AllEntries.Union(otherAcronym.AllEntries).ToList();
            Ignore = Ignore || otherAcronym.Ignore;
        }

        public static IEnumerable<Match> Find(string markDown)
        {
            foreach (Match match in _acronymMatch.Matches(markDown))
            {
                yield return match;
            }
        }
    }

}
