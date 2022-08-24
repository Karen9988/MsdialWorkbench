﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parser;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    [MessagePackObject]
    public class PeptideMsReference : IMSScanProperty, IIonProperty {
        [Key(0)]
        public Peptide Peptide { get; }
        [IgnoreMember]
        public Stream Fs { get; set; }
        [Key(1)]
        public long SeekPoint2MS { get; set; }

        public PeptideMsReference(Peptide peptide) {
            Peptide = peptide;
        }

        public PeptideMsReference(Peptide peptide, Stream fs, long seekPoint, AdductIon adduct, int id) {
            Peptide = peptide; Fs = fs; SeekPoint2MS = seekPoint; AdductType = adduct;
            PrecursorMz = adduct.ConvertToMz(peptide.ExactMass);
            ScanID = id;
        }

        [Key(2)]
        public int ScanID { get; set; }
        [IgnoreMember]
        public List<SpectrumPeak> Spectrum {
            get {
                if (cacheSpectrum is null) {
                    cacheSpectrum = ReadSpectrum(Fs, SeekPoint2MS);
                    return cacheSpectrum;
                }
                return cacheSpectrum; 
            }
            set => new NotSupportedException(); 
        }
        private List<SpectrumPeak> cacheSpectrum = null;
        private List<SpectrumPeak> ReadSpectrum(Stream fs, long seekPoint) {
            lock (fs) {
                return MsfPepFileParser.ReadSpectrumPeaks(fs, seekPoint);
            }
        }

        public void AddPeak(double mass, double intensity, string comment = null) {

        }

        [Key(4)]
        public ChromXs ChromXs { get; set; } = new ChromXs();
        [Key(5)]
        public IonMode IonMode { get; set; } = IonMode.Positive;
        [Key(6)]
        public double PrecursorMz { get; set; }

        [Key(7)]
        public AdductIon AdductType { get; set; }
        [Key(8)]
        public double CollisionCrossSection { get; set; }
        [Key(9)]
        public bool IsAnnotated { get; set; }
    }
}
