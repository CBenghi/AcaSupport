using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PaperFlow.MarkDown;
using System.IO;
using PaperFlow.Markdown;

namespace Tests
{
    [TestClass]
    public class PandocMarkDownTests
    {
        [TestMethod]
        [DeploymentItem(@"TestFiles\WholeSentences.md") ]
        [DeploymentItem(@"TestFiles\BrokenSentences.md")]
        public void BreakSentencesTest()
        {
            // prepare
            FileInfo fin = new FileInfo("WholeSentences.md");
            var f = new FileInfo("BrokenSentences.md");

            var broken = "";

            // run
            using (var p = new PandocMarkDownReader(fin))
            using (var r = f.OpenText())
            {
                var expected = r.ReadToEnd();
                broken = PandocMarkdown.BreakSentences(p);
                Assert.AreEqual(expected, broken);
            }

            // todo: resume

            //// now convert back
            //using (var r = fin.OpenText())
            //using (var p = new PandocMarkDownReader(broken))
            //{
            //    var original = r.ReadToEnd();
            //    var joined = PandocMarkdown.JoinSentences(p);
            //    Assert.AreEqual(joined, original);
            //}
        }
    }
}
