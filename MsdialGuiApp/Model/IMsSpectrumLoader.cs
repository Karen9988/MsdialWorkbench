﻿using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public interface IMsSpectrumLoader<in T>
    {
        IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(T target);
    }

    internal sealed class MsRawSpectrumLoader : IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        public MsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            this.provider = provider;
            this.parameter = parameter;
        }

        private readonly IDataProvider provider;
        private readonly ParameterBase parameter;

        public Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            return target is null
                ? Task.FromResult(new List<SpectrumPeak>())
                : LoadSpectrumCoreAsync(target, token);
        }

        private async Task<List<SpectrumPeak>> LoadSpectrumCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target.MS2RawSpectrumId < 0) {
                return new List<SpectrumPeak>(0);
            }
            var msSpectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var spectra = DataAccess.GetCentroidMassSpectra(
                msSpectra[target.MS2RawSpectrumId],
                parameter.MS2DataType,
                0f, float.MinValue, float.MaxValue);
            if (parameter.RemoveAfterPrecursor) {
                spectra = spectra.Where(peak => peak.Mass <= target.Mass + parameter.KeptIsotopeRange).ToList();
            }
            return spectra;
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(ChromatogramPeakFeatureModel target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }

    internal sealed class MultiMsRawSpectrumLoader : DisposableModelBase, IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        private readonly IDataProvider _provider;
        private readonly ParameterBase _parameter;
        private readonly Task<ReadOnlyCollection<RawSpectrum>> _msSpectra;

        public MultiMsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _msSpectra = _provider.LoadMsSpectrumsAsync(default);
            _ms2List = new Subject<List<int>>().AddTo(Disposables);
            Ms2IdSelector = new ReactivePropertySlim<int>().AddTo(Disposables);
        }

        public ReactivePropertySlim<int> Ms2IdSelector { get; }

        public IObservable<List<int>> Ms2List => _ms2List;
        private readonly Subject<List<int>> _ms2List;

        private IObservable<List<SpectrumPeak>> LoadSpectrumAsObservableCore(ChromatogramPeakFeatureModel target) {
            if (target.InnerModel.MS2RawSpectrumID2CE.Count == 0) {
                return Observable.Return(new List<SpectrumPeak>(0));
            }
            _ms2List.OnNext(target.InnerModel.MS2RawSpectrumID2CE.Keys.ToList());
            Ms2IdSelector.Value = target.MS2RawSpectrumId;

            return Observable.FromAsync(() => _msSpectra).CombineLatest(Ms2IdSelector, (msSpectra, ms2Id) =>
            {
                var spectra = DataAccess.GetCentroidMassSpectra(msSpectra[ms2Id], _parameter.MS2DataType, 0f, float.MinValue, float.MaxValue);
                if (_parameter.RemoveAfterPrecursor) {
                    spectra = spectra.Where(spectrum => spectrum.Mass <= target.Mass + _parameter.KeptIsotopeRange).ToList();
                }
                return spectra;
            });
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(ChromatogramPeakFeatureModel target) {
            return target is null
                ? Observable.Return(new List<SpectrumPeak>(0))
                : LoadSpectrumAsObservableCore(target);
        }
    }

    internal sealed class MsDecSpectrumLoader : IMsSpectrumLoader<object>
    {
        public MsDecSpectrumLoader(
            MSDecLoader loader,
            IReadOnlyList<object> ms1Peaks) {

            this.ms1Peaks = ms1Peaks;
            this.loader = loader;
        }

        public MSDecResult Result { get; private set; }

        private readonly MSDecLoader loader;
        private readonly IReadOnlyList<object> ms1Peaks;

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(object target, CancellationToken token) {
            var ms2DecSpectrum = new List<SpectrumPeak>();
            if (target != null) {
                ms2DecSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested();
            return ms2DecSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(object target) {
            var idx = ms1Peaks.IndexOf(target);
            if (target.GetType() == typeof(ChromatogramPeakFeatureModel)) {
                var peak = (ChromatogramPeakFeatureModel)ms1Peaks[idx];
                idx = peak.MSDecResultIDUsedForAnnotation;
            } 
            else if (target.GetType() == typeof(AlignmentSpotPropertyModel)) {
                var peak = (AlignmentSpotPropertyModel)ms1Peaks[idx];
                //idx = peak.MSDecResultIDUsedForAnnotation;
            }
            else {
               
            }
            var msdecResult = loader.LoadMSDecResult(idx);
            Result = msdecResult;
            return msdecResult?.Spectrum ?? new List<SpectrumPeak>(0);
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(object target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }

    internal sealed class MsRefSpectrumLoader : IMsSpectrumLoader<IAnnotatedObject>
    {
        public MsRefSpectrumLoader(DataBaseMapper mapper) {
            this.mapper = mapper;
        }

        //public MsRefSpectrumLoader(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
        //    this.moleculeRefer = refer;
        //}

        //public MsRefSpectrumLoader(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
        //    this.peptideRefer = refer;
        //}

        //private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> moleculeRefer;
        //private readonly IMatchResultRefer<PeptideMsReference, MsScanMatchResult> peptideRefer;
        private readonly DataBaseMapper mapper;

        public async Task<List<SpectrumPeak>> LoadSpectrumAsync(IAnnotatedObject target, CancellationToken token) {
            var ms2ReferenceSpectrum = new List<SpectrumPeak>();

            if (target != null) {
                ms2ReferenceSpectrum = await Task.Run(() => LoadSpectrumCore(target), token).ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();
            return ms2ReferenceSpectrum;
        }

        private List<SpectrumPeak> LoadSpectrumCore(IAnnotatedObject target) {
            var representative = target.MatchResults.Representative;
            if (representative.Source == SourceType.FastaDB) {
                //var reference = peptideRefer.Refer(representative);
                var reference = mapper.PeptideMsRefer(representative);
                if (reference != null) {
                    return reference.Spectrum;
                }
            }
            else {
                var reference = mapper.MoleculeMsRefer(representative);
                if (reference != null) {
                    return reference.Spectrum;
                }
            }
           
            return new List<SpectrumPeak>();
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(IAnnotatedObject target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }
}
