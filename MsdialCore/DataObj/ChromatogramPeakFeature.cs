﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ChromatogramPeakFeature: IChromatogramPeakFeature, IMoleculeMsProperty {

        // basic property of IChromatogramPeakFeature
        [Key(0)]
        public int ChromScanIdLeft { get; set; }
        [Key(1)]
        public int ChromScanIdTop { get; set; }
        [Key(2)]
        public int ChromScanIdRight { get; set; }
        [Key(3)]
        public ChromXs ChromXsLeft { get; set; }
        [Key(4)]
        public ChromXs ChromXsTop { get; set; }
        [Key(5)]
        public ChromXs ChromXsRight { get; set; }
        [Key(6)]
        public double PeakHeightLeft { get; set; }
        [Key(7)]
        public double PeakHeightTop { get; set; }
        [Key(8)]
        public double PeakHeightRight { get; set; }
        [Key(9)]
        public double PeakAreaAboveZero { get; set; }
        [Key(10)]
        public double PeakAreaAboveBaseline { get; set; }

        [Key(43)]
        public double Mass { get; set; }

        public double PeakWidth(ChromXType type) {
            switch (type) {
                case ChromXType.RT: return ChromXsRight.RT.Value - ChromXsLeft.RT.Value;
                case ChromXType.RI: return ChromXsRight.RI.Value - ChromXsLeft.RI.Value;
                case ChromXType.Drift: return ChromXsRight.Drift.Value - ChromXsLeft.Drift.Value;
                default: return ChromXsRight.Value - ChromXsLeft.Value;
            }
        }

        public double PeakWidth() {
            return ChromXsRight.Value - ChromXsLeft.Value;
        }
        // basic ID metadata
        [Key(11)]
        public int MasterPeakID { get; set; } // sequential IDs parsing all peak features extracted from an MS data
        [Key(12)]
        public int PeakID { get; set; } // sequential IDs from the same dimmension e.g. RT vs MZ or IM vs MZ
        [Key(13)]
        public int ParentPeakID { get; set; } // for LC-IM-MS/MS. The parent peak ID generating the daughter peak IDs
        [Key(15)]
        public long SeekPointToDCLFile { get; set; } // deconvoluted spectrum is stored in dcl file, and this is the seek pointer

        public int GetMSDecResultID() {
            if (IsMultiLayeredData()) return MasterPeakID;
            return PeakID;
        }

        // link to raw data
        [Key(16)]
        public int MS1RawSpectrumIdTop { get; set; }
        [Key(44)]
        public int MS1RawSpectrumIdLeft { get; set; }
        [Key(45)]
        public int MS1RawSpectrumIdRight { get; set; }
        [Key(17)]
        public int MS1AccumulatedMs1RawSpectrumIdTop { get; set; } // used for LC-IM-MS/MS
        [Key(46)]
        public int MS1AccumulatedMs1RawSpectrumIdLeft { get; set; } // used for LC-IM-MS/MS
        [Key(47)]
        public int MS1AccumulatedMs1RawSpectrumIdRight { get; set; } // used for LC-IM-MS/MS
        [Key(18)]
        public int MS2RawSpectrumID { get; set; } // representative ID
        [Key(19)]
        public Dictionary<int, double> MS2RawSpectrumID2CE { get; set; } = new Dictionary<int, double>();

        // set for IMMScanProperty
        [Key(20)]
        public int ScanID { get; set; } // same as MS1RawSpectrumID
        [Key(21)]
        public double PrecursorMz { get; set; } // in LC-MS/MS same as Mass
        [Key(22)]
        public IonMode IonMode { get; set; }
        [Key(23)]
        public ChromXs ChromXs { get; set; } // same as ChromXsTop
        [Key(24)]
        public List<SpectrumPeak> Spectrum { get; set; } = new List<SpectrumPeak>();
        public void AddPeak(double mass, double intensity, string comment = null) {
            Spectrum.Add(new SpectrumPeak(mass, intensity, comment));
        }

        // set for IMoleculeProperty (for representative)
        [Key(25)]
        public string Name { get; set; } = string.Empty;
        [Key(26)]
        public Formula Formula { get; set; } = new Formula();
        [Key(27)]
        public string Ontology { get; set; } = string.Empty;
        [Key(28)]
        public string SMILES { get; set; } = string.Empty;
        [Key(29)]
        public string InChIKey { get; set; } = string.Empty;

        public bool IsValidInChIKey() {
            if (InChIKey == null || InChIKey == string.Empty || InChIKey.Length != 27) return false;
            return true;
        }


        // ion physiochemical information
        [Key(30)]
        public AdductIon AdductType { get; set; } // representative
        public void AddAdductType(AdductIon adductIon) {
            AdductType = adductIon;
            if (PeakCharacter == null) PeakCharacter = new IonFeatureCharacter();
            PeakCharacter.AdductType = adductIon;
            PeakCharacter.Charge = adductIon.ChargeNumber;
        }
        public bool IsAdductTypeFormatted() {
            if (AdductType == null || !AdductType.FormatCheck || AdductType.AdductIonName == null || AdductType.AdductIonName == string.Empty) return false;
            return true;
        }

        [Key(31)]
        public double CollisionCrossSection { get; set; }

        // molecule annotation results
        // IDs to link properties
        //[Key(32)]
        //public int MspID { get; set; } = -1; // representative msp id
        //[Key(48)]
        //public int MspIDWhenOrdered { get; set; } = -1; // representative msp id
        [Key(33)]
        public Dictionary<int, List<int>> MSRawID2MspIDs { get; set; } = new Dictionary<int, List<int>>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS). ID list having the metabolite candidates exceeding the threshold
        [Key(36)]
        public Dictionary<int, MsScanMatchResult> MSRawID2MspBasedMatchResult { get; set; } = new Dictionary<int, MsScanMatchResult>(); // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS).
       
        //[Key(34)]
        //public int TextDbID { get; set; }// representative text id
        //[Key(49)]
        //public int TextDbIDWhenOrdered { get; set; }// representative text id
        [Key(35)]
        public List<int> TextDbIDs { get; set; } = new List<int>(); // ID list having the metabolite candidates exceeding the threshold (optional)
        [Key(37)]
        public MsScanMatchResult TextDbBasedMatchResult { get; set; } = null;
        
        [IgnoreMember]
        public MsScanMatchResult MspBasedMatchResult { // get result having max score
            get {
                if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return null;
                else {
                    return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value)).Value;
                }
            }
        }

        public int TextDbID() {
            if (TextDbBasedMatchResult != null) return TextDbBasedMatchResult.LibraryID;
            else return -1;
        }

        public int TextDbIDWhenOrdered() {
            if (TextDbBasedMatchResult != null) return TextDbBasedMatchResult.LibraryIDWhenOrdered;
            else return -1;
        }

        public int MspID() {
            if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return -1;
            else {
                return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value.LibraryID)).LibraryID;
            }
        }

        public int MspIDWhenOrdered() {
            if (MSRawID2MspBasedMatchResult.IsEmptyOrNull()) return -1;
            else {
                return MSRawID2MspBasedMatchResult.Max(n => (n.Value.TotalScore, n.Value.LibraryIDWhenOrdered)).LibraryIDWhenOrdered;
            }
        }

        public bool IsReferenceMatched() {
            if (TextDbID() >= 0) return true;
            if (MspID() >= 0 && MSRawID2MspBasedMatchResult.Values.Count(n => n.IsSpectrumMatch) > 0) return true;
            return false;
        }

        public bool IsAnnotationSuggested() {
            if (MspID() >= 0 && MSRawID2MspBasedMatchResult.Values.Count(n => n.IsSpectrumMatch) == 0) return true;
            return false;
        }

        public bool IsUnknown() {
            if (MspID() < 0 && TextDbID() < 0) return true;
            return false;
        }

      
        [Key(38)]
        public string Comment { get; set; }

        // peak characters
        [Key(39)]
        public IonFeatureCharacter PeakCharacter { get; set; } = new IonFeatureCharacter();
        [Key(40)]
        public ChromatogramPeakShape PeakShape { get; set; }

        // others
        [Key(41)]
        public FeatureFilterStatus FeatureFilterStatus { get; set; }
        [Key(42)]
        public List<ChromatogramPeakFeature> DriftChromFeatures { get; set; } = null;
        public bool IsMultiLayeredData() {
            if (DriftChromFeatures.IsEmptyOrNull()) return false;
            return true;
        }
    }

    [MessagePackObject]
    public class LinkedPeakFeature {
        [Key(0)]
        public int LinkedPeakID { get; set; }
        [Key(1)]
        public PeakLinkFeatureEnum Character { get; set; }
    }
}
