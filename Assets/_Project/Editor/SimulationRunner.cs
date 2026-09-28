using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DRG.EditorTools
{
    // Runs the random-policy simulation for 2..MaxPlayers and writes the Markdown report.
    // Menu: DRG > Run Simulation
    // Command line: Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod DRG.EditorTools.SimulationRunner.Run
    public static class SimulationRunner
    {
        private const int GamesPerPlayerCount = 1000;
        private const int Seed = 20260928;
        private const string ReportPath = "Docs/Simulation/random_policy_report.md";

        [MenuItem("DRG/Run Simulation")]
        public static void Run()
        {
            GameSettings settings = new GameSettings();
            BattleSimulator simulator = new BattleSimulator(settings);

            List<SimulationStats> batches = new List<SimulationStats>();
            for (int playerCount = 2; playerCount <= settings.MaxPlayers; playerCount++)
            {
                batches.Add(simulator.Run(playerCount, GamesPerPlayerCount, Seed, BattleSimulator.DefaultTurnCap));
            }

            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ReportPath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, SimulationReport.ToMarkdown(batches));
            Debug.Log("DRG simulation report written: " + fullPath);
        }
    }
}
