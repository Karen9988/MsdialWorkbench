﻿using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class AnalysisPeakPlotViewModel : ViewModelBase
    {
        public AnalysisPeakPlotViewModel(
            AnalysisPeakPlotModel model,
            IObservable<IBrushMapper<ChromatogramPeakFeatureModel>> brushSource = null,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> verticalAxis = null) {
            Spots = model.Spots;

            if (horizontalAxis == null) {
                var hRange = model.ObserveProperty(m => m.HorizontalRange)
                    .ToReadOnlyReactivePropertySlim();
                horizontalAxis = new ReactiveAxisManager<double>(hRange);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis == null) {
                var vRange = model.ObserveProperty(m => m.VerticalRange)
                    .ToReadOnlyReactivePropertySlim();
                verticalAxis = new ReactiveAxisManager<double>(vRange);
            }
            VerticalAxis = verticalAxis;

            if (brushSource == null) {
                brushSource = Observable.Return<IBrushMapper<ChromatogramPeakFeatureModel>>(null);
            }
            Brush = brushSource.ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Target = model.TargetSource;

            GraphTitle = model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = model.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = model.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = model.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = model.LabelSource
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ObservableCollection<ChromatogramPeakFeatureModel> Spots { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; } 

        public ReadOnlyReactivePropertySlim<IBrushMapper<ChromatogramPeakFeatureModel>> Brush { get; }

        public IReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }
    }
}
