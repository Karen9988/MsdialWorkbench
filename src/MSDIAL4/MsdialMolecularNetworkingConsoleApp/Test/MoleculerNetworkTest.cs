﻿using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.MolecularNetwork {

    public class LinkNode {
        public double[] Score { get; set; }
        public MoleculeMsReference Node { get; set; }
    }

    public class MoleculerNetworkParameter {
        public double MinimumPeakMatch { get; set; } = 6;
        public double MatchThreshold { get; set; } = 0.95;
        public double MaxEdgeNumPerNode { get; set; } = 5;
        public double MaxPrecursorDiff { get; set; } = 400;
        public double MaxPrecursorDiff_Percent { get; set; } = 100;
        public bool Screening_With_MoleculerNetwork { get; set; } = false;
    }

    public sealed class MoleculerSpectrumNetworkingTest {
        private MoleculerSpectrumNetworkingTest() { }

        public static void Run(string input, string parameterfile, string outputdir) {
            if (!Directory.Exists(outputdir)) {
                Directory.CreateDirectory(outputdir);
            }

            var dt = DateTime.Now;
            var timeStamp = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + dt.Second.ToString();
            var param = ReadParameters(parameterfile);

            var minimumPeakMatch = param.MinimumPeakMatch;
            var matchThreshold = param.MatchThreshold;
            var maxEdgeNumPerNode = param.MaxEdgeNumPerNode;
            var maxPrecursorDiff = param.MaxPrecursorDiff;
            var maxPrecursorDiff_Percent = param.MaxPrecursorDiff_Percent;

            var inputfilename = Path.GetFileNameWithoutExtension(input);
            var output_node_file = Path.Combine(outputdir, inputfilename + "_" + timeStamp + "_node.txt");
            var output_edge_file = Path.Combine(outputdir, inputfilename + "_" + timeStamp + "_edge.txt");
            var output_candidate_file = Path.Combine(outputdir, "candidates_" + timeStamp + ".csv");
            var output_files = Path.Combine(outputdir, "networking_output_files.txt");

            List<MoleculeMsReference> spectra;
            var input_extension = Path.GetExtension(input).ToUpper();
            switch(input_extension)
            {
                case ".MSP":
                    spectra = MspFileParser.MspFileReader(input);
                    break;
                case ".MSDIAL":
                    string msg = "";
                    spectra = MsDialResultParser.MsDialResultRecords(input, out msg);
                    if (msg.Length > 0)
                    {
                        Console.WriteLine(msg);
                        return;
                    }
                    break;
                default:
                    Console.WriteLine("open spectra file failed!");
                    return;
            }

            Console.WriteLine("Converting to normalized spectra");
            foreach (var record in spectra) {
                record.Spectrum = MsScanMatching.GetProcessedSpectrum(record.Spectrum, record.PrecursorMz);
            }

            Console.WriteLine("Creating molecular networking in plant spectra");
            var node2links = new Dictionary<int, List<LinkNode>>();
            var counter = 0;
            var max = spectra.Count * spectra.Count;
            for (int i = 0; i < spectra.Count; i++) {
                for (int j = i + 1; j < spectra.Count; j++) {
                    counter++;
                    //Console.WriteLine("{0} / {1}", counter, max);
                    //Console.SetCursorPosition(0, Console.CursorTop);

                    var prop1 = spectra[i];
                    var prop2 = spectra[j];
                    var massDiff = Math.Abs(prop1.PrecursorMz - prop2.PrecursorMz);
                    if (massDiff > maxPrecursorDiff) continue;
                    if (Math.Max(prop1.PrecursorMz, prop2.PrecursorMz) * maxPrecursorDiff_Percent * 0.01 - Math.Min(prop1.PrecursorMz, prop2.PrecursorMz) < 0) continue;

                    var scoreitem = MsScanMatching.GetModifiedDotProductScore(prop1, prop2);
                    if (scoreitem[1] < minimumPeakMatch) continue;
                    if (scoreitem[0] < matchThreshold) continue;

                    if (node2links.ContainsKey(i)) {
                        node2links[i].Add(new LinkNode() { Score = scoreitem, Node = spectra[j] });
                    }
                    else {
                        node2links[i] = new List<LinkNode>() { new LinkNode() { Score = scoreitem, Node = spectra[j] } };
                    }
                }
                //Console.WriteLine("finish i {0}, total {1}", i, spectra.Count);
            }
            Console.WriteLine("Creating molecular networking done");

            var cNode2Links = new Dictionary<int, List<LinkNode>>();
            foreach (var item in node2links) {
                var nitem = item.Value.OrderByDescending(n => n.Score[0]).ToList();
                cNode2Links[item.Key] = new List<LinkNode>();
                for (int i = 0; i < nitem.Count; i++) {
                    if (i > maxEdgeNumPerNode - 1) break;
                    cNode2Links[item.Key].Add(nitem[i]);
                }
            }
            Console.WriteLine("cnode2links done");

            var nodeDict = new Dictionary<string, MoleculeMsReference>();
            var nodeClustering = new Dictionary<string, int>();
            var clustering = new List<Dictionary<string, MoleculeMsReference>>();
            var removeIdx = new List<int>();
            var sw_output_files = new StreamWriter(output_files);
            using (var sw = new StreamWriter(output_edge_file)) {
                sw_output_files.WriteLine("edge_file:" + output_edge_file);
                sw.WriteLine("Source\tTarget\tSimilarity\tMatchNumber");
                foreach (var item in cNode2Links) {
                    foreach (var link in item.Value) {

                        var source_node_id = spectra[item.Key].Comment;
                        var target_node_id = link.Node.Comment;

                        sw.WriteLine(source_node_id + "\t" + target_node_id + "\t" + link.Score[0] + "\t" + link.Score[1]);

                        if (!nodeDict.ContainsKey(source_node_id)) {
                            nodeDict[source_node_id] = spectra[item.Key];
                            if (nodeClustering.ContainsKey(target_node_id))
                            {
                                var targetClusteringIdx = nodeClustering[target_node_id];
                                nodeClustering[source_node_id] = targetClusteringIdx;
                                clustering[targetClusteringIdx][source_node_id] = spectra[item.Key];
                            }
                            else
                            {
                                var newDict = new Dictionary<string, MoleculeMsReference>();
                                newDict[source_node_id] = spectra[item.Key];
                                clustering.Add(newDict);
                                nodeClustering[source_node_id] = clustering.Count - 1;
                            }
                        }
                        if (!nodeDict.ContainsKey(target_node_id)) {
                            nodeDict[target_node_id] = link.Node;
                            var sourceClusterIdx = nodeClustering[source_node_id];
                            nodeClustering[target_node_id] = sourceClusterIdx;
                            clustering[sourceClusterIdx][target_node_id] = link.Node;
                        }
                        if (nodeClustering[target_node_id] != nodeClustering[source_node_id])
                        {
                            var targetIdx = nodeClustering[target_node_id];
                            var sourceIdx = nodeClustering[source_node_id];
                            var largerIdx = Math.Max(targetIdx, sourceIdx);
                            var lessIdx = Math.Min(targetIdx, sourceIdx);
                            foreach(var pair in clustering[largerIdx])
                            {
                                clustering[lessIdx][pair.Key] = pair.Value;
                                nodeClustering[pair.Key] = lessIdx;
                            }
                            clustering[largerIdx].Clear();
                            removeIdx.Add(largerIdx);
                        }
                    }
                }
            }
            Console.WriteLine("done edge file");
            using (var sw = new StreamWriter(output_node_file)) {
                sw_output_files.WriteLine("node_file:" + output_node_file);
                sw.WriteLine("ID\tMz\tRt\tName\tAdduct");
                foreach (var item in nodeDict) {
                    var key = item.Key;
                    var record = item.Value;
                    var lines = new List<string>() {
                        key, record.PrecursorMz.ToString(), record.ChromXs.Value.ToString(),
                        record.Name,
                        record.AdductType.ToString(),
                    };
                    sw.WriteLine(String.Join("\t", lines));
                }
            }
            Console.WriteLine("done node file");
            if (param.Screening_With_MoleculerNetwork && clustering.Count > 0) {
                Dictionary<string, MoleculeMsReference> maxSubCluster = null;
                int maxClusterSize = 0;
                foreach (var subCluster in clustering)
                {
                    if (subCluster.Count > maxClusterSize)
                    {
                        maxClusterSize = subCluster.Count;
                        maxSubCluster = subCluster;
                    }
                }
                using (var sw = new StreamWriter(output_candidate_file))
                {
                    sw_output_files.WriteLine("candidate_file:" + output_candidate_file);
                    sw.WriteLine("ID,Rt,m/z,ms2");
                    foreach (var item in maxSubCluster)
                    {
                        var record = item.Value;
                        var m2Items = new List<string>();
                        foreach (var item2 in record.Spectrum) {
                            m2Items.Add(String.Join(":", new List<String>{ item2.Mass.ToString(), item2.Intensity.ToString()}));
                        }
                        var lines = new List<string>() {
                            record.ScanID.ToString(), record.ChromXs.Value.ToString(), record.PrecursorMz.ToString(),
                            String.Join(" ", m2Items)
                        };
                        sw.WriteLine(String.Join(",", lines));
                    }
                }
                Console.WriteLine("candidate_file," + output_candidate_file);
                Console.WriteLine("done candidate_file");
            }
            sw_output_files.Close();
        }
        public static MoleculerNetworkParameter ReadParameters(string file) {
            var param = new MoleculerNetworkParameter();
            using (var sr = new StreamReader(file)) {
                while (sr.Peek() != -1) {
                    var line = sr.ReadLine();
                    var linearray = line.Split('=');
                    if (linearray.Length > 1) {
                        var key = linearray[0];
                        var value = linearray[1];
                        switch (key.ToLower()) {
                            case "minimumpeakmatch":
                                if (double.TryParse(value, out double minimumpeakmatch))
                                    param.MinimumPeakMatch = minimumpeakmatch;
                                break;
                            case "matchthreshold":
                                if (double.TryParse(value, out double matchthreshold))
                                    param.MatchThreshold = matchthreshold;
                                break;
                            case "maxedgenumpernode":
                                if (double.TryParse(value, out double maxedgenumpernode))
                                    param.MaxEdgeNumPerNode = maxedgenumpernode;
                                break;
                            case "maxprecursordiff":
                                if (double.TryParse(value, out double maxprecursordiff))
                                    param.MaxPrecursorDiff = maxprecursordiff;
                                break;
                            case "maxprecursordiff_percent":
                                if (double.TryParse(value, out double maxprecursordiff_percent))
                                    param.MaxPrecursorDiff_Percent = maxprecursordiff_percent;
                                break;
                            case "screening_with_moleculernetwork":
                                if (bool.TryParse(value, out bool screening_with_mn))
                                    param.Screening_With_MoleculerNetwork = screening_with_mn;
                                Console.WriteLine(string.Format("screening_with_moleculernetwork:{0}", param.Screening_With_MoleculerNetwork));
                                break;
                            default:
                                Console.WriteLine(string.Format("unknown parameter: {0}", key));
                                break;
                        }
                    }
                }
            }
            return param;
        }
    }
}
