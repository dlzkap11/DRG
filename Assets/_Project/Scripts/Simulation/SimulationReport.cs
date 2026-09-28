using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DRG
{
    // Formats simulation batches as a Markdown report. Contains no timestamp so the same seed gives an identical file.
    public static class SimulationReport
    {
        private static readonly ActionType[] ReportedActions =
        {
            ActionType.Gather,
            ActionType.EnergyWave,
            ActionType.Block,
            ActionType.Teleport,
            ActionType.SpiritBomb
        };

        public static string ToMarkdown(List<SimulationStats> batches)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# DRG 자동 시뮬레이션 리포트 (무작위 정책)");
            sb.AppendLine();
            sb.AppendLine("> 이 파일은 `DRG > Run Simulation` 메뉴 또는 `-executeMethod DRG.EditorTools.SimulationRunner.Run`으로 생성됩니다. 직접 수정하지 마세요.");
            sb.AppendLine();
            sb.AppendLine("## 설정");
            sb.AppendLine("- 정책: 매 턴 사용할 수 있는 행동 중 하나를 같은 확률로 고르고, 공격 대상도 살아 있는 상대 중 같은 확률로 고른다.");
            sb.AppendLine("- 경로: 실제 `TurnManager` (Lock 검증) → `BattleResolver`");
            if (batches.Count > 0)
            {
                sb.AppendLine("- 인원별 판 수: " + batches[0].Games + ", seed: " + batches[0].Seed + ", 턴 상한: " + batches[0].TurnCap + " (시뮬레이션 안전장치이며 게임 규칙이 아님)");
            }

            sb.AppendLine();
            sb.AppendLine("> **해석 주의**: 판단 없이 무작위로 행동하는 기준선이다. 실제 사람이나 봇의 플레이와 다르며, 이 수치만으로 밸런스를 결론짓지 않는다.");
            sb.AppendLine();

            sb.AppendLine("## 1. 게임 길이와 결과");
            sb.AppendLine("| 인원 | 판 수 | 평균 턴 | 중앙값 | 최소 | 최대 | 승자 결정 | 무승부 | 미종료(상한 도달) |");
            sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            for (int i = 0; i < batches.Count; i++)
            {
                SimulationStats s = batches[i];
                List<int> sorted = new List<int>(s.GameLengths);
                sorted.Sort();
                sb.AppendLine("| " + s.PlayerCount + " | " + s.Games
                    + " | " + Format(Ratio(s.TotalTurns, s.Games))
                    + " | " + Median(sorted)
                    + " | " + (sorted.Count > 0 ? sorted[0] : 0)
                    + " | " + (sorted.Count > 0 ? sorted[sorted.Count - 1] : 0)
                    + " | " + Percent(s.WinnerGames, s.Games)
                    + " | " + Percent(s.DrawGames, s.Games)
                    + " | " + Percent(s.UnfinishedGames, s.Games) + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 2. 행동 사용률");
            sb.AppendLine("살아 있는 플레이어가 Lock한 전체 행동 중 비율.");
            sb.AppendLine();
            sb.AppendLine("| 인원 | 기 모으기 | 에너지파 | 방어 | 순간이동 | 원기옥 | 전체 행동 수 |");
            sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");
            for (int i = 0; i < batches.Count; i++)
            {
                SimulationStats s = batches[i];
                sb.Append("| " + s.PlayerCount);
                for (int a = 0; a < ReportedActions.Length; a++)
                {
                    sb.Append(" | " + Percent(s.ActionCounts[(int)ReportedActions[a]], s.TotalActions));
                }

                sb.AppendLine(" | " + s.TotalActions + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 3. 공격 결과");
            sb.AppendLine("각 공격의 판정 결과 비율.");
            sb.AppendLine();
            sb.AppendLine("| 인원 | 에너지파 수 | 적중 | 막힘 | 회피 | 상쇄 | 원기옥 수 | 적중 | 회피 |");
            sb.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            for (int i = 0; i < batches.Count; i++)
            {
                SimulationStats s = batches[i];
                int ew = (int)ActionType.EnergyWave;
                int sbomb = (int)ActionType.SpiritBomb;
                int ewTotal = AttackTotal(s, ew);
                int sbTotal = AttackTotal(s, sbomb);
                sb.AppendLine("| " + s.PlayerCount
                    + " | " + ewTotal
                    + " | " + Percent(s.AttackResults[ew, (int)ActionResultType.Hit], ewTotal)
                    + " | " + Percent(s.AttackResults[ew, (int)ActionResultType.Blocked], ewTotal)
                    + " | " + Percent(s.AttackResults[ew, (int)ActionResultType.Dodged], ewTotal)
                    + " | " + Percent(s.AttackResults[ew, (int)ActionResultType.Cancelled], ewTotal)
                    + " | " + sbTotal
                    + " | " + Percent(s.AttackResults[sbomb, (int)ActionResultType.Hit], sbTotal)
                    + " | " + Percent(s.AttackResults[sbomb, (int)ActionResultType.Dodged], sbTotal) + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 4. 방어 행동의 효과");
            sb.AppendLine("방어·순간이동 중 그 턴에 실제로 공격을 하나 이상 막거나 피한 비율.");
            sb.AppendLine();
            sb.AppendLine("| 인원 | 방어 사용 | 실제로 막음 | 순간이동 사용 | 실제로 피함 |");
            sb.AppendLine("|---:|---:|---:|---:|---:|");
            for (int i = 0; i < batches.Count; i++)
            {
                SimulationStats s = batches[i];
                int blocks = s.ActionCounts[(int)ActionType.Block];
                int teleports = s.ActionCounts[(int)ActionType.Teleport];
                sb.AppendLine("| " + s.PlayerCount
                    + " | " + blocks + " | " + Percent(s.UsefulBlocks, blocks)
                    + " | " + teleports + " | " + Percent(s.UsefulTeleports, teleports) + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 5. 좌석별 승률");
            sb.AppendLine("승자가 나온 판 중 좌석(P0부터)별 승리 비율. 좌석 순서가 결과에 영향을 주는지 확인하는 용도이다.");
            sb.AppendLine();
            sb.AppendLine("| 인원 | 좌석별 승률 |");
            sb.AppendLine("|---:|---|");
            for (int i = 0; i < batches.Count; i++)
            {
                SimulationStats s = batches[i];
                StringBuilder seats = new StringBuilder();
                for (int seat = 0; seat < s.WinsBySeat.Length; seat++)
                {
                    if (seat > 0)
                    {
                        seats.Append(", ");
                    }

                    seats.Append("P" + seat + " " + Percent(s.WinsBySeat[seat], s.WinnerGames));
                }

                sb.AppendLine("| " + s.PlayerCount + " | " + seats + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 6. 동시 탈락");
            sb.AppendLine("| 인원 | 2명 이상이 한 턴에 함께 탈락한 턴 | 전체 턴 대비 | 무승부로 끝난 판 |");
            sb.AppendLine("|---:|---:|---:|---:|");
            for (int i = 0; i < batches.Count; i++)
            {
                SimulationStats s = batches[i];
                sb.AppendLine("| " + s.PlayerCount
                    + " | " + s.MultiEliminationTurns
                    + " | " + Percent(s.MultiEliminationTurns, s.TotalTurns)
                    + " | " + Percent(s.DrawGames, s.Games) + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 7. 무결성 확인");
            int failed = 0;
            for (int i = 0; i < batches.Count; i++)
            {
                failed += batches[i].FailedActions;
            }

            sb.AppendLine("- 전투 처리 중 `Failed`가 된 행동: " + failed + "건 (모든 행동이 Lock 검증을 거치므로 0이어야 한다)");
            return sb.ToString();
        }

        private static int AttackTotal(SimulationStats s, int actionIndex)
        {
            int total = 0;
            for (int r = 0; r < SimulationStats.ActionResultTypeCount; r++)
            {
                total += s.AttackResults[actionIndex, r];
            }

            return total;
        }

        private static string Median(List<int> sorted)
        {
            if (sorted.Count == 0)
            {
                return "0";
            }

            int mid = sorted.Count / 2;
            if (sorted.Count % 2 == 1)
            {
                return sorted[mid].ToString(CultureInfo.InvariantCulture);
            }

            return Format((sorted[mid - 1] + sorted[mid]) / 2.0);
        }

        private static double Ratio(int value, int total)
        {
            return total == 0 ? 0.0 : (double)value / total;
        }

        private static string Percent(int value, int total)
        {
            return Format(Ratio(value, total) * 100.0) + "%";
        }

        private static string Format(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
