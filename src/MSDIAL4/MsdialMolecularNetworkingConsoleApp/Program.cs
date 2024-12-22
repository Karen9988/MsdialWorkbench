using CompMs.App.MsdialConsole.MolecularNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.MolecularNetworking {
    class Program {
        static int Main(string[] args) {
            if (args.Length == 0) return argsError();
            if (args.Length < 7) return argsError();

            var mspfilepath = string.Empty;
            var paramfilepath = string.Empty;
            var outputdir = string.Empty;
            for (int i = 1; i < args.Length; i++) {
                if (args[i] == "-i" && i + 1 < args.Length) mspfilepath = args[i + 1];
                else if (args[i] == "-p" && i + 1 < args.Length) paramfilepath = args[i + 1];
                else if (args[i] == "-o" && i + 1 < args.Length) outputdir = args[i + 1];
            }

            if (mspfilepath == string.Empty || outputdir == string.Empty) return argsError();

            MoleculerSpectrumNetworkingTest.Run(mspfilepath, paramfilepath, outputdir);
            return 1;
        }

        /// <summary>
		/// Shows console application usage help
		/// </summary>
		/// <returns>error code -1</returns>
        private static int argsError() {
            var error = @"Msdial molecular netwokring console app requires the following args:
						MsdialMolecularNetworkingConsoleApp.exe <analysisType> -i <input msp file path> -o <output directory> -p <parameter file path>
						Where: <analysisType>	is now 'msms' only	(required)
							   <input msp file path>	is the MSP format file for edge clustering	(required)
							   <output directory>	is the folder path to save results	(required)
							   <parameter file>	contains parameter sets	(required)";

            Console.Error.WriteLine(error);

            return -1;
        }
    }
}
