﻿using CompMs.App.Msdial.Model.Notification;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.View.Table;
using CompMs.App.Msdial.ViewModel;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.Message;
using CompMs.Graphics.UI.ProgressBar;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow() {
            InitializeComponent();

            var compoundSearchService = new DialogService<CompoundSearchWindow, CompoundSearchVM>(this);
            var peakSpotTableService = new DialogService<AlignmentSpotTable, PeakSpotTableViewModelBase>(this);
            var proteomicsTableService = new DialogService<ProteomicsSpotTable, PeakSpotTableViewModelBase>(this);
            var analysisFilePropertyResetService = new DialogService<AnalysisFilePropertyResettingWindow, AnalysisFilePropertySetViewModel>(this);
            var processSettingDialogService = new DialogService<ProjectSettingDialog, ProcessSettingViewModel>(this);
            DataContext = new MainWindowVM(
                compoundSearchService,
                peakSpotTableService,
                analysisFilePropertyResetService,
                proteomicsTableService,
                processSettingDialogService);

            broker = MessageBroker.Default;

            broker.ToObservable<ProgressBarMultiContainerVM>()
                .Subscribe(ShowProgressBar);
            broker.ToObservable<ExperimentSpectrumViewModel>()
                .Subscribe(OpenExperimentSpectrumView);
            broker.ToObservable<ProteinGroupTableViewModel>()
                .Subscribe(OpenProteinGroupTable);
            broker.ToObservable<SaveFileNameRequest>()
                .Subscribe(GetSaveFilePath);
            broker.ToObservable<ErrorMessageBoxRequest>()
                .Subscribe(ShowErrorComfirmationMessage);
            broker.ToObservable<AlignedChromatogramModificationViewModelLegacy>()
                .Subscribe(CreateAlignedChromatogramModificationDialog);
            broker.ToObservable<SampleTableViewerInAlignmentViewModelLegacy>()
                .Subscribe(CreateSampleTableViewerDialog);
            broker.ToObservable<NormalizationSetViewModel>()
                .Subscribe(OpenNormalizationSetView);
            broker.ToObservable<PcaSettingViewModel>()
                .Subscribe(OpenPcaSettingView);
#if DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#endif
        }

        private readonly IMessageBroker broker;

        public void CloseOwnedWindows() {
            Dispatcher.Invoke(() =>
            {
                foreach (var child in OwnedWindows.OfType<Window>()) {
                    if (child.IsLoaded) {
                        child.Close();
                    }
                }
            });
        }

        private void ShowProgressBar(ProgressBarMultiContainerVM viewmodel) {
            var dialog = new ProgressBarMultiContainerWindow
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            dialog.Loaded += async (s, e) =>
            {
                await viewmodel.RunAsync();
                viewmodel.Result = true;

                dialog.DialogResult = true;
                dialog.Close();
            };
            dialog.ShowDialog();
        }

        private void OpenExperimentSpectrumView(ExperimentSpectrumViewModel viewmodel) {
            var dialog = new ExperimentSpectrumView() { Owner = this, DataContext = viewmodel, };
            dialog.Show();
        }

        private void OpenProteinGroupTable(ProteinGroupTableViewModel viewmodel) {
            var dialog = new ProteinGroupTable() { Owner = this, DataContext = viewmodel, };
            dialog.Show();
        }

        private void OpenNormalizationSetView(NormalizationSetViewModel viewmodel) {
            if (viewmodel is null) {
                return;
            }
            var view = new NormalizationSetView {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            view.ShowDialog();
        }

        private void OpenPcaSettingView(PcaSettingViewModel viewmodel) {
            if (viewmodel is null) {
                MessageBox.Show("Please select an alignment result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var dialog = new PcaSettingView()
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.Show();
        }

        private void GetSaveFilePath(SaveFileNameRequest request) {
            var sfd = new SaveFileDialog
            {
                Title = request.Title,
                Filter = request.Filter,
                RestoreDirectory = request.RestoreDirectory,
                AddExtension = request.AddExtension,
            };

            if (sfd.ShowDialog(this) == true) {
                request.Run(sfd.FileName);
            }
        }

        private void ShowErrorComfirmationMessage(ErrorMessageBoxRequest request) {
            Dispatcher.Invoke(() =>
            {
                var result = MessageBox.Show(request.Content, request.Caption, request.ButtonType, MessageBoxImage.Error);
                request.Result = result;
            });
        }

        private void CreateAlignedChromatogramModificationDialog(AlignedChromatogramModificationViewModelLegacy vm) {
            Dispatcher.Invoke(() =>
            {
                var window = new AlignedPeakCorrectionWinLegacy(vm)
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Show();
            });
        }

        private void CreateSampleTableViewerDialog(SampleTableViewerInAlignmentViewModelLegacy vm) {
            Dispatcher.Invoke(() => {
                var window = new SampleTableViewerInAlignmentLegacy(vm) {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Show();
            });
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);

            if (Properties.Resources.VERSION.Contains("-tada")
                || Properties.Resources.VERSION.Contains("-alpha")
                || Properties.Resources.VERSION.Contains("-beta")
                || Properties.Resources.VERSION.Contains("-dev")) {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            var window = new ShortMessageWindow() {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Checking for updates.."
            };
            window.Show();
            VersionUpdateNotificationService.CheckForUpdates();
            window.Close();

            Mouse.OverrideCursor = null;
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);

            if (DataContext is MainWindowVM vm && vm.TaskProgressCollection.Any()) {
                var result = MessageBox.Show(
                    "A process is running in the background.\n" +
                    "If the application is terminated, the project may be corrupted.\n" +
                    "Do you want to close the application?",
                    "Warning",
                    MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK) {
                    e.Cancel = true;
                }
            }
        }
    }
}
