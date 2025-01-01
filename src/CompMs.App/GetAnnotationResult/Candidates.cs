using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.Common.Algorithm;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.GetAnnotationResult
{
    public class Candidates
    {
        public Candidates() { }

        public class Candidate : IAnnotatedObject, IChromatogramPeak
        {
            public Candidate() { }

            public double Mass { get; set; }

            public double RT { get; set; }

            public string Type { get; set; } = "Unknown";
            public string Adduct { get; set; } = "Unknown";

            public double FPSRatio {  get; set; }

            public List<SpectrumPeak> Peaks { get; set; } = new List<SpectrumPeak>();

            public MsScanMatchResultContainer MatchResults => throw new NotImplementedException();

            public int ID {  get; set; }

            public ChromXs ChromXs { get; set; }
            public double Intensity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        }

        public List<Candidate> Candis { get; set; } = new List<Candidate> ();

    }
}
