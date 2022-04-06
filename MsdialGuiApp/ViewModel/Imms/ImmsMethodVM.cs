﻿using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsMethodVM : MethodViewModel
    {
        public ImmsMethodVM(
            ImmsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model) {
            if (compoundSearchService is null) {
                throw new System.ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new System.ArgumentNullException(nameof(peakSpotTableService));
            }

            this.model = model;

            AnalysisViewModel = model.ObserveProperty(m => m.AnalysisModel)
                .Where(m => m != null)
                .Select(m => new AnalysisImmsVM(m, compoundSearchService, peakSpotTableService) { DisplayFilters = displayFilters })
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AlignmentViewModel = model.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new AlignmentImmsVM(m, compoundSearchService, peakSpotTableService) { DisplayFilters = displayFilters })
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            PropertyChanged += OnDisplayFiltersChanged;
        }

        private readonly ImmsMethodModel model;

        public ReadOnlyReactivePropertySlim<AnalysisImmsVM> AnalysisViewModel { get; }
        public ReadOnlyReactivePropertySlim<AlignmentImmsVM> AlignmentViewModel { get; }

        public bool RefMatchedChecked {
            get => ReadDisplayFilter(DisplayFilter.RefMatched);
            set => WriteDisplayFilter(DisplayFilter.RefMatched, value);
        }
        public bool SuggestedChecked {
            get => ReadDisplayFilter(DisplayFilter.Suggested);
            set => WriteDisplayFilter(DisplayFilter.Suggested, value);
        }
        public bool UnknownChecked {
            get => ReadDisplayFilter(DisplayFilter.Unknown);
            set => WriteDisplayFilter(DisplayFilter.Unknown, value);
        }
        public bool CcsChecked {
            get => ReadDisplayFilter(DisplayFilter.CcsMatched);
            set => WriteDisplayFilter(DisplayFilter.CcsMatched, value);
        }
        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilter(DisplayFilter.Ms2Acquired);
            set => WriteDisplayFilter(DisplayFilter.Ms2Acquired, value);
        }
        public bool MolecularIonChecked {
            get => ReadDisplayFilter(DisplayFilter.MolecularIon);
            set => WriteDisplayFilter(DisplayFilter.MolecularIon, value);
        }
        public bool BlankFilterChecked {
            get => ReadDisplayFilter(DisplayFilter.Blank);
            set => WriteDisplayFilter(DisplayFilter.Blank, value);
        }
        public bool UniqueIonsChecked {
            get => ReadDisplayFilter(DisplayFilter.UniqueIons);
            set => WriteDisplayFilter(DisplayFilter.UniqueIons, value);
        }
        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilter(DisplayFilter.ManuallyModified);
            set => WriteDisplayFilter(DisplayFilter.ManuallyModified, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        void OnDisplayFiltersChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(displayFilters)) {
                if (AnalysisViewModel.Value != null)
                    AnalysisViewModel.Value.DisplayFilters = displayFilters;
                if (AlignmentViewModel.Value != null)
                    AlignmentViewModel.Value.DisplayFilters = displayFilters;
            }
        }

        private bool ReadDisplayFilter(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            displayFilters.Write(flag, set);
            OnPropertyChanged(nameof(displayFilters));
        }

        public override int InitializeNewProject(Window window) {
            model.InitializeNewProject(window);

            AnalysisFilesView.MoveCurrentToFirst();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();

            return 0;
        }

        public override void LoadProject() {
            AnalysisFilesView.MoveCurrentToFirst();
            model.Load();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File == null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);
        }

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(model.ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;
        

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(model.ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;

        public DelegateCommand<Window> ShowTicCommand => showTicCommand ?? (showTicCommand = new DelegateCommand<Window>(model.ShowTIC));
        private DelegateCommand<Window> showTicCommand;

        public DelegateCommand<Window> ShowBpcCommand => showBpcCommand ?? (showBpcCommand = new DelegateCommand<Window>(model.ShowBPC));
        private DelegateCommand<Window> showBpcCommand;

        public DelegateCommand<Window> ShowTicBpcRepEICCommand => showTicBpcRepEIC ?? (showTicBpcRepEIC = new DelegateCommand<Window>(model.ShowTicBpcRepEIC));
        private DelegateCommand<Window> showTicBpcRepEIC;

        public DelegateCommand<Window> ShowEicCommand => showEicCommand ?? (showEicCommand = new DelegateCommand<Window>(model.ShowEIC));
        private DelegateCommand<Window> showEicCommand;
    }
}
