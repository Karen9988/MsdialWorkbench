﻿using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm.Annotation
{
    public sealed class LcimmsMspAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> {
        private static readonly IComparer<IMSIonProperty> comparer = CompositeComparer.Build<IMSIonProperty>(MassComparer.Comparer, ChromXsComparer.RTComparer, CollisionCrossSectionComparer.Comparer);

        private readonly TargetOmics omics;

        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;

        public LcimmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID, int priority)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            db.Sort(comparer);
            this.omics = omics;
            ReferObject = mspDB;
        }

        public MsScanMatchResult Annotate(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, DataAccess.GetNormalizedMSScanProperty(query.Scan, parameter), query.Isotopes, parameter, omics, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, DataAccess.GetNormalizedMSScanProperty(query.Scan, parameter), query.Isotopes, parameter, omics, Key);
        }


        private List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID) {

            var candidates = SearchCore(property, parameter);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, scan, isotopes, candidate, candidate.IsotopicPeaks, parameter, omics, annotatorID);
                ValidateCore(result, property, scan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            var normScan = DataAccess.GetNormalizedMSScanProperty(query.Scan, parameter);
            var result = CalculateScoreCore(query.Property, normScan, query.Isotopes, reference, reference.IsotopicPeaks, parameter, omics, Key);
            ValidateCore(result, query.Property, normScan, reference, parameter, omics);
            return result;
        }

        private MsScanMatchResult CalculateScoreCore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID) {

            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var matchedPeaksScores = omics == TargetOmics.Lipidomics
                ? MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd)
                : MsScanMatching.GetMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                WeightedDotProduct = (float)weightedDotProduct, SimpleDotProduct = (float)simpleDotProduct, ReverseDotProduct = (float)reverseDotProduct,
                MatchedPeaksPercentage = (float)matchedPeaksScores[0], MatchedPeaksCount = (float)matchedPeaksScores[1],
                AcurateMassSimilarity = (float)ms1Similarity, IsotopeSimilarity = (float)isotopeSimilarity,
                Source = SourceType.MspDB, AnnotatorID = annotatorID, Priority = Priority,
            };

            if (parameter.IsUseTimeForAnnotationScoring) {
                var rtSimilarity = MsScanMatching.GetGaussianSimilarity(property.ChromXs.RT.Value, reference.ChromXs.RT.Value, parameter.RtTolerance);
                result.RtSimilarity = (float)rtSimilarity;
            }
            if (parameter.IsUseCcsForAnnotationScoring) {
                var ccsSimilarity = MsScanMatching.GetGaussianSimilarity(property.CollisionCrossSection, reference.CollisionCrossSection, parameter.CcsTolerance);
                result.CcsSimilarity = (float)ccsSimilarity;
            }
            result.TotalScore = (float)CalculateAnnotatedScoreCore(result, parameter);

            return result;
        }

        public double CalculateAnnotatedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateAnnotatedScoreCore(result, parameter);
        }

        public double CalculateSuggestedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateSuggestedScoreCore(result, parameter);
        }

        private static double CalculateAnnotatedScoreCore(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
                scores.Add((result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3);
            if (result.MatchedPeaksPercentage >= 0)
                scores.Add(result.MatchedPeaksPercentage);
            if (parameter.IsUseTimeForAnnotationScoring && result.RtSimilarity >= 0)
                scores.Add(result.RtSimilarity);
            if (parameter.IsUseCcsForAnnotationScoring && result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        private static double CalculateSuggestedScoreCore(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            var scores = new List<float> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (parameter.IsUseTimeForAnnotationScoring && result.RtSimilarity >= 0)
                scores.Add(result.RtSimilarity);
            if (parameter.IsUseCcsForAnnotationScoring && result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery query) {
            return SearchCore(query.Property, query.Parameter ?? Parameter).ToList();
        }

        private IReadOnlyList<MoleculeMsReference> SearchCore(IMSIonProperty property, MsRefSearchParameterBase parameter) {
            if (parameter.IsUseTimeForAnnotationFiltering && Parameter.IsUseCcsForAnnotationFiltering) {
                return SearchWithRtCcsCore(property, parameter.Ms1Tolerance, parameter.RtTolerance, parameter.CcsTolerance);
            }
            else if (parameter.IsUseCcsForAnnotationFiltering) {
                return SearchWithCcsCore(property, parameter.Ms1Tolerance, parameter.CcsTolerance);
            }
            else if (parameter.IsUseTimeForAnnotationFiltering) {
                return SearchWithRtCore(property, parameter.Ms1Tolerance, parameter.RtTolerance);
            }
            else {
                return SearchMassCore(property, parameter.Ms1Tolerance);
            }
        }

        private MassReferenceSearcher<MoleculeMsReference> Searcher
            => searcher ?? (searcher = new MassReferenceSearcher<MoleculeMsReference>(db));
        private MassReferenceSearcher<MoleculeMsReference> searcher;

        private IReadOnlyList<MoleculeMsReference> SearchMassCore(IMSProperty property, double massTolerance) {
            return Searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz)));
        }

        private MassRtCcsReferenceSearcher<MoleculeMsReference> SearcherWithRtCcs
            => searcherWithRtCcs ?? (searcherWithRtCcs = new MassRtCcsReferenceSearcher<MoleculeMsReference>(db));
        private MassRtCcsReferenceSearcher<MoleculeMsReference> searcherWithRtCcs;

        private IReadOnlyList<MoleculeMsReference> SearchWithRtCore(IMSProperty property, double massTolerance, double rtTolerance) {
            return SearcherWithRtCcs.Search(MSIonSearchQuery.CreateMassRtQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz), property.ChromXs.RT.Value, rtTolerance));
        }

        private IReadOnlyList<MoleculeMsReference> SearchWithCcsCore(IMSIonProperty property, double massTolerance, double ccsTolerance) {
            return SearcherWithRtCcs.Search(MSIonSearchQuery.CreateMassCcsQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz), property.CollisionCrossSection, ccsTolerance));
        }

        private IReadOnlyList<MoleculeMsReference> SearchWithRtCcsCore(IMSIonProperty property, double massTolerance, double rtTolerance, double ccsTolerance) {
            return SearcherWithRtCcs.Search(MSIonSearchQuery.CreateMassRtCcsQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz), property.ChromXs.RT.Value, rtTolerance, property.CollisionCrossSection, ccsTolerance));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(MsScanMatchResult result, IAnnotationQuery query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            ValidateCore(result, query.Property, DataAccess.GetNormalizedMSScanProperty(query.Scan, parameter), reference, parameter, omics);
        }

        private static void ValidateCore(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter, TargetOmics omics) {

            if (omics == TargetOmics.Lipidomics)
                ValidateOnLipidomics(result, property, scan, reference, parameter);
            else
                ValidateBase(result, property, reference, parameter);
        }

        //private static readonly double MsdialRtMatchThreshold = 0.5;
        //private static readonly double MsdialCcsMatchThreshold = 10d;
        private static void ValidateBase(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch;

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            var rtDiff = Math.Abs(property.ChromXs.RT.Value - reference.ChromXs.RT.Value);
            result.IsRtMatch = rtDiff <= parameter.RtTolerance;

            var ccsDiff = Math.Abs(property.CollisionCrossSection - reference.CollisionCrossSection);
            result.IsCcsMatch = ccsDiff <= parameter.CcsTolerance;
        }

        private static void ValidateOnLipidomics(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter) {

            ValidateBase(result, property, reference, parameter);

            MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, parameter.Ms2Tolerance, out var isLipidClassMatch, out var isLipidChainsMatch, out var isLipidPositionMatch, out var isOtherLipidMatch);
            result.IsLipidChainsMatch = isLipidChainsMatch;
            result.IsLipidClassMatch = isLipidClassMatch;
            result.IsLipidPositionMatch = isLipidPositionMatch;
            result.IsOtherLipidMatch = isOtherLipidMatch;
            result.IsSpectrumMatch &= isLipidChainsMatch | isLipidClassMatch | isLipidPositionMatch | isOtherLipidMatch;

            if (result.IsOtherLipidMatch)
                return;

            var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(reference);
            if (molecule == null || molecule.SublevelLipidName == null || molecule.LipidName == null) {
                result.Name = reference.Name; // for others and splash etc in compoundclass
            }
            else if (molecule.SublevelLipidName == molecule.LipidName) {
                result.Name = molecule.LipidName;
            }
            else {
                result.Name = $"{molecule.SublevelLipidName}|{molecule.LipidName}";
            }
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfySuggestedConditions(result, parameter)).ToList();
        }

        private static bool SatisfyRefMatchedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && result.IsSpectrumMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch)
                && (!parameter.IsUseCcsForAnnotationFiltering || result.IsCcsMatch);
        }

        private static bool SatisfySuggestedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch)
                && (!parameter.IsUseCcsForAnnotationFiltering || result.IsCcsMatch);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfyRefMatchedConditions(result, parameter)).ToList();
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfyRefMatchedConditions(result, parameter ?? Parameter);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return SatisfySuggestedConditions(result, parameter) && !SatisfyRefMatchedConditions(result, parameter);
        }
    }
}
