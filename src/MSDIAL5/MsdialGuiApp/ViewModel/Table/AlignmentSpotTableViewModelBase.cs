﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal abstract class AlignmentSpotTableViewModelBase : PeakSpotTableViewModelBase {
        private readonly AlignmentSpotTableModelBase _model;
        private readonly IMessageBroker _broker;

        public AlignmentSpotTableViewModelBase(AlignmentSpotTableModelBase model, PeakSpotNavigatorViewModel peakSpotNavigatorViewModel, ICommand setUnknonwCommand, UndoManagerViewModel undoManagerViewModel, IMessageBroker broker)
            : base(model, peakSpotNavigatorViewModel, setUnknonwCommand, undoManagerViewModel)
        {
            _model = model;
            _broker = broker;
            BarItemsLoader = model.BarItemsLoader;
            ClassBrush = model.ClassBrush.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            FileClassPropertiesModel = model.FileClassProperties;
            MarkAllAsConfirmedCommand = new ReactiveCommand().WithSubscribe(model.MarkAllAsConfirmed).AddTo(Disposables);
            SwitchTagCommand = new ReactiveCommand<PeakSpotTag>().WithSubscribe(model.SwitchTag).AddTo(Disposables);
            ExportMatchedSpectraCommand = new AsyncReactiveCommand().WithSubscribe(ExportMatchedSpectraAsync).AddTo(Disposables);
        }

        public IObservable<IBarItemsLoader> BarItemsLoader { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<BarItem>> ClassBrush { get; }
        public FileClassPropertiesModel FileClassPropertiesModel { get; }

        public ReactiveCommand MarkAllAsConfirmedCommand { get; }
        public ReactiveCommand<PeakSpotTag> SwitchTagCommand { get; }
        public AsyncReactiveCommand ExportMatchedSpectraCommand { get; }

        private Task ExportMatchedSpectraAsync() {
            var request = new SelectFolderRequest
            {
                Title = "Select folder to save spectra",
            };
            _broker?.Publish(request);
            return _model.ExportMatchedSpectraAsync(request.SelectedPath);
        }
    }
}