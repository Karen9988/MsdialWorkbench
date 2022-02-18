﻿using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    [TestClass()]
    public class IdentifySettingViewModelTests
    {
        [TestMethod()]
        public void DataBaseFocusWhenAnnotatorRemovedTest() {
            var model = BuildModel();
            var viewmodel = BuildViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            model.RemoveAnnotatorZZZ(viewmodel.AnnotatorViewModel.Value.Model);
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
        }

        [TestMethod()]
        public void AnnotatorFocusWhenDataBaseRemovedTest() {
            var model = BuildModel();
            var viewmodel = BuildViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            model.RemoveDataBaseZZZ(viewmodel.DataBaseViewModel.Value.Model);
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
        }

        [TestMethod()]
        public void DataBaseFocusWhenAnnotatorChangedTest() {
            var model = BuildModel();
            var viewmodel = BuildViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels[0];
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
        }

        [TestMethod()]
        public void AnnotatorFocusWhenDataBaseChangedTest() {
            var model = BuildModel();
            var viewmodel = BuildViewModel(model);

            // DB1 and Annotator1
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Msp;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // DB2 and Annotator2
            model.AddDataBaseZZZ();
            model.DataBaseModel = model.DataBaseModels.Last();
            model.DataBaseModel.DBSource = DataBaseSource.Text;
            model.AddAnnotatorZZZ(model.DataBaseModel);

            // Select DB2 and Annotator2
            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels.Last();
            viewmodel.AnnotatorViewModel.Value = viewmodel.AnnotatorViewModels.Last();

            viewmodel.DataBaseViewModel.Value = viewmodel.DataBaseViewModels[0];
            Assert.AreEqual(viewmodel.DataBaseViewModels[0], viewmodel.DataBaseViewModel.Value);
            Assert.AreEqual(viewmodel.AnnotatorViewModels[0], viewmodel.AnnotatorViewModel.Value);
        }

        private IdentifySettingModel BuildModel() {
            return new IdentifySettingModel(new ParameterBase(), new MockAnnotatorSettingModelFactory());
        }

        private IdentifySettingViewModel BuildViewModel(IdentifySettingModel model) {
            return new IdentifySettingViewModel(model, new MockAnnotatorSettingViewModelFactory());
        }
    }

    class MockAnnotatorSettingModel : IAnnotatorSettingModel
    {
        public MockAnnotatorSettingModel(DataBaseSettingModel model, string annotatorID) {
            AnnotationSource = SourceType.Unknown;
            DataBaseSettingModel = model;
            AnnotatorID = annotatorID;
        }

        public SourceType AnnotationSource { get; }

        public string AnnotatorID { get; set; }

        public DataBaseSettingModel DataBaseSettingModel { get; }
    }

    class MockAnnotatorSettingModelFactory : IAnnotatorSettingModelFactory
    {
        public IAnnotatorSettingModel Create(DataBaseSettingModel dataBaseSettingModel, string annotatorID, MsRefSearchParameterBase searchParameter) {
            return new MockAnnotatorSettingModel(dataBaseSettingModel, annotatorID);
        }
    }

    class MockAnnotatorSettingViewModel : IAnnotatorSettingViewModel
    {
        public MockAnnotatorSettingViewModel(IAnnotatorSettingModel model) {
            Model = model;
            AnnotatorID = new ReactiveProperty<string>(model.AnnotatorID);
            ObserveHasErrors = new ReadOnlyReactivePropertySlim<bool>(Observable.Never<bool>(), false);
        }

        public IAnnotatorSettingModel Model { get; }

        public ReactiveProperty<string> AnnotatorID { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        public void Dispose() {

        }
    }

    class MockAnnotatorSettingViewModelFactory : IAnnotatorSettingViewModelFactory
    {
        public IAnnotatorSettingViewModel Create(IAnnotatorSettingModel model) {
            return new MockAnnotatorSettingViewModel(model);
        }
    }
}