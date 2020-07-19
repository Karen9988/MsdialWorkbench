﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;

using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Common;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;

namespace MsdialDimsCoreUiTestApp
{
    internal class MainWindowVM : INotifyPropertyChanged
    {
        public ObservableCollection<ChromatogramPeak> Ms1Peaks {
            get => ms1Peaks;
            set => SetProperty(ref ms1Peaks, value);
        }

        public Rect Ms1Area
        {
            get => ms1Area;
            set => SetProperty(ref ms1Area, value);
        }

        public Rect Ms2Area
        {
            get => ms2Area;
            set => SetProperty(ref ms2Area, value);
        }

        public ObservableCollection<Ms2Info> Ms2Features
        {
            get => ms2Features;
            set => SetProperty(ref ms2Features, value);
        }

        private ObservableCollection<ChromatogramPeak> ms1Peaks;
        private ObservableCollection<Ms2Info> ms2Features;
        private Rect ms1Area, ms2Area;

        public MainWindowVM()
        {
            // testfiles
            var filepath = @"C:\Users\YUKI MATSUZAWA\works\data\sciex_msmsall\704_Egg2 Egg Yolk.abf";
            var lbmFile = @"C:\Users\YUKI MATSUZAWA\works\data\lbm\MSDIAL_LipidDB_Test.lbm2";
            var textLibraryFile = @"C:\Users\YUKI MATSUZAWA\works\data\textlib\TestLibrary.txt";
            var param = new MsdialDimsParameter() {
                IonMode = CompMs.Common.Enum.IonMode.Negative,
                MspFilePath = lbmFile,
                TextDBFilePath = textLibraryFile,
                TargetOmics = CompMs.Common.Enum.TargetOmics.Lipidomics,
                LipidQueryContainer = new CompMs.Common.Query.LipidQueryBean() {
                    SolventType = CompMs.Common.Enum.SolventType.HCOONH4
                },
                MspSearchParam = new CompMs.Common.Parameter.MsRefSearchParameterBase() {
                    WeightedDotProductCutOff = 0.1F, SimpleDotProductCutOff = 0.1F,
                    ReverseDotProductCutOff = 0.4F, MatchedPeaksPercentageCutOff = 0.8F,
                    MinimumSpectrumMatch = 1
                }
            };

            var iupacDB = IupacResourceParser.GetIUPACDatabase();
            List<MoleculeMsReference> mspDB = null;
            if (param.TargetOmics == TargetOmics.Metablomics) {
                mspDB = MspFileParser.MspFileReader(param.MspFilePath);
            } else if (param.TargetOmics == TargetOmics.Lipidomics) {
                var lbmQueries = LbmQueryParcer.GetLbmQueries(true);
                var extension = System.IO.Path.GetExtension(param.MspFilePath);
                if (extension == ".lbm2") {
                    mspDB = MspFileParser.ReadSerializedLbmLibrary(param.MspFilePath, lbmQueries,
                        param.IonMode, param.LipidQueryContainer.SolventType, param.LipidQueryContainer.CollisionType);
                }
                else {
                    mspDB = MspFileParser.LbmFileReader(param.MspFilePath, lbmQueries,
                        param.IonMode, param.LipidQueryContainer.SolventType, param.LipidQueryContainer.CollisionType);
                }
            }
            mspDB.Sort((a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));

            List<MoleculeMsReference> textDB = null;
            textDB = TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out string _);
            textDB.Sort((a, b) => a.PrecursorMz.CompareTo(b.PrecursorMz));

            var spectras = DataAccess.GetAllSpectra(filepath);
            var ms1spectra = spectras.Where(spectra => spectra.MsLevel == 1)
                                     .Where(spectra => spectra.Spectrum != null)
                                     .Max(spectra => (length: spectra.Spectrum.Length, spectra: spectra))
                                     .spectra;

            var chromPeaks = ComponentsConverter.ConvertRawPeakElementToChromatogramPeakList(ms1spectra.Spectrum);
            var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
            var peakPickResults = PeakDetection.PeakDetectionVS1(sChromPeaks, param.MinimumDatapoints, param.MinimumAmplitude);
            var chromatogramPeakFeatures = GetChromatogramPeakFeatures(peakPickResults, ms1spectra, spectras);
            SetSpectrumPeaks(chromatogramPeakFeatures, spectras);

            var ms2spectra = spectras.Where(spectra => spectra.MsLevel == 2)
                                     .Where(spectra => spectra.Spectrum != null);


            Ms1Peaks = new ObservableCollection<ChromatogramPeak>(sChromPeaks);
            Ms1Area = new Rect(new Point(sChromPeaks.Min(peak => peak.Mass), sChromPeaks.Min(peak => peak.Intensity)),
                               new Point(sChromPeaks.Max(peak => peak.Mass), sChromPeaks.Max(peak => peak.Intensity)));
            Ms2Area = new Rect(0, 0, 1000, 1);
            var results = chromatogramPeakFeatures.Select(feature => CalculateAndSetAnnotatedReferences(feature, mspDB, textDB, param)).ToList();

            Ms2Features = new ObservableCollection<Ms2Info>(
                chromatogramPeakFeatures.Zip(results, (feature, result) =>
                {
                    var spectrum = ScalingSpectrumPeaks(ComponentsConverter.ConvertToSpectrumPeaks(spectras[feature.MS2RawSpectrumID].Spectrum));
                    var centroid = ScalingSpectrumPeaks(feature.Spectrum);
                    // TODO: check please
                    var mspIDs = feature.MSRawID2MspIDs.IsEmptyOrNull() ? new List<int>() : feature.MSRawID2MspIDs[feature.MS2RawSpectrumID];
                    var detectedMsp = mspIDs.Zip(result.Msp, (id, res) => new AnnotationResult{Reference = mspDB[id], Result = res });
                    var detectedText = feature.TextDbIDs.Zip(result.Text, (id, res) => new AnnotationResult { Reference = textDB[id], Result = res });
                    var detected = detectedMsp.Concat(detectedText).ToList();
                    foreach (var det in detected)
                        det.Reference.Spectrum = ScalingSpectrumPeaks(det.Reference.Spectrum);
                    return new Ms2Info
                    {
                        ChromatogramPeakFeature = feature,
                        PeakID = feature.PeakID,
                        Mass = feature.Mass,
                        Intensity = feature.PeakHeightTop,
                        Spectrum = spectrum,
                        Centroids = centroid,
                        Detected = detected,
                        //Annotated = 0
                    };
                })
                );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyname) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyname));

        bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyname = "")
        {
            if (value == null && property == null || value.Equals(property)) return false;
            property = value;
            RaisePropertyChanged(propertyname);
            return true;
        }

        private List<SpectrumPeak> ScalingSpectrumPeaks(IEnumerable<SpectrumPeak> spectrumPeaks)
        {
            if (!spectrumPeaks.Any()) return new List<SpectrumPeak>();
            var min = spectrumPeaks.Min(peak => peak.Intensity);
            var width = spectrumPeaks.Max(peak => peak.Intensity) - min;

            return spectrumPeaks.Select(peak => new SpectrumPeak(peak.Mass, (peak.Intensity - min) / width)).ToList();
        }

        private List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(List<PeakDetectionResult> peakPickResults, RawSpectrum ms1Spectrum, List<RawSpectrum> allSpectra) {
            var peakFeatures = new List<ChromatogramPeakFeature>();
            var ms2SpecObjects = allSpectra.Where(n => n.MsLevel == 2 && n.Precursor != null).OrderBy(n => n.Precursor.SelectedIonMz).ToList();

            foreach (var result in peakPickResults) {

                // here, the chrom scan ID should be matched to the scan number of RawSpectrum Element
                var peakFeature = DataAccess.GetChromatogramPeakFeature(result, ChromXType.Mz, ChromXUnit.Mz, ms1Spectrum.Spectrum[result.ScanNumAtPeakTop].Mz);
                var chromScanID = peakFeature.ChromScanIdTop;
                peakFeature.ChromXs.RT = new RetentionTime(0);
                peakFeature.ChromXsTop.RT = new RetentionTime(0);
                peakFeature.IonMode = ms1Spectrum.ScanPolarity == ScanPolarity.Positive ? CompMs.Common.Enum.IonMode.Positive : CompMs.Common.Enum.IonMode.Negative;
                peakFeature.PrecursorMz = ms1Spectrum.Spectrum[chromScanID].Mz;
                peakFeature.MS1RawSpectrumIdTop = ms1Spectrum.ScanNumber;
                peakFeature.ScanID = ms1Spectrum.ScanNumber;
                peakFeature.MS2RawSpectrumID2CE = GetMS2RawSpectrumIDs(peakFeature.PrecursorMz, ms2SpecObjects); // maybe, in msmsall, the id count is always one but for just in case
                peakFeature.MS2RawSpectrumID = GetRepresentativeMS2RawSpectrumID(peakFeature.MS2RawSpectrumID2CE, allSpectra);
                // foreach (var spec in allSpectra[peakFeature.MS2RawSpectrumID].Spectrum)
                //     peakFeature.AddPeak(spec.Mz, spec.Intensity);
                peakFeatures.Add(peakFeature);

                // result check
                Console.WriteLine("Peak ID={0}, Scan ID={1}, MZ={2}, MS2SpecID={3}, Height={4}, Area={5}",
                    peakFeature.PeakID, peakFeature.ChromScanIdTop, peakFeature.ChromXsTop.Mz.Value, peakFeature.MS2RawSpectrumID, peakFeature.PeakHeightTop, peakFeature.PeakAreaAboveZero);
            }

            return peakFeatures;
        }

        private int GetRepresentativeMS2RawSpectrumID(Dictionary<int, double> ms2RawSpectrumID2CE, List<RawSpectrum> allSpectra) {
            if (ms2RawSpectrumID2CE.Count == 0) return -1;

            var maxIntensity = 0.0;
            var maxIntensityID = -1;
            foreach (var pair in ms2RawSpectrumID2CE) {
                var specID = pair.Key;
                var specObj = allSpectra[specID];
                if (specObj.TotalIonCurrent > maxIntensity) {
                    maxIntensity = specObj.TotalIonCurrent;
                    maxIntensityID = specID;
                }
            }
            return maxIntensityID;
        }

        /// <summary>
        /// currently, the mass tolerance is based on ad hoc (maybe can be added to parameter obj.)
        /// the mass tolerance is considered by the basic quadrupole mass resolution.
        /// </summary>
        /// <param name="precursorMz"></param>
        /// <param name="allSpectra"></param>
        /// <param name="mzTolerance"></param>
        /// <returns></returns>
        private Dictionary<int, double> GetMS2RawSpectrumIDs(double precursorMz, List<RawSpectrum> ms2SpecObjects, double mzTolerance = 0.25) {
            var ID2CE = new Dictionary<int, double>();
            var startID = GetSpectrumObjectStartIndexByPrecursorMz(precursorMz, mzTolerance, ms2SpecObjects);
            for (int i = startID; i < ms2SpecObjects.Count; i++) {
                var spec = ms2SpecObjects[i];
                var precursorMzObj = spec.Precursor.SelectedIonMz;
                if (precursorMzObj < precursorMz - mzTolerance) continue;
                if (precursorMzObj > precursorMz + mzTolerance) break;

                ID2CE[spec.ScanNumber] = spec.CollisionEnergy;
            }
            return ID2CE; // maybe, in msmsall, the id count is always one but for just in case
        }

        private int GetSpectrumObjectStartIndexByPrecursorMz(double targetedMass, double massTolerance, List<RawSpectrum> ms2SpecObjects) {
            if (ms2SpecObjects.Count == 0) return 0;
            var targetMass = targetedMass - massTolerance;
            int startIndex = 0, endIndex = ms2SpecObjects.Count - 1;
            int counter = 0;

            if (targetMass > ms2SpecObjects[endIndex].Precursor.SelectedIonMz) return endIndex;

            while (counter < 5) {
                if (ms2SpecObjects[startIndex].Precursor.SelectedIonMz <= targetMass && targetMass < ms2SpecObjects[(startIndex + endIndex) / 2].Precursor.SelectedIonMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (ms2SpecObjects[(startIndex + endIndex) / 2].Precursor.SelectedIonMz <= targetMass && targetMass < ms2SpecObjects[endIndex].Precursor.SelectedIonMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private void SetSpectrumPeaks(List<ChromatogramPeakFeature> chromFeatures, List<RawSpectrum> spectra) {
            foreach (var feature in chromFeatures) {
                if (feature.MS2RawSpectrumID < 0 || feature.MS2RawSpectrumID > spectra.Count - 1) {

                }
                else {
                    var peakElements = spectra[feature.MS2RawSpectrumID].Spectrum;
                    var spectrumPeaks = ComponentsConverter.ConvertToSpectrumPeaks(peakElements);
                    var centroidSpec = SpectralCentroiding.Centroid(spectrumPeaks);
                    feature.Spectrum = centroidSpec;
                }

                Console.WriteLine("Peak ID={0}, Scan ID={1}, Spectrum count={2}", feature.PeakID, feature.ScanID, feature.Spectrum.Count);
            }
        }

        private (List<MsScanMatchResult> Msp, List<MsScanMatchResult> Text) CalculateAndSetAnnotatedReferences(ChromatogramPeakFeature chromatogramPeakFeature, List<MoleculeMsReference> mspDB, List<MoleculeMsReference> textDB, MsdialDimsParameter param)
        {
            AnnotationProcess.Run(chromatogramPeakFeature, mspDB, textDB, param.MspSearchParam, param.TargetOmics, out List<MsScanMatchResult> mspResult, out List<MsScanMatchResult> textResult);
            Console.WriteLine("PeakID={0}, Annotation={1}", chromatogramPeakFeature.PeakID, chromatogramPeakFeature.Name);
            return (mspResult, textResult);
        }

        private List<SpectrumPeak> MergeReferences(List<(MoleculeMsReference, double)> references, double tolerance)
        {
            var spectrums = new List<List<SpectrumPeak>>(references.Count);
            foreach ((MoleculeMsReference reference, double ratio) in references)
            {
                if (ratio == 0) continue;
                var spectrum = reference.Spectrum.Select(peak => new SpectrumPeak(peak.Mass, peak.Intensity * ratio)).ToList();
                spectrums.Add(spectrum);
            }

            return MergeSpectrums(spectrums, tolerance);
        }

        private List<SpectrumPeak> MergeSpectrums(List<List<SpectrumPeak>> spectrums, double tolerance)
        {
            var n = spectrums.Count;
            if (n == 0) return new List<SpectrumPeak>();
            if (n == 1) return spectrums.First();

            return MergeSpectrum(
                MergeSpectrums(spectrums.GetRange(0, n / 2), tolerance),
                MergeSpectrums(spectrums.GetRange(n / 2, n - n / 2), tolerance),
                tolerance
                );
        }

        private List<SpectrumPeak> MergeSpectrum(List<SpectrumPeak> spectrum1, List<SpectrumPeak> spectrum2, double tolerance)
        {
            var result = new List<SpectrumPeak>(spectrum1.Count + spectrum2.Count);
            int i = 0, j = 0;
            while (i < spectrum1.Count && j < spectrum2.Count)
            {
                if (Math.Abs(spectrum1[i].Mass - spectrum2[j].Mass) < tolerance)
                {
                    result.Add(new SpectrumPeak(spectrum1[i].Mass, spectrum1[i].Intensity + spectrum2[j].Intensity));
                    ++i; ++j;
                }
                else if (spectrum1[i].Mass < spectrum2[j].Mass)
                {
                    result.Add(spectrum1[i]);
                    ++i;
                }
                else
                {
                    result.Add(spectrum2[j]);
                    ++j;
                }
            }
            if (i < spectrum1.Count)
                result.AddRange(spectrum1.GetRange(i, spectrum1.Count - i));
            if (j < spectrum2.Count)
                result.AddRange(spectrum2.GetRange(j, spectrum2.Count - j));

            return result;
        } 
    }

    internal class AnnotationResult
    {
        public MoleculeMsReference Reference { get; set; }
        public MsScanMatchResult Result { get; set; }
        public double Score => Result.TotalScore;
    }

    internal class Ms2Info
    {
        public ChromatogramPeakFeature ChromatogramPeakFeature { get; set; }
        public int PeakID { get; set; }
        public double Mass { get; set; }
        public double Intensity { get; set; }
        public List<SpectrumPeak> Spectrum { get; set; }
        public List<SpectrumPeak> Centroids { get; set; }
        public List<AnnotationResult> Detected { get; set; }
        public int Annotated { get; set; }
    }
}
