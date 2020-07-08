﻿using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;

namespace CompMs.MsdialCore.Algorithm
{
    public class GapFiller
    {
        public static void GapFilling(
            List<RawSpectrum> spectrumCollection, 
            float centralMz, float mzTol, float mzPeakWidth,
            IonMode ionMode, SmoothingMethod smoothingMethod, int smoothingLevel,
            bool isForceInsert, AlignmentChromPeakFeature alignmentChromPeakFeature
            ) {

            mzTol = Math.Max(mzTol, 0.005f);
            mzPeakWidth = Math.Max(mzPeakWidth, 0.2f);

            var result = alignmentChromPeakFeature;

            var peaklist = DataAccess.GetMs1Peaklist(spectrumCollection, centralMz, mzPeakWidth * 1.5f, ionMode);
            var sPeaklist = DataAccess.GetSmoothedPeaklist(peaklist, smoothingMethod, smoothingLevel);

            result.PeakID = -2;

            if (sPeaklist == null || sPeaklist.Count == 0) return;

            var candidates = new List<ChromatogramPeak>();
            var minId = -1;
            var minDiff = double.MaxValue;

            var start = SearchCollection.LowerBound(sPeaklist, new ChromatogramPeak { Mass = centralMz - mzTol }, (a, b) => a.Mass.CompareTo(b.Mass));
            for (int i = start; i < sPeaklist.Count; i++) {
                if (sPeaklist[i].Mass < centralMz - mzTol) continue;
                if (centralMz + mzTol < sPeaklist[i].Mass) break;

                if (   sPeaklist[i-2].Intensity <= sPeaklist[i-1].Intensity && sPeaklist[i-1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i+1].Intensity
                    || sPeaklist[i-1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i+1].Intensity && sPeaklist[i+1].Intensity >= sPeaklist[i+2].Intensity) {
                    candidates.Add(sPeaklist[i]);
                }

                var diff = Math.Abs(sPeaklist[i].Mass - centralMz);
                if (diff < minDiff) {
                    minDiff = diff;
                    minId = i;
                }
            }

            if (minId == -1) minId = sPeaklist.Count / 2;

            int id, leftId, rightId;

            if (candidates.Count == 0) {
                if (!isForceInsert) return;
                var range = 5;

                id = minId;
                leftId = id - 1;
                rightId = id + 1;

                var limit = Math.Max(id - range, 0);
                while (limit < leftId) {
                    if (sPeaklist[leftId - 1].Intensity > sPeaklist[leftId].Intensity) break;
                    --leftId;
                }
                limit = Math.Min(id + range, sPeaklist.Count - 1);
                while (rightId < limit) {
                    if (sPeaklist[rightId].Intensity < sPeaklist[rightId + 1].Intensity) break;
                    ++rightId;
                }
            }
            else {
                var min = candidates.Min(cand => (Math.Abs(cand.Mass = centralMz), cand.ID));
                id = min.ID;

                var margin = 2;

                leftId = Math.Max(id - margin, 0);
                while (0 < leftId) {
                    if (sPeaklist[leftId - 1].Intensity >= sPeaklist[leftId].Intensity) break;
                    --leftId;
                }

                rightId = Math.Min(id + margin, sPeaklist.Count - 1);
                while (rightId < sPeaklist.Count - 1) {
                    if (sPeaklist[rightId].Intensity <= sPeaklist[rightId + 1].Intensity) break;
                    ++rightId;
                }

                if (!isForceInsert && (id - leftId < 2 || rightId - id < 2)) return;

                for(int i = leftId + 1; i <= rightId - 1; i++) {
                    if (sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i + 1].Intensity
                        || sPeaklist[i - 1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity) {
                        if (sPeaklist[id].Intensity < sPeaklist[i].Intensity) {
                            id = i;
                        }
                    }
                }

            }

            double peakAreaAboveZero = 0d;
            for (int i = leftId; i < rightId; i++)
                peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) / 2 * (sPeaklist[i + 1].Mass - sPeaklist[i].Mass);

            result.Mass = sPeaklist[id].Mass;
            result.ChromScanIdTop = id;
            result.ChromScanIdLeft = leftId;
            result.ChromScanIdRight = rightId;
            result.PeakHeightTop = sPeaklist[id].Intensity;
            result.PeakHeightLeft = sPeaklist[leftId].Intensity;
            result.PeakHeightRight = sPeaklist[rightId].Intensity;
            result.PeakAreaAboveZero = peakAreaAboveZero;

            return;

        }

        public void GapFilling(
            List<RawSpectrum> spectrumCollection,
            AlignmentSpotProperty alignmentSpotProperty,
            AlignmentChromPeakFeature alignmentChromPeakFeature,
            float centralMz, float mzTol,
            float averagePeakWidth,
            ParameterBase param
            ) {
            GapFilling(
                spectrumCollection, alignmentSpotProperty, alignmentChromPeakFeature,
                centralMz, mzTol, averagePeakWidth,
                param.IonMode, param.SmoothingMethod, param.SmoothingLevel, param.IsForceInsertForGapFilling);
        }

        public void GapFilling(
            List<RawSpectrum> spectrumCollection,
            AlignmentSpotProperty alignmentSpotProperty,
            AlignmentChromPeakFeature alignmentChromPeakFeature,
            float centralMz, float mzTol,
            float averagePeakWidth,
            IonMode ionMode, SmoothingMethod smoothingMethod, int smoothingLevel,
            bool isForceInsert
            ) {

            GapFilling(spectrumCollection, centralMz, mzTol, averagePeakWidth, ionMode, smoothingMethod, smoothingLevel, isForceInsert, alignmentChromPeakFeature);

            alignmentChromPeakFeature.PeakShape.EstimatedNoise = Math.Max(alignmentSpotProperty.EstimatedNoiseAve, 1.0f);
            var peakHeight = alignmentChromPeakFeature.PeakHeightTop - Math.Min(alignmentChromPeakFeature.PeakHeightLeft, alignmentChromPeakFeature.PeakHeightRight);
            alignmentChromPeakFeature.PeakShape.SignalToNoise = (float)peakHeight / alignmentSpotProperty.EstimatedNoiseAve;
        }

        public static AlignmentChromPeakFeature GapFilling(
            List<RawSpectrum> spectrumCollection,
            ChromXs center,
            float mzTol, float mzPeakWidth, IonMode ionMode,
            SmoothingMethod smoothingMethod, int smoothingLevel,
            bool isForceInsert
            ) {
            var result = new AlignmentChromPeakFeature();

            GapFilling(spectrumCollection, (float)center.Mz.Value, mzTol, mzPeakWidth, ionMode, smoothingMethod, smoothingLevel, isForceInsert, result);

            return result;
        }
    }
}