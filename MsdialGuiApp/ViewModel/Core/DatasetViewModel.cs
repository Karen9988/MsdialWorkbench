﻿using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Lcimms;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Core
{
    public class DatasetViewModel : ViewModelBase
    {
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;
        private readonly IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService;

        public DatasetViewModel(
            IDatasetModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService) {
            Model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;
            this.analysisFilePropertyResetService = analysisFilePropertyResetService;
            MethodViewModel = model.ToReactivePropertySlimAsSynchronized(
                m => m.Method,
                m => ConvertToViewModel(m),
                vm => vm?.Model)
            .AddTo(Disposables);
            FilePropertyResetCommand = new ReactiveCommand()
                .WithSubscribe(FilePropertyResetting)
                .AddTo(Disposables);
        }

        public IDatasetModel Model { get; }

        public ReactivePropertySlim<MethodViewModel> MethodViewModel { get; }

        public ReactiveCommand FilePropertyResetCommand { get; }

        private void FilePropertyResetting() {
            using (var analysisFilePropertySetWindowVM = new AnalysisFilePropertySetViewModel(Model.AnalysisFilePropertySetModel)) {
                var afpsw_result = analysisFilePropertyResetService.ShowDialog(analysisFilePropertySetWindowVM);
                if (afpsw_result == true) {
                    Model.AnalysisFilePropertyUpdate();
                }
            }
        }

        private MethodViewModel ConvertToViewModel(IMethodModel model) {
            switch (model) {
                case LcmsMethodModel lc:
                    return new LcmsMethodVM(lc, compoundSearchService, peakSpotTableService, proteomicsTableService);
                case ImmsMethodModel im:
                    return new ImmsMethodVM(im, compoundSearchService, peakSpotTableService);
                case DimsMethodModel di:
                    return new DimsMethodVM(di, compoundSearchService, peakSpotTableService);
                case LcimmsMethodModel lcim:
                    return new LcimmsMethodVM(lcim, compoundSearchService, peakSpotTableService);
                // case GcmsMethodModel _:
                default:
                    return null;
            }
        }
    }
}
