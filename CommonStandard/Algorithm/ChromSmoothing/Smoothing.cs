﻿using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Mathematics.Matrix;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.ChromSmoothing
{
    /// <summary>
    /// Now I'm preparing six smoothing methods but do not use LowessFilter and LowessFilter since I do not test them yet.
    /// These methods will return the list of ChromatogramPeak, i.e. chromatogram information. 
    /// Each ChromatogramPeak includes peak information as [0]scan number [1]retention time [2]m/z [3]intensity.
    /// The first argument of all smoothing methods should be raw chromatogram (list of ChromatogramPeak as described above.).
    /// The second argument of all smoothing methods is the number of data points which are used for the smoothing.
    /// </summary>
    public sealed class Smoothing {
        private readonly List<double> intensitiesBuffer = new List<double>();

        public Smoothing() {

        }

        public ValuePeak[] LinearWeightedMovingAverageXXX(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel) {
            var peaklist_ = peaklist as ValuePeak[] ?? peaklist.ToArray();
            var n = peaklist_.Length;
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            lock (intensitiesBuffer) {
                var size = n + smoothingLevel * 2 + 2;
                if (intensitiesBuffer.Count < size) {
                    intensitiesBuffer.AddRange(new double[size - intensitiesBuffer.Count]);
                }
                var intensities = intensitiesBuffer;
                for (int i = 0; i < size; i++) {
                    intensities[i] = 0d;
                    if (i < peaklist_.Length) {
                        intensities[i] += peaklist_[i].Intensity;
                    }
                    if (i - smoothingLevel - 1 >= 0 && i - smoothingLevel - 1 < peaklist_.Length) {
                        intensities[i] -= peaklist_[i - smoothingLevel - 1].Intensity * 2;
                    }
                    if (i - smoothingLevel * 2 - 2 >= 0) {
                        intensities[i] += peaklist_[i - smoothingLevel * 2 - 2].Intensity;
                    }
                }

                for (int i = 1; i < size; i++) {
                    intensities[i] += intensities[i - 1];
                }

                for (int i = 1; i < size; i++) {
                    intensities[i] += intensities[i - 1];
                }

                for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                    intensities[i + smoothingLevel] += peaklist_[i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
                }

                for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                    intensities[n - 1 - i + smoothingLevel] += peaklist_[n - 1 - i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
                }

                var smoothedPeaklist = new ValuePeak[n];
                for (int i = 0; i < peaklist_.Length; i++) {
                    smoothedPeaklist[i] = new ValuePeak(peaklist_[i].Id, i, i, peaklist_[i].Time, peaklist_[i].Mz, intensities[i + smoothingLevel] / normalizationValue);
                }
                return smoothedPeaklist;
            }
        }

        public static ValuePeak[] LinearWeightedMovingAverageZZZ(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel) {
            var peaklist_ = peaklist as ValuePeak[] ?? peaklist.ToArray();
            var n = peaklist_.Length;
            var intensities = new double[n + smoothingLevel * 2 + 2];
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            for (int i = 0; i < peaklist_.Length; i++) {
                intensities[i] += peaklist_[i].Intensity;
                intensities[i + smoothingLevel + 1] -= peaklist_[i].Intensity * 2;
                intensities[i + smoothingLevel * 2 + 2] += peaklist_[i].Intensity;
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist_[i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist_[n - 1 - i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            var smoothedPeaklist = new ValuePeak[n];
            for (int i = 0; i < peaklist_.Length; i++) {
                smoothedPeaklist[i] = new ValuePeak(peaklist_[i].Id, i, i, peaklist_[i].Time, peaklist_[i].Mz, intensities[i + smoothingLevel] / normalizationValue);
            }

            return smoothedPeaklist;
        }

        // imos method
        public static List<ChromatogramPeak> LinearWeightedMovingAverage(IReadOnlyList<IChromatogramPeak> peaklist, int smoothingLevel) {
            var peaklist_ = peaklist as IChromatogramPeak[] ?? peaklist.ToArray();
            var n = peaklist_.Length;
            var intensities = new double[n + smoothingLevel * 2 + 2];
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            for (int i = 0; i < peaklist_.Length; i++) {
                intensities[i] += peaklist_[i].Intensity;
                intensities[i + smoothingLevel + 1] -= peaklist_[i].Intensity * 2;
                intensities[i + smoothingLevel * 2 + 2] += peaklist_[i].Intensity;
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist_[i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist_[n - 1 - i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            var smoothedPeaklist = new List<ChromatogramPeak>(n);
            for (int i = 0; i < peaklist_.Length; i++) {
                smoothedPeaklist.Add(new ChromatogramPeak(i, peaklist_[i].Mass, intensities[i + smoothingLevel] / normalizationValue, peaklist_[i].ChromXs));
            }

            return smoothedPeaklist;
        }

        public static List<SpectrumPeak> LinearWeightedMovingAverage(IReadOnlyList<ISpectrumPeak> peaklist, int smoothingLevel) {
            var n = peaklist.Count;
            var intensities = new double[n + smoothingLevel * 2 + 2];
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            for (int i = 0; i < n; i++) {
                intensities[i] += peaklist[i].Intensity;
                intensities[i + smoothingLevel + 1] -= peaklist[i].Intensity * 2;
                intensities[i + smoothingLevel * 2 + 2] += peaklist[i].Intensity;
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist[i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist[n - 1 - i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            var smoothedPeaklist = new List<SpectrumPeak>(n);
            for (int i = 0; i < n; i++) {
                smoothedPeaklist.Add(new SpectrumPeak {
                    Mass = (float)peaklist[i].Mass,
                    Intensity = (float)intensities[i + smoothingLevel] / normalizationValue
                });
            }

            return smoothedPeaklist;
        }

        public static List<ValuePeak> LinearWeightedMovingAverage(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel) {
            var peaklist_ = peaklist as ValuePeak[] ?? peaklist.ToArray();
            var n = peaklist_.Length;
            var intensities = new double[n + smoothingLevel * 2 + 2];
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            for (int i = 0; i < peaklist_.Length; i++) {
                intensities[i] += peaklist_[i].Intensity;
                intensities[i + smoothingLevel + 1] -= peaklist_[i].Intensity * 2;
                intensities[i + smoothingLevel * 2 + 2] += peaklist_[i].Intensity;
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist_[i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist_[n - 1 - i].Intensity * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            var smoothedPeaklist = new List<ValuePeak>(n);
            for (int i = 0; i < peaklist_.Length; i++) {
                smoothedPeaklist.Add(new ValuePeak(peaklist_[i].Id, i, i, peaklist_[i].Time, peaklist_[i].Mz, intensities[i + smoothingLevel] / normalizationValue));
            }

            return smoothedPeaklist;
        }

        public static List<double[]> LinearWeightedMovingAverage(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            var peaklist_ = peaklist as double[][] ?? peaklist.ToArray();
            var n = peaklist_.Length;
            var intensities = new double[n + smoothingLevel * 2 + 2];
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            for (int i = 0; i < peaklist_.Length; i++) {
                intensities[i] += peaklist_[i][3];
                intensities[i + smoothingLevel + 1] -= peaklist_[i][3] * 2;
                intensities[i + smoothingLevel * 2 + 2] += peaklist_[i][3];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist_[i][3] * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist_[n - 1 - i][3] * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            var smoothedPeaklist = new List<double[]>(n);
            for (int i = 0; i < peaklist_.Length; i++) {
                smoothedPeaklist.Add(new double[] { i, peaklist_[i][1], peaklist_[i][2], intensities[i + smoothingLevel] / normalizationValue });
            }

            return smoothedPeaklist;
        }

        public static List<double[]> LinearWeightedMovingAverageOnSpectrum(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            var n = peaklist.Count;
            var intensities = new double[n + smoothingLevel * 2 + 2];
            int normalizationValue = (smoothingLevel + 1) * (smoothingLevel + 1);

            for (int i = 0; i < n; i++) {
                intensities[i] += peaklist[i][1];
                intensities[i + smoothingLevel + 1] -= peaklist[i][1] * 2;
                intensities[i + smoothingLevel * 2 + 2] += peaklist[i][1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist[i][1] * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist[n - 1 - i][1] * ((smoothingLevel - i + 1) * (smoothingLevel - i) / 2);
            }

            var smoothedPeaklist = new List<double[]>(n);
            for (int i = 0; i < n; i++) {
                smoothedPeaklist.Add(new double[] {
                    (float)peaklist[i][0],
                    (float)intensities[i + smoothingLevel] / normalizationValue
                });
            }

            return smoothedPeaklist;
        }



        // imos method
        public static List<ChromatogramPeak> SimpleMovingAverage(IReadOnlyList<IChromatogramPeak> peaklist, int smoothingLevel)
        {
            var n = peaklist.Count;
            var intensities = new double[n + smoothingLevel * 2 + 1];
            int normalizationValue = 2 * smoothingLevel + 1;

            for (int i = 0; i < peaklist.Count; i++) {
                intensities[i] += peaklist[i].Intensity;
                intensities[i + smoothingLevel * 2 + 1] -= peaklist[i].Intensity;
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist[i].Intensity * (smoothingLevel - i);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist[n - 1 - i].Intensity * (smoothingLevel - i);
            }

            var smoothedPeaklist = new List<ChromatogramPeak>(n);
            for (int i = 0; i < peaklist.Count; i++) {
                smoothedPeaklist.Add(new ChromatogramPeak(i, peaklist[i].Mass, intensities[i + smoothingLevel] / normalizationValue, peaklist[i].ChromXs));
            }

            return smoothedPeaklist;
        }

        public static List<ValuePeak> SimpleMovingAverage(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel) {
            var n = peaklist.Count;
            var intensities = new double[n + smoothingLevel * 2 + 1];
            int normalizationValue = 2 * smoothingLevel + 1;

            for (int i = 0; i < peaklist.Count; i++) {
                intensities[i] += peaklist[i].Intensity;
                intensities[i + smoothingLevel * 2 + 1] -= peaklist[i].Intensity;
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist[i].Intensity * (smoothingLevel - i);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist[n - 1 - i].Intensity * (smoothingLevel - i);
            }

            var smoothedPeaklist = new List<ValuePeak>(n);
            for (int i = 0; i < peaklist.Count; i++) {
                smoothedPeaklist.Add(new ValuePeak(peaklist[i].Id, i, i, peaklist[i].Time, peaklist[i].Mz, intensities[i + smoothingLevel] / normalizationValue));
            }

            return smoothedPeaklist;
        }

        public static List<double[]> SimpleMovingAverage(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            var n = peaklist.Count;
            var intensities = new double[n + smoothingLevel * 2 + 1];
            int normalizationValue = 2 * smoothingLevel + 1;

            for (int i = 0; i < peaklist.Count; i++) {
                intensities[i] += peaklist[i][3];
                intensities[i + smoothingLevel * 2 + 1] -= peaklist[i][3];
            }

            for (int i = 1; i < intensities.Length; i++) {
                intensities[i] += intensities[i - 1];
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[i + smoothingLevel] += peaklist[i][3] * (smoothingLevel - i);
            }

            for (int i = 0; i < Math.Min(smoothingLevel, n); i++) {
                intensities[n - 1 - i + smoothingLevel] += peaklist[n - 1 - i][3] * (smoothingLevel - i);
            }

            var smoothedPeaklist = new List<double[]>(n);
            for (int i = 0; i < peaklist.Count; i++) {
                smoothedPeaklist.Add(new double[] { i, peaklist[i][1], peaklist[i][2], intensities[i + smoothingLevel] / normalizationValue });
            }

            return smoothedPeaklist;
        }

        public static List<ChromatogramPeak> SavitxkyGolayFilter(IReadOnlyList<IChromatogramPeak> peaklist, int smoothingLevel)
        {
            double[,] hatMatrix;
            double[,] vandermondeMatrix = new double[2 * smoothingLevel + 1, 4];
            double[] xvector = new double[2 * smoothingLevel + 1];
            double[] coefficientVector = new double[2 * smoothingLevel + 1];

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                xvector[i] = (-1) * smoothingLevel + i;

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                for (int j = 0; j < 4; j++)
                    vandermondeMatrix[i, j] = Math.Pow(xvector[i], j);

            var luMatrix = MatrixCalculate.MatrixDecompose(MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixTranspose(vandermondeMatrix), vandermondeMatrix));

            hatMatrix = MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixProduct(vandermondeMatrix, MatrixCalculate.MatrixInverse(luMatrix)), MatrixCalculate.MatrixTranspose(vandermondeMatrix));

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = hatMatrix[smoothingLevel, i];

            var smoothedPeaklist = new List<ChromatogramPeak>();
            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                var sum = 0.0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i].Intensity;
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new ChromatogramPeak(i, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs); 
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<double[]> SavitxkyGolayFilter(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            double[,] hatMatrix;
            double[,] vandermondeMatrix = new double[2 * smoothingLevel + 1, 4];
            double[] xvector = new double[2 * smoothingLevel + 1];
            double[] coefficientVector = new double[2 * smoothingLevel + 1];

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                xvector[i] = (-1) * smoothingLevel + i;

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                for (int j = 0; j < 4; j++)
                    vandermondeMatrix[i, j] = Math.Pow(xvector[i], j);

            var luMatrix = MatrixCalculate.MatrixDecompose(MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixTranspose(vandermondeMatrix), vandermondeMatrix));

            hatMatrix = MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixProduct(vandermondeMatrix, MatrixCalculate.MatrixInverse(luMatrix)), MatrixCalculate.MatrixTranspose(vandermondeMatrix));

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = hatMatrix[smoothingLevel, i];

            var smoothedPeaklist = new List<double[]>();
            for (int i = 0; i < peaklist.Count; i++) {
                var smoothedPeakIntensity = 0.0;
                var sum = 0.0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++) {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i][3];
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j][3];
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<ValuePeak> SavitxkyGolayFilter(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel)
        {
            double[,] hatMatrix;
            double[,] vandermondeMatrix = new double[2 * smoothingLevel + 1, 4];
            double[] xvector = new double[2 * smoothingLevel + 1];
            double[] coefficientVector = new double[2 * smoothingLevel + 1];

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                xvector[i] = (-1) * smoothingLevel + i;

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                for (int j = 0; j < 4; j++)
                    vandermondeMatrix[i, j] = Math.Pow(xvector[i], j);

            var luMatrix = MatrixCalculate.MatrixDecompose(MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixTranspose(vandermondeMatrix), vandermondeMatrix));

            hatMatrix = MatrixCalculate.MatrixProduct(MatrixCalculate.MatrixProduct(vandermondeMatrix, MatrixCalculate.MatrixInverse(luMatrix)), MatrixCalculate.MatrixTranspose(vandermondeMatrix));

            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = hatMatrix[smoothingLevel, i];

            var smoothedPeaklist = new List<ValuePeak>();
            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                var sum = 0.0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i].Intensity;
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new ValuePeak(peaklist[i].Id, i, i, peaklist[i].Time, peaklist[i].Mz, smoothedPeakIntensity); 
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<ChromatogramPeak> BinomialFilter(IReadOnlyList<IChromatogramPeak> peaklist, int smoothingLevel)
        {
            double[] coefficientVector = new double[2 * smoothingLevel + 1];
            double sum = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                coefficientVector[i] = BasicMathematics.BinomialCoefficient(2 * smoothingLevel, i);
                sum += coefficientVector[i];
            }
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = coefficientVector[i] / sum;

            var smoothedPeaklist = new List<ChromatogramPeak>();

            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i].Intensity;
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new ChromatogramPeak(i, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs); 
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }
        
        public static List<double[]> BinomialFilter(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            double[] coefficientVector = new double[2 * smoothingLevel + 1];
            double sum = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++) {
                coefficientVector[i] = BasicMathematics.BinomialCoefficient(2 * smoothingLevel, i);
                sum += coefficientVector[i];
            }
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = coefficientVector[i] / sum;

            var smoothedPeaklist = new List<double[]>();

            for (int i = 0; i < peaklist.Count; i++) {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++) {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i][3];
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j][3];
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }

        public static List<ValuePeak> BinomialFilter(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel)
        {
            double[] coefficientVector = new double[2 * smoothingLevel + 1];
            double sum = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                coefficientVector[i] = BasicMathematics.BinomialCoefficient(2 * smoothingLevel, i);
                sum += coefficientVector[i];
            }
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
                coefficientVector[i] = coefficientVector[i] / sum;

            var smoothedPeaklist = new List<ValuePeak>();

            for (int i = 0; i < peaklist.Count; i++)
            {
                var smoothedPeakIntensity = 0.0;
                sum = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1) sum += coefficientVector[j + smoothingLevel] * peaklist[i].Intensity;
                    else sum += coefficientVector[j + smoothingLevel] * peaklist[i + j].Intensity;
                }
                smoothedPeakIntensity = sum;
                var smoothedPeak = new ValuePeak(peaklist[i].Id, i, i, peaklist[i].Time, peaklist[i].Mz, smoothedPeakIntensity); 
                smoothedPeaklist.Add(smoothedPeak);
            }
            return smoothedPeaklist;
        }
        
        public static List<ChromatogramPeak> LowessFilter(IReadOnlyList<IChromatogramPeak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<ChromatogramPeak>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a = 0, b = 0, c = 0, d = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                b += (i - smoothingLevel) * coefficient[i];
                c = b;
                d += coefficient[i];
            }
            double[,] inverseMatrix = new double[2, 2];
            double detA = a * d - b * c;
            inverseMatrix[0, 0] = d / detA;
            inverseMatrix[0, 1] = (-1) * b / detA;
            inverseMatrix[1, 0] = (-1) * c / detA;
            inverseMatrix[1, 1] = a / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = 0;
                a = b = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i].Intensity * j * coefficient[smoothingLevel + j];
                        b += peaklist[i].Intensity * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j].Intensity * j * coefficient[smoothingLevel + j];
                        b += peaklist[i + j].Intensity * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b;
                smoothedPeakIntensity = coefficientB;

                var smoothedPeak = new ChromatogramPeak(i, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs); 
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }

        public static List<double[]> LowessFilter(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            var smoothedPeaklist = new List<double[]>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++) {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a = 0, b = 0, c = 0, d = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++) {
                a += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                b += (i - smoothingLevel) * coefficient[i];
                c = b;
                d += coefficient[i];
            }
            double[,] inverseMatrix = new double[2, 2];
            double detA = a * d - b * c;
            inverseMatrix[0, 0] = d / detA;
            inverseMatrix[0, 1] = (-1) * b / detA;
            inverseMatrix[1, 0] = (-1) * c / detA;
            inverseMatrix[1, 1] = a / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB;
            for (int i = 0; i < peaklist.Count; i++) {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = 0;
                a = b = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++) {
                    if (i + j < 0 || i + j > peaklist.Count - 1) {
                        a += peaklist[i][3] * j * coefficient[smoothingLevel + j];
                        b += peaklist[i][3] * coefficient[smoothingLevel + j];
                    }
                    else {
                        a += peaklist[i + j][3] * j * coefficient[smoothingLevel + j];
                        b += peaklist[i + j][3] * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b;
                smoothedPeakIntensity = coefficientB;

                var smoothedPeak = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }

        public static List<ValuePeak> LowessFilter(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<ValuePeak>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a = 0, b = 0, c = 0, d = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                b += (i - smoothingLevel) * coefficient[i];
                c = b;
                d += coefficient[i];
            }
            double[,] inverseMatrix = new double[2, 2];
            double detA = a * d - b * c;
            inverseMatrix[0, 0] = d / detA;
            inverseMatrix[0, 1] = (-1) * b / detA;
            inverseMatrix[1, 0] = (-1) * c / detA;
            inverseMatrix[1, 1] = a / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = 0;
                a = b = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i].Intensity * j * coefficient[smoothingLevel + j];
                        b += peaklist[i].Intensity * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j].Intensity * j * coefficient[smoothingLevel + j];
                        b += peaklist[i + j].Intensity * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b;
                smoothedPeakIntensity = coefficientB;

                var smoothedPeak = new ValuePeak(peaklist[i].Id, i, i, peaklist[i].Time, peaklist[i].Mz, smoothedPeakIntensity); 
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }

        public static List<ChromatogramPeak> LoessFilter(IReadOnlyList<IChromatogramPeak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<ChromatogramPeak>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a11 = 0, a12 = 0, a13 = 0, a21 = 0, a22 = 0, a23 = 0, a31 = 0, a32 = 0, a33 = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a11 += Math.Pow(i - smoothingLevel, 4) * coefficient[i];
                a12 += Math.Pow(i - smoothingLevel, 3) * coefficient[i];
                a13 += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                a21 = a12;
                a22 = a13;
                a23 += (i - smoothingLevel) * coefficient[i];
                a31 = a13;
                a32 = a23;
                a33 += coefficient[i];
            }
            double[,] inverseMatrix = new double[3, 3];
            double detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;
            inverseMatrix[0, 0] = (a22 * a33 - a23 * a32) / detA;
            inverseMatrix[0, 1] = (a13 * a32 - a12 * a33) / detA;
            inverseMatrix[0, 2] = (a12 * a23 - a13 * a22) / detA;
            inverseMatrix[1, 0] = (a23 * a31 - a21 * a33) / detA;
            inverseMatrix[1, 1] = (a11 * a33 - a13 * a31) / detA;
            inverseMatrix[1, 2] = (a13 * a21 - a11 * a23) / detA;
            inverseMatrix[2, 0] = (a21 * a32 - a22 * a31) / detA;
            inverseMatrix[2, 1] = (a12 * a31 - a11 * a32) / detA;
            inverseMatrix[2, 2] = (a11 * a22 - a12 * a21) / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB, coefficientC, a, b, c;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = coefficientC = 0;
                a = b = c = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i].Intensity * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i].Intensity * j * coefficient[smoothingLevel + j];
                        c += peaklist[i].Intensity * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j].Intensity * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i + j].Intensity * j * coefficient[smoothingLevel + j];
                        c += peaklist[i + j].Intensity * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b + inverseMatrix[0, 2] * c;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b + inverseMatrix[1, 2] * c;
                coefficientC = inverseMatrix[2, 0] * a + inverseMatrix[2, 1] * b + inverseMatrix[2, 2] * c;

                smoothedPeakIntensity = coefficientC;
                var smoothedPeak = new ChromatogramPeak(i, peaklist[i].Mass, smoothedPeakIntensity, peaklist[i].ChromXs); 
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }

        public static List<double[]> LoessFilter(IReadOnlyList<double[]> peaklist, int smoothingLevel) {
            var smoothedPeaklist = new List<double[]>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++) {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a11 = 0, a12 = 0, a13 = 0, a21 = 0, a22 = 0, a23 = 0, a31 = 0, a32 = 0, a33 = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++) {
                a11 += Math.Pow(i - smoothingLevel, 4) * coefficient[i];
                a12 += Math.Pow(i - smoothingLevel, 3) * coefficient[i];
                a13 += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                a21 = a12;
                a22 = a13;
                a23 += (i - smoothingLevel) * coefficient[i];
                a31 = a13;
                a32 = a23;
                a33 += coefficient[i];
            }
            double[,] inverseMatrix = new double[3, 3];
            double detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;
            inverseMatrix[0, 0] = (a22 * a33 - a23 * a32) / detA;
            inverseMatrix[0, 1] = (a13 * a32 - a12 * a33) / detA;
            inverseMatrix[0, 2] = (a12 * a23 - a13 * a22) / detA;
            inverseMatrix[1, 0] = (a23 * a31 - a21 * a33) / detA;
            inverseMatrix[1, 1] = (a11 * a33 - a13 * a31) / detA;
            inverseMatrix[1, 2] = (a13 * a21 - a11 * a23) / detA;
            inverseMatrix[2, 0] = (a21 * a32 - a22 * a31) / detA;
            inverseMatrix[2, 1] = (a12 * a31 - a11 * a32) / detA;
            inverseMatrix[2, 2] = (a11 * a22 - a12 * a21) / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB, coefficientC, a, b, c;
            for (int i = 0; i < peaklist.Count; i++) {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = coefficientC = 0;
                a = b = c = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++) {
                    if (i + j < 0 || i + j > peaklist.Count - 1) {
                        a += peaklist[i][3] * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i][3] * j * coefficient[smoothingLevel + j];
                        c += peaklist[i][3] * coefficient[smoothingLevel + j];
                    }
                    else {
                        a += peaklist[i + j][3] * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i + j][3] * j * coefficient[smoothingLevel + j];
                        c += peaklist[i + j][3] * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b + inverseMatrix[0, 2] * c;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b + inverseMatrix[1, 2] * c;
                coefficientC = inverseMatrix[2, 0] * a + inverseMatrix[2, 1] * b + inverseMatrix[2, 2] * c;

                smoothedPeakIntensity = coefficientC;
                var smoothedPeak = new double[] { i, peaklist[i][1], peaklist[i][2], smoothedPeakIntensity };
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }

        public static List<ValuePeak> LoessFilter(IReadOnlyList<ValuePeak> peaklist, int smoothingLevel)
        {
            var smoothedPeaklist = new List<ValuePeak>();

            //Loess coefficient
            double[] coefficient = new double[2 * smoothingLevel + 1];
            for (int i = 0; i < smoothingLevel; i++)
            {
                coefficient[i] = Math.Pow(1 - Math.Pow(Math.Abs((i - smoothingLevel)) / smoothingLevel, 3), 3);
                coefficient[2 * smoothingLevel - i] = coefficient[i];
            }
            coefficient[smoothingLevel] = 1;

            //inverse matrix calculation
            double a11 = 0, a12 = 0, a13 = 0, a21 = 0, a22 = 0, a23 = 0, a31 = 0, a32 = 0, a33 = 0;
            for (int i = 0; i < 2 * smoothingLevel + 1; i++)
            {
                a11 += Math.Pow(i - smoothingLevel, 4) * coefficient[i];
                a12 += Math.Pow(i - smoothingLevel, 3) * coefficient[i];
                a13 += Math.Pow(i - smoothingLevel, 2) * coefficient[i];
                a21 = a12;
                a22 = a13;
                a23 += (i - smoothingLevel) * coefficient[i];
                a31 = a13;
                a32 = a23;
                a33 += coefficient[i];
            }
            double[,] inverseMatrix = new double[3, 3];
            double detA = a11 * a22 * a33 + a21 * a32 * a13 + a31 * a12 * a23 - a11 * a32 * a23 - a31 * a22 * a13 - a21 * a12 * a33;
            inverseMatrix[0, 0] = (a22 * a33 - a23 * a32) / detA;
            inverseMatrix[0, 1] = (a13 * a32 - a12 * a33) / detA;
            inverseMatrix[0, 2] = (a12 * a23 - a13 * a22) / detA;
            inverseMatrix[1, 0] = (a23 * a31 - a21 * a33) / detA;
            inverseMatrix[1, 1] = (a11 * a33 - a13 * a31) / detA;
            inverseMatrix[1, 2] = (a13 * a21 - a11 * a23) / detA;
            inverseMatrix[2, 0] = (a21 * a32 - a22 * a31) / detA;
            inverseMatrix[2, 1] = (a12 * a31 - a11 * a32) / detA;
            inverseMatrix[2, 2] = (a11 * a22 - a12 * a21) / detA;

            //smoothing
            double smoothedPeakIntensity;
            double coefficientA, coefficientB, coefficientC, a, b, c;
            for (int i = 0; i < peaklist.Count; i++)
            {
                smoothedPeakIntensity = 0;
                coefficientA = coefficientB = coefficientC = 0;
                a = b = c = 0;

                for (int j = -smoothingLevel; j <= smoothingLevel; j++)
                {
                    if (i + j < 0 || i + j > peaklist.Count - 1)
                    {
                        a += peaklist[i].Intensity * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i].Intensity * j * coefficient[smoothingLevel + j];
                        c += peaklist[i].Intensity * coefficient[smoothingLevel + j];
                    }
                    else
                    {
                        a += peaklist[i + j].Intensity * Math.Pow(j, 2) * coefficient[smoothingLevel + j];
                        b += peaklist[i + j].Intensity * j * coefficient[smoothingLevel + j];
                        c += peaklist[i + j].Intensity * coefficient[smoothingLevel + j];
                    }
                }

                coefficientA = inverseMatrix[0, 0] * a + inverseMatrix[0, 1] * b + inverseMatrix[0, 2] * c;
                coefficientB = inverseMatrix[1, 0] * a + inverseMatrix[1, 1] * b + inverseMatrix[1, 2] * c;
                coefficientC = inverseMatrix[2, 0] * a + inverseMatrix[2, 1] * b + inverseMatrix[2, 2] * c;

                smoothedPeakIntensity = coefficientC;
                var smoothedPeak = new ValuePeak(peaklist[i].Id, i, i, peaklist[i].Time, peaklist[i].Mz, smoothedPeakIntensity); 
                smoothedPeaklist.Add(smoothedPeak);
            }

            return smoothedPeaklist;
        }
    }
}
