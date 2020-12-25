﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class PeakAligner
    {
        protected DataAccessor Accessor { get; set; }
        protected PeakJoiner Joiner { get; set; }
        protected GapFiller Filler { get; set; }
        protected AlignmentRefiner Refiner { get; set; }
        protected ParameterBase Param { get; set; }
        protected IupacDatabase Iupac { get; set; }

        public PeakAligner(DataAccessor accessor, PeakJoiner joiner, GapFiller filler, AlignmentRefiner refiner, ParameterBase param, IupacDatabase iupac) {
            Accessor = accessor;
            Joiner = joiner;
            Filler = filler;
            Refiner = refiner;
            Param = param;
            Iupac = iupac;
        }

        public PeakAligner(AlignmentProcessFactory factory) {
            Accessor = factory.CreateDataAccessor();
            Joiner = factory.CreatePeakJoiner();
            Filler = factory.CreateGapFiller();
            Refiner = factory.CreateAlignmentRefiner();
            Param = factory.Parameter;
            Iupac = factory.Iupac;
        }

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var spots = Joiner.Join(analysisFiles, Param.AlignmentReferenceFileID, Accessor);
            spots = FilterAlignments(spots, analysisFiles);

            CollectPeakSpots(analysisFiles, alignmentFile, spots, spotSerializer);
            IsotopeAnalysis(spots);
            spots = GetRefinedAlignmentSpotProperties(spots);

            return PackingSpots(spots);
        }

        protected virtual List<AlignmentSpotProperty> FilterAlignments(
            List<AlignmentSpotProperty> spots, IReadOnlyList<AnalysisFileBean> analysisFiles ) {
            var result = spots.Where(spot => spot.AlignedPeakProperties.Any(peak => peak.MasterPeakID >= 0));

            var peakCountThreshold = Param.PeakCountFilter / 100 * analysisFiles.Count;
            result = result.Where(spot => spot.AlignedPeakProperties.Count(peak => peak.MasterPeakID >= 0) >= peakCountThreshold);

            if (Param.QcAtLeastFilter) {
                var qcidx = analysisFiles.WithIndex().Where(fi => fi.Item1.AnalysisFileType == AnalysisFileType.QC).Select(fi => fi.Item2).ToArray();
                result = result.Where(spot => qcidx.All(idx => spot.AlignedPeakProperties[idx].MasterPeakID >= 0));
            }

            Func<AlignmentSpotProperty, bool> IsNPercentDetectedInOneGroup = GetNPercentDetectedInOneGroupFilter(analysisFiles);
            result = result.Where(IsNPercentDetectedInOneGroup);

            return result.ToList();
        }

        private Func<AlignmentSpotProperty, bool> GetNPercentDetectedInOneGroupFilter(IReadOnlyList<AnalysisFileBean> files) {
            var groupDic = new Dictionary<string, List<int>>();
            foreach ((var file, var idx) in files.WithIndex()) {
                if (!groupDic.ContainsKey(file.AnalysisFileClass))
                    groupDic[file.AnalysisFileClass] = new List<int>();
                groupDic[file.AnalysisFileClass].Add(idx);
            }

            double threshold = Param.NPercentDetectedInOneGroup / 100d;

            bool isNPercentDetected(AlignmentSpotProperty spot) {
                return groupDic.Any(kvp => kvp.Value.Count(idx => spot.AlignedPeakProperties[idx].MasterPeakID >= 0) >= threshold * kvp.Value.Count);
            }

            return isNPercentDetected;
        }

        private void CollectPeakSpots(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            List<AlignmentSpotProperty> spots, ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var files = new List<string>();
            var chromPeakInfoSerializer = spotSerializer == null ? null : ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");

            foreach (var analysisFile in analysisFiles) {
                var peaks = new List<AlignmentChromPeakFeature>(spots.Count);
                foreach (var spot in spots)
                    peaks.Add(spot.AlignedPeakProperties.FirstOrDefault(peak => peak.FileID == analysisFile.AnalysisFileId));
                var file = CollectAlignmentPeaks(analysisFile, peaks, spots, chromPeakInfoSerializer);
                files.Add(file);
            }
            foreach (var spot in spots)
                PackingSpot(spot);

            if (chromPeakInfoSerializer != null)
                SerializeSpotInfo(spots, files, alignmentFile, spotSerializer, chromPeakInfoSerializer);
            foreach (var f in files)
                if (File.Exists(f))
                    File.Delete(f);
        }

        protected virtual string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<AlignmentSpotProperty> spots,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var peakInfos = new List<ChromatogramPeakInfo>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                var spectra = DataAccess.GetAllSpectra(rawDataAccess);
                foreach ((var peak, var spot) in peaks.Zip(spots)) {
                    if (spot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                        Filler.GapFill(spectra, spot, analysisFile.AnalysisFileId);
                    }

                    // UNDONE: retrieve spectrum data
                    var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                    var peaklist = DataAccess.GetMs1Peaklist(
                        spectra, (float)peak.Mass,
                        (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                        peak.IonMode);
                    var peakInfo = new ChromatogramPeakInfo(
                        peak.FileID, peaklist,
                        (float)peak.ChromXsTop.Value,
                        (float)peak.ChromXsLeft.Value,
                        (float)peak.ChromXsRight.Value
                        );
                    peakInfos.Add(peakInfo);
                }
            }
            var file = Path.GetTempFileName();
            serializer?.SerializeAllToFile(file, peakInfos);
            return file;
        }

        private void IsotopeAnalysis(IReadOnlyList<AlignmentSpotProperty> alignmentSpots) {
            foreach (var spot in alignmentSpots) {
                if (Param.TrackingIsotopeLabels || spot.IsReferenceMatched) {
                    spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
                    spot.PeakCharacter.IsotopeWeightNumber = 0;
                }
                if (!spot.IsReferenceMatched) {
                    spot.AdductType.AdductIonName = string.Empty;
                }
            }
            if (Param.TrackingIsotopeLabels) return;

            IsotopeEstimator.Process(alignmentSpots, Param, Iupac);
        }

        private List<AlignmentSpotProperty> GetRefinedAlignmentSpotProperties(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.Count <= 1) return alignmentSpots;
            return Refiner.Refine(alignmentSpots);
        }
        private AlignmentResultContainer PackingSpots(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.IsEmptyOrNull()) return null;

            var minInt = (double)alignmentSpots.Min(spot => spot.HeightMin);
            var maxInt = (double)alignmentSpots.Max(spot => spot.HeightMax);

            maxInt = maxInt > 1 ? Math.Log(maxInt, 2) : 1;
            minInt = minInt > 1 ? Math.Log(minInt, 2) : 0;

            for (int i = 0; i < alignmentSpots.Count; i++) {
                var relativeValue = (float)((Math.Log(alignmentSpots[i].HeightMax, 2) - minInt) / (maxInt - minInt));
                alignmentSpots[i].RelativeAmplitudeValue = Math.Min(1, Math.Max(0, relativeValue));
            }

            var spots = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = Param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = spots.Count,
                AlignmentSpotProperties = spots,
            };
        }

        private void PackingSpot(AlignmentSpotProperty spot) {
            foreach (var child in spot.AlignmentDriftSpotFeatures)
                DataObjConverter.SetRepresentativeProperty(child);

            DataObjConverter.SetRepresentativeProperty(spot);
        }

        private void SerializeSpotInfo(
            IReadOnlyCollection<AlignmentSpotProperty> spots, IEnumerable<string> files,
            AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
            ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
            var pss = files.Select(file => peakSerializer.DeserializeAllFromFile(file)).ToList();
            var qss = pss.Sequence();

            Debug.WriteLine("Serialize start.");
            using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
                spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
            }
            Debug.WriteLine("Serialize finish.");

            pss.ForEach(ps => ((IDisposable)ps).Dispose());
        }
    }
}
