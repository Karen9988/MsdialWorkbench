﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class MspDbRestorationKeyTests
    {
        [TestMethod()]
        public void MspDbRestorationKeyTest() {
            IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> key = new MspDbRestorationKey("MspKey");
            IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> expected = key;
            IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> actual;

            using (var stream = new MemoryStream()) {
                Common.MessagePack.MessagePackDefaultHandler.SaveToStream<IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(key, stream);
                actual = Common.MessagePack.MessagePackDefaultHandler.LoadFromStream<IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>>(stream);
            }

            Assert.AreEqual(expected.Key, actual.Key);
        }

        [TestMethod()]
        public void AcceptTest() {
            var key = new MspDbRestorationKey("MspKey");
            var visitor = new MockLoadAnnotator(key);
            key.Accept(visitor, null);
            Assert.IsTrue(visitor.Called);
        }
    }

    class MockLoadAnnotator : ILoadAnnotatorVisitor
    {
        private readonly IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> expected;

        public MockLoadAnnotator(IReferRestorationKey<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> expected) {
            this.expected = expected;
        }

        public bool Called { get; private set; } = false;

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(StandardRestorationKey key, MoleculeDataBase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(MspDbRestorationKey key, MoleculeDataBase database) {
            Assert.AreEqual(expected, key);
            Called = true;
            return null;
        }

        public ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Visit(TextDbRestorationKey key, MoleculeDataBase database) {
            throw new NotImplementedException();
        }

        public ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> Visit(ShotgunProteomicsRestorationKey key, ShotgunProteomicsDB database) {
            throw new NotImplementedException();
        }
    }
}