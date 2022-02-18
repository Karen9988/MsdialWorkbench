﻿using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class LPCLipidParserTests
    {
        [TestMethod()]
        [DataRow("LPC 18:1", 521.34814000, LbmClass.LPC, 2)]
        [DataRow("LPC 18:1/0:0", 521.34814000, LbmClass.LPC, 3)]
        public void ParseTest(string lipidName, double expectedMass, LbmClass lipidClass, int expectedLevel) {
            var parser = new LPCLipidParser();

            var lipid = parser.Parse(lipidName);
            Assert.AreEqual(lipidName, lipid.Name);
            Assert.AreEqual(expectedMass, lipid.Mass, 0.001);
            Assert.AreEqual(lipidClass, lipid.LipidClass);
            Assert.AreEqual(expectedLevel, lipid.AnnotationLevel);
        }
    }

    [TestClass()]
    public class LPELipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LPELipidParser();

            var lipid = parser.Parse("LPE 18:1");
            Assert.AreEqual(479.2939, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPE, lipid.LipidClass);

            lipid = parser.Parse("LPE 18:1/0:0");
            Assert.AreEqual(479.2939, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPE, lipid.LipidClass);

        }
    }

    [TestClass()]
    public class LPGLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LPGLipidParser();

            var lipid = parser.Parse("LPG 18:1");
            Assert.AreEqual(510.2885, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPG, lipid.LipidClass);

            lipid = parser.Parse("LPG 18:1/0:0");
            Assert.AreEqual(510.2885, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPG, lipid.LipidClass);

        }
    }

    [TestClass()]
    public class LPILipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LPILipidParser();

            var lipid = parser.Parse("LPI 18:1");
            Assert.AreEqual(598.3045, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPI, lipid.LipidClass);

            lipid = parser.Parse("LPI 18:1/0:0");
            Assert.AreEqual(598.3045, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPI, lipid.LipidClass);

        }
    }

    [TestClass()]
    public class LPSLipidParserTests
    {
        [TestMethod()]
        public void ParseTest()
        {
            var parser = new LPSLipidParser();

            var lipid = parser.Parse("LPS 18:1");
            Assert.AreEqual(523.2837, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPS, lipid.LipidClass);

            lipid = parser.Parse("LPS 18:1/0:0");
            Assert.AreEqual(523.2837, lipid.Mass, 0.01);
            Assert.AreEqual(LbmClass.LPS, lipid.LipidClass);

        }
    }

  
}