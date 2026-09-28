using System;
using System.Collections.Generic;

namespace DRG
{
    // Deterministic resolution of one turn from the players' locked actions.
    // Steps run in a fixed implementation order, but every attack is judged only from the locked actions
    // (never from HP changed earlier in the same resolution), so all actions take effect simultaneously.
    // Must not depend on UI, animation, VFX, audio, or scene objects.
    public class BattleResolver
    {
        private readonly GameSettings settings;
        private readonly ActionValidator validator;

        public BattleResolver(GameSettings settings)
        {
            this.settings = settings;
            validator = new ActionValidator(settings);
        }

        public BattleResult ResolveTurn(List<Player> players)
        {
            BattleResult result = new BattleResult();

            // 1-2. Collect locked actions and validate them against the state at the start of resolution.
            List<PlayerAction> validActions = CollectValidActions(players, result);

            // 3. Ki costs and gains. DEC-002: paid regardless of whether an attack later hits.
            for (int i = 0; i < validActions.Count; i++)
            {
                ApplyKi(FindPlayer(players, validActions[i].PlayerId), validActions[i], result);
            }

            // 4-8. Judge every attack (Spirit Bomb, Teleport, mutual Energy Wave cancel, Block) and total damage.
            Dictionary<int, int> damageTaken = new Dictionary<int, int>();
            for (int i = 0; i < validActions.Count; i++)
            {
                PlayerAction action = validActions[i];
                if (!IsAttack(action.ActionType))
                {
                    continue;
                }

                PlayerAction targetAction = FindAction(validActions, action.TargetPlayerId);
                ActionResultType resultType = JudgeAttack(action, targetAction);
                result.ActionResults.Add(new ActionResult
                {
                    PlayerId = action.PlayerId,
                    ActionType = action.ActionType,
                    TargetPlayerId = action.TargetPlayerId,
                    ResultType = resultType
                });

                if (resultType == ActionResultType.Hit)
                {
                    int damage = GetDamage(action.ActionType);
                    result.DamageResults.Add(new DamageResult
                    {
                        AttackerId = action.PlayerId,
                        TargetId = action.TargetPlayerId,
                        Damage = damage
                    });

                    int total;
                    damageTaken.TryGetValue(action.TargetPlayerId, out total);
                    damageTaken[action.TargetPlayerId] = total + damage;
                }
            }

            // 9. Apply HP. DEC-003: HP does not go below 0; DamageResults keep the full damage dealt.
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                int damage;
                if (damageTaken.TryGetValue(player.PlayerId, out damage))
                {
                    player.HP = Math.Max(0, player.HP - damage);
                }
            }

            // 10. Eliminate players at HP 0 or below.
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player.State == PlayerState.Alive && player.HP <= 0)
                {
                    player.State = PlayerState.Eliminated;
                    result.EliminatedPlayerIds.Add(player.PlayerId);
                }
            }

            // 11. Victory check. DEC-004: zero survivors is a draw.
            DecideOutcome(players, result);
            return result;
        }

        // DEC-001: eliminated players act with None and are skipped.
        // DEC-005: an invalid action is recorded as Failed and has no effect (no Ki change, no attack, no defense).
        private List<PlayerAction> CollectValidActions(List<Player> players, BattleResult result)
        {
            List<PlayerAction> validActions = new List<PlayerAction>();
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player.State != PlayerState.Alive)
                {
                    continue;
                }

                PlayerAction action = player.CurrentAction;
                bool isValid = action != null
                    && action.PlayerId == player.PlayerId
                    && validator.Validate(action, players) == ActionValidationResult.Valid;

                if (isValid)
                {
                    validActions.Add(action);
                }
                else
                {
                    result.ActionResults.Add(new ActionResult
                    {
                        PlayerId = player.PlayerId,
                        ActionType = action != null ? action.ActionType : ActionType.None,
                        TargetPlayerId = action != null ? action.TargetPlayerId : PlayerAction.NoTarget,
                        ResultType = ActionResultType.Failed
                    });
                }
            }

            return validActions;
        }

        private void ApplyKi(Player player, PlayerAction action, BattleResult result)
        {
            int amount;
            if (action.ActionType == ActionType.Gather)
            {
                amount = Math.Min(player.Ki + settings.GatherKiGain, player.MaxKi) - player.Ki;
            }
            else
            {
                amount = -validator.GetKiCost(action.ActionType);
            }

            if (amount == 0)
            {
                return;
            }

            player.Ki += amount;
            result.KiChanges.Add(new KiChangeResult { PlayerId = player.PlayerId, Amount = amount });
        }

        // targetAction is null when the target's own action Failed; a failed action gives no defense.
        private static ActionResultType JudgeAttack(PlayerAction attack, PlayerAction targetAction)
        {
            // Teleport avoids every attack targeting self.
            if (targetAction != null && targetAction.ActionType == ActionType.Teleport)
            {
                return ActionResultType.Dodged;
            }

            // Spirit Bomb ignores Block and is never cancelled.
            if (attack.ActionType == ActionType.SpiritBomb)
            {
                return ActionResultType.Hit;
            }

            // Energy Wave cancels only when the target is firing an Energy Wave back at this attacker (exact A<->B).
            if (targetAction != null
                && targetAction.ActionType == ActionType.EnergyWave
                && targetAction.TargetPlayerId == attack.PlayerId)
            {
                return ActionResultType.Cancelled;
            }

            if (targetAction != null && targetAction.ActionType == ActionType.Block)
            {
                return ActionResultType.Blocked;
            }

            return ActionResultType.Hit;
        }

        private static void DecideOutcome(List<Player> players, BattleResult result)
        {
            int aliveCount = 0;
            int lastAliveId = BattleResult.NoWinner;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].State == PlayerState.Alive)
                {
                    aliveCount++;
                    lastAliveId = players[i].PlayerId;
                }
            }

            if (aliveCount == 1)
            {
                result.Outcome = GameOutcome.Winner;
                result.WinnerPlayerId = lastAliveId;
            }
            else if (aliveCount == 0)
            {
                result.Outcome = GameOutcome.Draw;
            }
        }

        private int GetDamage(ActionType actionType)
        {
            return actionType == ActionType.SpiritBomb ? settings.SpiritBombDamage : settings.EnergyWaveDamage;
        }

        private static bool IsAttack(ActionType actionType)
        {
            return actionType == ActionType.EnergyWave || actionType == ActionType.SpiritBomb;
        }

        private static PlayerAction FindAction(List<PlayerAction> actions, int playerId)
        {
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i].PlayerId == playerId)
                {
                    return actions[i];
                }
            }

            return null;
        }

        private static Player FindPlayer(List<Player> players, int playerId)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].PlayerId == playerId)
                {
                    return players[i];
                }
            }

            return null;
        }
    }
}
