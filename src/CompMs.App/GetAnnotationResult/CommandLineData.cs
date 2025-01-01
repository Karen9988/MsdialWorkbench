using Accord.Math;
using CompMs.App.GetAnnotationResult;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;

namespace CompMs.App.CompareAnnotationResult
{
    internal sealed class CommandLineData {
        [LongStyleArgument("--input")]
        public string? AlignmentResultPath { get; set; }
        [LongStyleArgument("--output")]
        public string? OutputPath { get; set; }
        [LongStyleArgument("--library")]
        public string? LibraryPath { get; set; }
        [LongStyleArgument("--mz-tolerance")]
        public double MzTolerance { get; set; }
        [LongStyleArgument("--rt-tolerance")]
        public double RtTolerance { get; set; }
        [LongStyleArgument("--amplitude-threshold")]
        public double AmplitudeThreshold { get; set; }

        public List<MoleculeMsReference> GetLibrary() {
            if (!File.Exists(LibraryPath)) {
                throw new Exception("Library path is not entered.");
            }

            string libraryPath = LibraryPath!;
            switch (Path.GetExtension(libraryPath)) {
                case ".txt":
                    var textdb = TextLibraryParser.TextLibraryReader(libraryPath, out string error);
                    if (string.IsNullOrEmpty(error)) {
                        return textdb;
                    }
                    else {
                        throw new Exception(error);
                    }
                case ".msp":
                    var mspdb = LibraryHandler.ReadMspLibrary(libraryPath) ?? throw new Exception("Loading msp file failed.");
                    return mspdb;
                default:
                    throw new Exception( "Unsupported library type.");
            }
        }

        public static Candidates LoadFromClusterResult(string filePath)
        {
            var alignmentResultContainer = new Candidates();
            alignmentResultContainer.Candis = new List<Candidates.Candidate>();
            using (var rdr = new StreamReader(filePath))
            {
                if (rdr.ReadLine() == null) { return alignmentResultContainer; }
                while (rdr.Peek() != -1)
                {
                    var line = rdr.ReadLine();
                    var contents = line.Split(',');
                    var aresult = new Candidates.Candidate();
                    aresult.ID = int.Parse(contents[0]);
                    aresult.Mass = double.Parse(contents[2]);
                    aresult.RT = double.Parse(contents[1]);
                    aresult.ChromXs = new ChromXs() { RT = new RetentionTime(aresult.RT) };
                    aresult.FPSRatio = double.Parse(contents[10]);
                    aresult.Adduct = contents[11].Trim();
                    aresult.Type = contents[12];
                    var ms2s = contents.Length > 12 ? contents[13] : "";
                    var ms2list = ms2s.Split(' ');
                    Console.WriteLine(ms2s);
                    foreach (var ms2item in ms2list)
                    {
                        var ms2pair = ms2item.Split(':');
                        if (ms2pair.Length != 2) {  continue; }
                        var peakItem = new SpectrumPeak();
                        peakItem.Mass = double.Parse(ms2pair[0]);
                        peakItem.Intensity = double.Parse(ms2pair[1]);
                        aresult.Peaks.Add(peakItem);
                    }
                    alignmentResultContainer.Candis.Add(aresult);
                }
            }
            return alignmentResultContainer;
        }
        public Candidates LoadSpots() {
            if (!File.Exists(AlignmentResultPath))
            {
                throw new Exception("AlignmentResultFile is not found");
            }
            if (!AlignmentResultPath.EndsWith(".csv"))
            {
                throw new Exception("Unknown alignment result format.");
            }
            return LoadFromClusterResult(AlignmentResultPath);
        }

        public Stream GetOutputStream() {
            var output = OutputPath;
            var file = AlignmentResultPath!;
            if (string.IsNullOrEmpty(output)) {
                output = Directory.GetParent(file)?.FullName;
            }
            if (string.IsNullOrEmpty(output)) {
                throw new Exception("OutputPath is required.");
            }
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }
            return File.Open(Path.Combine(output, "annotation_result.csv"), FileMode.Create);
        }
    }
}