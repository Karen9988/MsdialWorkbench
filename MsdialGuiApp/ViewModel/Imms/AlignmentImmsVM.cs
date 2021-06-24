﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;


namespace CompMs.App.Msdial.ViewModel.Imms
{
    class AlignmentImmsVM : AlignmentFileVM
    {
        public AlignmentImmsVM(
            ImmsAlignmentModel model,
            IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator,
            IAnnotator<AlignmentSpotProperty, MSDecResult> textDBAnnotator,
            IWindowService<CompoundSearchVM> compoundSearchService) {

            this.model = model;
            this.compoundSearchService = compoundSearchService;

            this.mspAnnotator = mspAnnotator;
            this.textDBAnnotator = textDBAnnotator;

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Brushes = this.model.Brushes.AsReadOnly();
            SelectedBrush = this.model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            MassMin = this.model.MassMin;
            MassMax = this.model.MassMax;
            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);
            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too large" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);

            DriftMin = this.model.DriftMin;
            DriftMax = this.model.DriftMax;
            DriftLower = new ReactiveProperty<double>(DriftMin).AddTo(Disposables);
            DriftUpper = new ReactiveProperty<double>(DriftMax).AddTo(Disposables);
            DriftLower.SetValidateNotifyError(v => v < DriftMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftUpper.Value ? "Too large" : null);
            DriftUpper.SetValidateNotifyError(v => v < DriftLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftMax ? "Too large" : null);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty);

            CommentFilterKeywords = CommentFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                DriftLower.ToUnit(),
                DriftUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Spots.Refresh())
            .AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(this.model.Ms1Spots);

            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(model.PlotModel, SelectedBrush).AddTo(Disposables);
            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);
            BarChartViewModel = new Chart.BarChartViewModel(model.BarChartModel).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);
            AlignmentSpotTableViewModel = new ImmsAlignmentSpotTableViewModel(
                model.AlignmentSpotTableModel,
                MassLower, MassUpper,
                DriftLower, DriftUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = this.model.Target
                .CombineLatest(this.model.MsdecResult, (t, r) => t?.innerModel != null && r != null)
                .ToReactiveCommand()
                .AddTo(Disposables);
            SearchCompoundCommand.Subscribe(SearchCompound);
        }

        private readonly ImmsAlignmentModel model;

        public Chart.AlignmentPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private Chart.AlignmentPeakPlotViewModel plotViewModel;

        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel {
            get => ms2SpectrumViewModel;
            set => SetProperty(ref ms2SpectrumViewModel, value);
        }
        private Chart.MsSpectrumViewModel ms2SpectrumViewModel;

        public Chart.BarChartViewModel BarChartViewModel {
            get => barChartViewModel;
            set => SetProperty(ref barChartViewModel, value);
        }
        private Chart.BarChartViewModel barChartViewModel;

        public Chart.AlignmentEicViewModel AlignmentEicViewModel {
            get => alignmentEicViewModel;
            set => SetProperty(ref alignmentEicViewModel, value);
        }
        private Chart.AlignmentEicViewModel alignmentEicViewModel;

        public ImmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => alignmentSpotTableViewModel;
            set => SetProperty(ref alignmentSpotTableViewModel, value);
        }
        private ImmsAlignmentSpotTableViewModel alignmentSpotTableViewModel;

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Spots != null) ms1Spots.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Spots;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<IBrushMapper<AlignmentSpotPropertyModel>> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }
        public double DriftMax { get; }
        public ReactiveProperty<double> DriftLower { get; }
        public ReactiveProperty<double> DriftUpper { get; }

        public IReactiveProperty<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }
        public IReactiveProperty<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            protected internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = 0;

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && DriftFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered)
                    && (!ManuallyModifiedChecked || spot.innerModel.IsManuallyModifiedForAnnotation)
                    && CommentFilter(spot, CommentFilterKeywords.Value)
                    && MetaboliteFilter(spot, MetaboliteFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched
                || SuggestedChecked && spot.IsSuggested
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower.Value <= spot.MassCenter
                && spot.MassCenter <= MassUpper.Value;
        }

        bool DriftFilter(AlignmentSpotPropertyModel spot) {
            return DriftLower.Value <= spot.innerModel.TimesCenter.Drift.Value
                && spot.innerModel.TimesCenter.Drift.Value <= DriftUpper.Value;
        }

        bool CommentFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Comment.Contains(keyword));
        }

        bool MetaboliteFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Name.Contains(keyword));
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IAnnotator<AlignmentSpotProperty, MSDecResult> mspAnnotator, textDBAnnotator;

        private void SearchCompound() {
            if (model.Target.Value?.innerModel == null || model.MsdecResult.Value == null)
                return;

            using (var model = new ImmsCompoundSearchModel<AlignmentSpotProperty>(
                this.model.AlignmentFile,
                Target.Value.innerModel,
                this.model.MsdecResult.Value,
                null,
                mspAnnotator,
                new MsRefSearchParameterBase(this.model.Parameter.MspSearchParam)))
            using (var vm = new ImmsCompoundSearchVM(model)) {
                compoundSearchService.ShowDialog(vm);
            }
        }

        public DelegateCommand<Window> ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand<Window>(ShowIonTable));
        private DelegateCommand<Window> showIonTableCommand;

        private void ShowIonTable(Window owner) {
            var window = new View.Table.AlignmentSpotTable
            {
                DataContext = AlignmentSpotTableViewModel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = owner,
            };

            window.Show();
        }

        public void SaveProject() {
            model.SaveProject();
        }

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return DisplayFilters.Read(flags);
        }
    }
}
