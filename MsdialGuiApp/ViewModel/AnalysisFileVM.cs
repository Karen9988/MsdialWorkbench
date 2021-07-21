﻿using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public abstract class TempAnalysisFileVM : ResultVM
    {
    }

    // In the future, TempAnalysisFileVM will be replaced by AnalysisFileViewModel.
    abstract class AnalysisFileViewModel : TempAnalysisFileVM
    {
        public AnalysisFileViewModel(AnalysisModelBase model) {
            if (model is null) {
                throw new System.ArgumentNullException(nameof(model));
            }

            Target = model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            CommentFilterKeywords = CommentFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AmplitudeOrderMax = model.Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0d;
            AmplitudeOrderMin = model.Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0d;
            AmplitudeLowerValue = new ReactivePropertySlim<double>(0d).AddTo(Disposables);
            AmplitudeUpperValue = new ReactivePropertySlim<double>(1d).AddTo(Disposables);

            Ms1PeaksView = CollectionViewSource.GetDefaultView(model.Ms1Peaks);

            DisplayLabel = model.ToReactivePropertySlimAsSynchronized(m => m.DisplayLabel).AddTo(Disposables);
        }

        public ICollectionView Ms1PeaksView {
            get => ms1PeaksView;
            private set => SetProperty(ref ms1PeaksView, value);
        }
        private ICollectionView ms1PeaksView;
        public override ICollectionView PeakSpotsView => ms1PeaksView;

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        // Filtering
        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }
        public ReactivePropertySlim<double> AmplitudeLowerValue { get; }
        public ReactivePropertySlim<double> AmplitudeUpperValue { get; }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        protected bool SetDisplayFilters(DisplayFilter flag, bool value) {
            if (ReadDisplayFilters(flag) != value) {
                WriteDisplayFilters(flag, value);
                OnPropertyChanged(nameof(DisplayFilters));
                return true;
            }
            return false;
        }

        protected void WriteDisplayFilters(DisplayFilter flag, bool value) {
            displayFilters.Write(flag, value);
        }

        protected bool ReadDisplayFilters(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        protected bool MetaboliteFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        protected bool CommentFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || (peak.Comment?.Contains(keyword) ?? false));
        }

        protected bool AmplitudeFilter(ChromatogramPeakFeatureModel peak) {
            return AmplitudeLowerValue.Value * (AmplitudeOrderMax - AmplitudeOrderMin) <= peak.AmplitudeOrderValue - AmplitudeOrderMin
                && peak.AmplitudeScore - AmplitudeOrderMin <= AmplitudeUpperValue.Value * (AmplitudeOrderMax - AmplitudeOrderMin);
        }

        public ReactivePropertySlim<string> DisplayLabel { get; }
    }
}