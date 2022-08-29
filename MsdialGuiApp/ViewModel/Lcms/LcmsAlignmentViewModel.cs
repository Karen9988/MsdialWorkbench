﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        private readonly LcmsAlignmentModel _model;
        private readonly IWindowService<CompoundSearchVM> _compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _proteomicsTableService;
        private readonly IMessageBroker _broker;

        public LcmsAlignmentViewModel(
            LcmsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model ?? throw new ArgumentNullException(nameof(model));
            _compoundSearchService = compoundSearchService ?? throw new ArgumentNullException(nameof(compoundSearchService));
            _peakSpotTableService = peakSpotTableService ?? throw new ArgumentNullException(nameof(peakSpotTableService));
            _proteomicsTableService = proteomicsTableService ?? throw new ArgumentNullException(nameof(proteomicsTableService));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));

            Target = _model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(_model.Ms1Spots);

            var (peakPlotAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(_model.PlotModel, peakPlotAction, peakPlotFocused).AddTo(Disposables);

            Ms2SpectrumViewModel = new MsSpectrumViewModel(_model.Ms2SpectrumModel).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            BarChartViewModel = new BarChartViewModel(_model.BarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            AlignmentEicViewModel = new AlignmentEicViewModel(_model.AlignmentEicModel).AddTo(Disposables);
            
            AlignmentSpotTableViewModel = new LcmsAlignmentSpotTableViewModel(
                _model.AlignmentSpotTableModel,
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
                .AddTo(Disposables);
            ProteomicsAlignmentTableViewModel = new LcmsProteomicsAlignmentTableViewModel(
                _model.AlignmentSpotTableModel,
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.RtLowerValue,
                PeakSpotNavigatorViewModel.RtUpperValue,
                PeakSpotNavigatorViewModel.ProteinFilterKeyword,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
                .AddTo(Disposables);

            SearchCompoundCommand = _model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);
            
            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, };

            ProteinResultContainerAsObservable = Observable.Return(model.ProteinResultContainerModel);

            NormalizationSetViewModel = new NormalizationSetViewModel(model.NormalizationSetModel).AddTo(Disposables);
            PcaSettingViewModel = new PcaSettingViewModel(model.PcaSettingModel).AddTo(Disposables);
        }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public ICollectionView Ms1Spots { get; }
        public ICollectionView PeakSpotsView => Ms1Spots;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
        public MsSpectrumViewModel Ms2SpectrumViewModel { get; }
        public BarChartViewModel BarChartViewModel { get; }
        public AlignmentEicViewModel AlignmentEicViewModel { get; }
        public LcmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }
        public LcmsProteomicsAlignmentTableViewModel ProteomicsAlignmentTableViewModel { get; }
        public AlignedChromatogramModificationViewModelLegacy AlignedChromatogramModificationViewModel { get; }
        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }
        public ReactiveCommand SearchCompoundCommand { get; }
        public IObservable<ProteinResultContainerModel> ProteinResultContainerAsObservable { get; }
        public NormalizationSetViewModel NormalizationSetViewModel { get; }
        public PcaSettingViewModel PcaSettingViewModel { get; }

        private void SearchCompound() {
            using (var csm = _model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new LcmsCompoundSearchViewModel(csm)) {
                    _compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            if (_model.Parameter.TargetOmics == CompMs.Common.Enum.TargetOmics.Proteomics) {
                _proteomicsTableService.Show(ProteomicsAlignmentTableViewModel);
            }
            else {
                _peakSpotTableService.Show(AlignmentSpotTableViewModel);
            }
        }

        public DelegateCommand SaveSpectraCommand => _saveSpectraCommand ?? (_saveSpectraCommand = new DelegateCommand(SaveSpectra, _model.CanSaveSpectra));
        private DelegateCommand _saveSpectraCommand;

        private void SaveSpectra() {
            var request = new SaveFileNameRequest(_model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);
        }
    }
}
