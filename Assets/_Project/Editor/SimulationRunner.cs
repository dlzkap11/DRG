using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DRG.EditorTools
{
    // Runs the random-policy baseline and the BotAI policy for 2..MaxPlayers and writes three Markdown reports:
    // one per policy plus a side-by-side comparison.
    // Menu: DRG > Run Simulation
    // Command line: Unity.exe -batchmode -nographics -quit -projectPath <path> -executeMethod DRG.EditorTools.SimulationRunner.Run
    public static class SimulationRunner
    {
        private const int GamesPerPlayerCount = 1000;
        private const int Seed = 20260928;
        private const string RandomReportPath = "Docs/Simulation/random_policy_report.md";
        private const string BotAIReportPath = "Docs/Simulation/bot_ai_report.md";
        private const string ComparisonReportPath = "Docs/Simulation/policy_comparison.md";

        [MenuItem("DRG/Run Simulation")]
        public static void Run()
        {
            GameSettings settings = new GameSettings();
            BattleSimulator simulator = new BattleSimulator(settings);

            List<SimulationStats> randomBatches = new List<SimulationStats>();
            List<SimulationStats> botBatches = new List<SimulationStats>();
            for (int playerCount = 2; playerCount <= settings.MaxPlayers; playerCount++)
            {
                randomBatches.Add(simulator.Run(playerCount, GamesPerPlayerCount, Seed, BattleSimulator.DefaultTurnCap));
                botBatches.Add(simulator.Run(playerCount, GamesPerPlayerCount, Seed, BattleSimulator.DefaultTurnCap, new BotAI(Seed, settings)));
            }

            Write(RandomReportPath, SimulationReport.ToMarkdown(randomBatches));
            Write(BotAIReportPath, SimulationReport.ToMarkdown(botBatches,
                SimulationReport.BotAIPolicyTitle, SimulationReport.BotAIPolicyDescription, SimulationReport.BotAIPolicyCaution));
            Write(ComparisonReportPath, SimulationReport.ToComparisonMarkdown(randomBatches, botBatches,
                SimulationReport.RandomPolicyTitle, SimulationReport.BotAIPolicyTitle));
        }

        private static void Write(string relativePath, string content)
        {
            string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content);
            Debug.Log("DRG simulation report written: " + fullPath);
        }
    }
}
