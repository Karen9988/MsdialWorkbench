﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class GM3SpectrumGenerator : ILipidSpectrumGenerator
    {
        private static readonly double C11H15NO7 = new[] {
            MassDiffDictionary.CarbonMass * 11,
            MassDiffDictionary.HydrogenMass * 15,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 7,
        }.Sum();
        private static readonly double C6H10O5 = new[] {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 10,
            MassDiffDictionary.OxygenMass*5,
        }.Sum();
        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double CH4O2 = new[]
        {
            MassDiffDictionary.CarbonMass * 1,
            MassDiffDictionary.HydrogenMass * 4,
            MassDiffDictionary.OxygenMass *2,
        }.Sum();
        private static readonly double C2H3NO = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass *1,
            MassDiffDictionary.OxygenMass *1,
        }.Sum();
        private static readonly double C2H3N = new[]
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass *1,
        }.Sum();

        public GM3SpectrumGenerator()
        {
            spectrumGenerator = new SpectrumPeakGenerator();
        }
        public GM3SpectrumGenerator(ISpectrumPeakGenerator spectrumGenerator)
        {
            this.spectrumGenerator = spectrumGenerator ?? throw new ArgumentNullException(nameof(spectrumGenerator));
        }

        private readonly ISpectrumPeakGenerator spectrumGenerator;

        public bool CanGenerate(ILipid lipid, AdductIon adduct)
        {
            if (lipid.LipidClass == LbmClass.GM3)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    return true;
                }
            }
            return false;
        }

        public IMSScanProperty Generate(Lipid lipid, AdductIon adduct, IMoleculeProperty molecule = null)
        {
            var spectrum = new List<SpectrumPeak>();
            var nlmass = adduct.AdductIonName == "[M+NH4]+" ? adduct.AdductIonAccurateMass :0.0;
            spectrum.AddRange(GetGM3Spectrum(lipid, adduct));
            if (lipid.Chains is PositionLevelChains plChains)
            {
                if (plChains.Chains[0] is SphingoChain sphingo)
                {
                    spectrum.AddRange(GetSphingoSpectrum(lipid, sphingo, adduct));
                    spectrum.AddRange(spectrumGenerator.GetSphingoDoubleBondSpectrum(lipid, sphingo, adduct, nlmass, 10d));
                }
                if (plChains.Chains[1] is AcylChain acyl)
                {
                    //spectrum.AddRange(GetAcylSpectrum(lipid, acyl, adduct));
                    spectrum.AddRange(spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acyl, adduct, nlmass, 10d));
                }
            }
            spectrum = spectrum.GroupBy(spec => spec, comparer)
                .Select(specs => new SpectrumPeak(specs.First().Mass, specs.Sum(n => n.Intensity), string.Join(", ", specs.Select(spec => spec.Comment))))
                .OrderBy(peak => peak.Mass)
                .ToList();
            return CreateReference(lipid, adduct, spectrum, molecule);
        }
        private MoleculeMsReference CreateReference(ILipid lipid, AdductIon adduct, List<SpectrumPeak> spectrum, IMoleculeProperty molecule)
        {
            return new MoleculeMsReference
            {
                PrecursorMz = adduct.ConvertToMz(lipid.Mass),
                IonMode = adduct.IonMode,
                Spectrum = spectrum,
                Name = lipid.Name,
                Formula = molecule?.Formula,
                Ontology = molecule?.Ontology,
                SMILES = molecule?.SMILES,
                InChIKey = molecule?.InChIKey,
                AdductType = adduct,
                CompoundClass = lipid.LipidClass.ToString(),
                Charge = adduct.ChargeNumber,
            };
        }

        private SpectrumPeak[] GetGM3Spectrum(ILipid lipid, AdductIon adduct)
        {
            var adductmass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var h2oLossMass = lipid.Mass + adductmass - H2O;
            var spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak((float)(adduct.ConvertToMz(lipid.Mass)), 999f, "Precursor") { SpectrumComment = SpectrumComment.precursor },
                new SpectrumPeak((float)(h2oLossMass - MassDiffDictionary.HydrogenMass), 100f, "[M-H2O-H+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak((float)(h2oLossMass - C11H15NO7), 100f, "[M-H2O-C11H15NO7+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak((float)(h2oLossMass - C11H15NO7-H2O), 100f, "[M-H2O-C11H17NO8+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak((float)(h2oLossMass - C11H15NO7 - C6H10O5), 150f, "[M-H2O-C17H25NO12+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak((float)(h2oLossMass - C11H15NO7 - C6H10O5-H2O), 150f, "[M-H2O-C17H27NO13+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak((float)(h2oLossMass - C11H15NO7 - C6H10O5 *2), 150f, "[M-H2O-C23H35NO17+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass },
                new SpectrumPeak((float)(h2oLossMass - C11H15NO7 - C6H10O5 *2 -H2O), 300f, "[M-H2O-C23H37NO18+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true},//548
                new SpectrumPeak((float)(C11H15NO7 + H2O + adductmass), 300f, "[C11H17NO8+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true}, //292
                new SpectrumPeak((float)(C11H15NO7 + adductmass), 300f, "[C11H15NO7+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass }, //274
                new SpectrumPeak((float)(C11H15NO7 + C6H10O5 + H2O + adductmass), 300f, "[C17H27NO13+H]+")  { SpectrumComment = SpectrumComment.metaboliteclass , IsAbsolutelyRequiredFragmentForAnnotation = true}, //454
                new SpectrumPeak((float)(C11H15NO7 + C6H10O5*2 +H2O*2 +C2H3N+ adductmass), 300f, "[C23H35NO17 +C2H3N +2H2O  +H]+")  { SpectrumComment = SpectrumComment.metaboliteclass }, //675
            };
            if (adduct.AdductIonName == "[M+NH4]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak((float)(lipid.Mass+ adductmass), 500f, "[M+H]+") { SpectrumComment = SpectrumComment.metaboliteclass },
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetSphingoSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct)
        {
            var chainMass = sphingo.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>();
            if (adduct.AdductIonName == "[M+H]+")
            {
                spectrum.AddRange
                (
                     new[]
                     {
                        new SpectrumPeak((float)(chainMass + MassDiffDictionary.ProtonMass - H2O*2),300f, "[sph+H]+ -Header -H2O") { SpectrumComment = SpectrumComment.acylchain },
                        //new SpectrumPeak(chainMass + MassDiffDictionary.ProtonMass - CH4O2, 100d, "[sph+H]+ -CH4O2"),
                     }
                );
            }
            return spectrum.ToArray();
        }

        private SpectrumPeak[] GetAcylSpectrum(ILipid lipid, AcylChain acyl, AdductIon adduct)
        {
            var chainMass = acyl.Mass + MassDiffDictionary.HydrogenMass;
            var spectrum = new List<SpectrumPeak>()
            {
                //new SpectrumPeak(adduct.ConvertToMz(chainMass) +C2H2N , 200d, "[FAA+C2H+adduct]+") { SpectrumComment = SpectrumComment.acylchain },
            };
            return spectrum.ToArray();
        }

        private static readonly IEqualityComparer<SpectrumPeak> comparer = new SpectrumEqualityComparer();

    }
}
