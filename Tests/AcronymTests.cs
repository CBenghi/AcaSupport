using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AcademicSupport;
using System.Linq;

namespace Tests
{
    [TestClass]
    public class AcronymTests
    {
        [TestMethod]
        public void MatchesTest()
        {
            string t = "aligns=LLR LLP @GAT GIO HS2 H2S";

            var all = AcronymDesc.Find(t).Select(x => x.Value).ToArray();
            Assert.AreEqual(all.Length, 4);
        }
    }
}
