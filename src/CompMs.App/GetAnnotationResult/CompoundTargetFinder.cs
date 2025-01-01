using CompMs.App.GetAnnotationResult;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.CompareAnnotationResult
{
    internal sealed class CompoundTargetFinder
    {
        private readonly MatchedSpotCandidateCalculator _calculator;
        private readonly List<MoleculeMsReference> _references;
        private readonly Ms2ScanMatching _ms2ScanMatcher;

        public CompoundTargetFinder(CommandLineData data, MatchedSpotCandidateCalculator candidateCalculator) {
            _calculator = candidateCalculator;
            _references = data.GetLibrary();
            _ms2ScanMatcher = new Ms2ScanMatching(new Common.Parameter.MsRefSearchParameterBase());
        }

        public List<Tuple<Candidates.Candidate, MoleculeMsReference>> Find(IEnumerable<Candidates.Candidate> spots) {
            var candidates = new List<Tuple<Candidates.Candidate, MoleculeMsReference>>();
            bool in_gal_glu_pair = false;
            string pair_mode = string.Empty;
            foreach (var spot in spots) {
                foreach (var reference in _references) {
                    var candidate = _calculator.Match(spot, reference);
                    if (candidate == null) {
                        //candidates.Add(candidate);
                        continue;
                    }
                    if (!spot.Adduct.Equals(reference.AdductType.ToString()))
                    {
                        continue;
                    }
                    var res = _ms2ScanMatcher.GetMatchedSpectrum(spot.Peaks, reference.Spectrum);
                    if (res != null && res.Reference.Count > 0) {
                        var index = reference.Name.IndexOf("galactoside");
                        if (index > 0)  // contains
                        {
                            var mode = reference.Name.Substring(0, index);
                            if (!mode.Equals(pair_mode) || !in_gal_glu_pair)
                            {
                                in_gal_glu_pair=true;
                                pair_mode = mode;
                                candidates.Add(new Tuple<Candidates.Candidate, MoleculeMsReference>(spot, reference));
                                break;
                            } else
                            {
                                continue;
                            }
                        }
                        candidates.Add(new Tuple<Candidates.Candidate, MoleculeMsReference>(spot, reference));
                        in_gal_glu_pair = false;
                    }
                }
            }
            return candidates;
        }
    }
}
