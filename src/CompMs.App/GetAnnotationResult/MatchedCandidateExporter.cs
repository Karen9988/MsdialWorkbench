using Accord.Math.Geometry;
using CompMs.App.GetAnnotationResult;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System.Xml.Linq;

namespace CompMs.App.CompareAnnotationResult
{
    internal sealed class MatchedCandidateExporter
    { 
        public static void Export(Stream outputStream, List<Tuple<Candidates.Candidate, MoleculeMsReference>> matchedCandidates)
        {
            var fw = new StreamWriter(outputStream);
            fw.WriteLine("CandidateIndex,FPS-RT,FPS-Precursor,Ratio,Adduct,Type,Name,Formula,INCHIKEY,SMILES");
            foreach (var tuple in matchedCandidates) {
                var candidate = tuple.Item1;
                var reference = tuple.Item2;
                var line = string.Join(',', candidate.ID, candidate.RT.ToString(), candidate.Mass.ToString(),
                                             candidate.FPSRatio.ToString(), reference.AdductType.ToString(),
                                             candidate.Type, reference.Name, reference.Formula.ToString(),
                                             reference.InChIKey, reference.SMILES);
                fw.WriteLine(line);
            }
            fw.Close();
        }
    }
}
