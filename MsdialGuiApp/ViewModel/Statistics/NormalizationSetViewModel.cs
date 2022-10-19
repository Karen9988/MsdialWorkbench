﻿using CompMs.App.Msdial.Model.Statistics;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class NormalizationSetViewModel : ViewModelBase
    {
        private readonly NormalizationSetModel _model;

        public NormalizationSetViewModel(NormalizationSetModel model, InternalStandardSetViewModel isSetViewModel) {
            _model = model;

            IsNormalizeNone = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeNone).AddTo(Disposables);
            IsNormalizeIS = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeIS).AddTo(Disposables);
            IsNormalizeLowess = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeLowess).AddTo(Disposables);
            IsNormalizeIsLowess = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeIsLowess).AddTo(Disposables);
            IsNormalizeSplash = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeSplash).AddTo(Disposables);
            IsNormalizeTic = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeTic).AddTo(Disposables);
            IsNormalizeMTic = model.ToReactivePropertySlimAsSynchronized(m => m.IsNormalizeMTic).AddTo(Disposables);

            SplashViewModel = new SplashSetViewModel(_model.SplashSetModel).AddTo(Disposables);
            IsSetViewModel = isSetViewModel;
            IsSetViewModelVisible = IsNormalizeIS.CombineLatest(IsNormalizeIsLowess, (a, b) => a || b).ToReadOnlyReactivePropertySlim(false).AddTo(Disposables);

            NormalizeCommand = model.CanNormalizeProperty
                .ToReactiveCommand()
                .WithSubscribe(model.Normalize)
                .AddTo(Disposables);
            CancelCommand = isSetViewModel.CancelCommand;
        }

        public ReactivePropertySlim<bool> IsNormalizeNone { get; }
        public ReactivePropertySlim<bool> IsNormalizeIS { get; }
        public ReactivePropertySlim<bool> IsNormalizeLowess { get; }
        public ReactivePropertySlim<bool> IsNormalizeIsLowess { get; }
        public ReactivePropertySlim<bool> IsNormalizeSplash { get; }
        public ReactivePropertySlim<bool> IsNormalizeTic { get; }
        public ReactivePropertySlim<bool> IsNormalizeMTic { get; }

        public SplashSetViewModel SplashViewModel { get; }
        public InternalStandardSetViewModel IsSetViewModel { get; }
        public ReadOnlyReactivePropertySlim<bool> IsSetViewModelVisible { get; }

        public ReactiveCommand NormalizeCommand { get; }
        public ReactiveCommand CancelCommand { get; }
    }
}
