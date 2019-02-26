using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry
{
    /// <summary>
    /// A move in combat uses intentions to tell AI what it does.
    /// </summary>
    public class Move
    {
        #region Variables
        /// <summary>
        /// After using a move, a cooldown prevents using the move again until
        /// that many turns have passed.
        /// Default value is 0.
        /// </summary>
        public byte Cooldown
        {
            get;
            set;
        }

        /// <summary>
        /// After using a move with a cooldown, this is set to the cooldown
        /// value and it falls by one each turn. At 0, the move is ready
        /// for use again.
        /// Default value is 0.
        /// </summary>
        public byte CooldownProgress
        {
            get;
            set;
        }

        /// <summary>
        /// True if the move can be selected.
        /// True by default.
        /// </summary>
        public bool IsMoveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// How long it takes to perform the move. Higher speed delays will
        /// cause other faster moves to take precedence in turn order.
        /// Default value is 0.
        /// </summary>
        public int MoveSpeedDelay
        {
            get;
            set;
        }

        /// <summary>
        /// Contains a list of motives associated with the move.
        /// </summary>
        public List<Constants.Motives> Motives
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, this targeting behavior is used instead of the
        /// character's default behavior.
        /// Default value is null.
        /// </summary>
        public TargetBehavior TargetBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// The action to perform when the move is executed.
        /// Takes a list of all characters, followed by a list of the
        /// targeted characters.
        /// </summary>
        public Action<Character, List<Character>, List<Character>> PerformAction
        {
            get;
            set;
        }

        /// <summary>
        /// How many turns it takes to use the move, default 1. Any number
        /// of moves adding up to 1 turn fraction can be performed each turn,
        /// or a character can forego their action to save up for a move that
        /// requires more than 1 turn.
        /// Default value is 1.
        /// </summary>
        public float TurnFraction
        {
            get;
            set;
        }

        /// <summary>
        /// How many times a move can be used in a turn for moves that take
        /// less than a full turn to execute.
        /// Default is 1.
        /// </summary>
        public int UsesPerTurn
        {
            get;
            set;
        }

        /// <summary>
        /// At the start of a turn, this is set to the uses per turn value. It
        /// decreases by 1 for each usage in the same turn. At 0, the move
        /// cannot be performed.
        /// Default is 0.
        /// </summary>
        public int UsesPerTurnProgress
        {
            get;
            set;
        }

        /// <summary>
        /// If true, using this move ends the turn afterwards.
        /// False by default.
        /// </summary>
        public bool UsesRemainingTurn
        {
            get;
            set;
        }
        #endregion

        #region Static Variables
        private static Random rng;

        /// <summary>
        /// Uses all built-in stats to damage targets the attacker hits, taking
        /// critical hits, range damage modifiers, reductions, knockback and
        /// recoil into consideration.
        /// </summary>
        public static readonly Action<Character, List<Character>, List<Character>> DefaultAction;
        #endregion

        #region Constructors
        static Move()
        {
            rng = new Random();
            DefaultAction = new Action<Character, List<Character>, List<Character>>((attacker, chars, targets) =>
            {
                List<Character> charsHit = ComputeTargetsHit(attacker, targets);

                for (int i = 0; i < charsHit.Count; i++)
                {
                    Character target = charsHit[i];
                    List<float> targetDamage = ComputeTargetDamage(target, ComputeAttackerDamage(attacker));

                    ComputeRangeDamageModifier(attacker, target, targetDamage);
                    ComputeDamageReductions(target, targetDamage);
                    ApplyDamage(attacker, target, targetDamage);
                    ApplyKnockbackDamage(attacker, target, ComputeKnockbackDamage(attacker, target, targetDamage.Sum()));
                    ApplyRecoil(attacker, target, ComputeRecoil(attacker, target));
                }
            });
        }

        /// <summary>
        /// Constructs a move with defaults and the given action, which
        /// takes the current character, list of combat characters, and list of targets.
        /// </summary>
        public Move(Action<Character, List<Character>, List<Character>> action)
        {
            Cooldown = 0;
            CooldownProgress = 0;
            IsMoveEnabled = true;
            MoveSpeedDelay = 0;
            Motives = new List<Constants.Motives>();
            TargetBehavior = null;
            PerformAction = action;
            TurnFraction = 1;
            UsesPerTurn = 1;
            UsesPerTurnProgress = 0;
            UsesRemainingTurn = false;
        }

        /// <summary>
        /// Constructs a move with all defaults, including a default attack action.
        /// </summary>
        public Move()
        {
            Cooldown = 0;
            CooldownProgress = 0;
            IsMoveEnabled = true;
            MoveSpeedDelay = 0;
            Motives = new List<Constants.Motives>();
            TargetBehavior = null;
            PerformAction = DefaultAction;
            TurnFraction = 1;
            UsesPerTurn = 1;
            UsesPerTurnProgress = 0;
            UsesRemainingTurn = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns true if the move can be performed.
        /// </summary>
        public bool CanPerform(Character current)
        {
            return IsMoveEnabled &&
                CooldownProgress == 0 &&
                current.MoveSelectBehavior.TurnFractionLeft <= 1 &&
                UsesPerTurnProgress > 0;
        }

        /// <summary>
        /// Resets and adjusts values in the move to indicate it's the next
        /// turn.
        /// </summary>
        public void NextTurn()
        {
            UsesPerTurnProgress = 0;

            if (CooldownProgress > 0)
            {
                CooldownProgress -= 1;
            }
        }

        /// <summary>
        /// Performs the move with the given list of characters and targets.
        /// Returns whether the move was performed or false if it was null or
        /// not performable.
        /// </summary>
        /// <param name="chars">
        /// A list of all characters.
        /// </param>
        public bool Perform(Character current, List<Character> chars, List<Character> targets)
        {
            if (CanPerform(current))
            {
                if (PerformAction != null)
                {
                    //Manages charge-up moves.
                    if (TurnFraction > 1)
                    {
                        current.MoveSelectBehavior.TurnFractionLeft = (float)Math.Round(
                            current.MoveSelectBehavior.TurnFractionLeft + TurnFraction - 1, 6);
                    }

                    UsesPerTurnProgress -= 1;
                    PerformAction(current, chars, targets);

                    //Starts the cooldown period if nonzero.
                    if (Cooldown != 0 && CooldownProgress == 0)
                    {
                        CooldownProgress = Cooldown;
                    }

                    return true;
                }
            }
            else if (current.MoveSelectBehavior.TurnFractionLeft > 0)
            {
                if (current.MoveSelectBehavior.TurnFractionLeft < 1)
                {
                    current.MoveSelectBehavior.TurnFractionLeft = 0;
                }
                else
                {
                    current.MoveSelectBehavior.TurnFractionLeft = (float)Math.Round(
                        current.MoveSelectBehavior.TurnFractionLeft - 1, 6);
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a list of all targets hit after considering hit status,
        /// attacker's chance to hit, and targets' chances to dodge.
        /// May trigger AttackMissed, AttackDodged.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static List<Character> ComputeTargetsHit(Character attacker, List<Character> targets)
        {
            List<Character> charsHit = new List<Character>();

            if (attacker.CombatStats.HitStatus.Data == Constants.HitStatuses.AlwaysHit)
            {
                charsHit.AddRange(targets);
            }
            else
            {
                // Chance to hit.
                if (attacker.CombatStats.PercentToHit.Data < 1 ||
                    rng.Next(100) + 1 > attacker.CombatStats.PercentToHit.Data)
                {
                    attacker.RaiseAttackMissed(null);
                    return charsHit;
                }

                // Chance for targets to dodge.
                for (int i = 0; i < targets.Count; i++)
                {
                    if (rng.Next(100) < targets[i].CombatStats.PercentToDodge.Data)
                    {
                        attacker.RaiseAttackMissed(targets[i]);
                        targets[i].RaiseAttackDodged(attacker);
                    }
                    else
                    {
                        charsHit.Add(targets[i]);
                    }
                }
            }

            return charsHit;
        }

        /// <summary>
        /// For the attacker, computes each kind of damage without regard to
        /// targets. Computes the critical damage, which is either the
        /// same if no critical hit was made, or equal to the max possible base
        /// damage times the critical hit multiplier. Returns as a tuple with
        /// base damage as the first argument and crit damage as the second.
        /// May trigger RaiseAttackCritHit.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static Tuple<float[], float[]> ComputeAttackerDamage(Character attacker)
        {
            CombatStats charStats = attacker.CombatStats;
            float[] baseDamage = new float[CombatStats.NUM_TYPES_DAMAGE];
            float[] critDamage = new float[CombatStats.NUM_TYPES_DAMAGE];

            // Compute damage and critical hits.
            for (int j = 0; j < CombatStats.NUM_TYPES_DAMAGE; j++)
            {
                baseDamage[j] = charStats.MinDamage.Data[j] + rng.Next(
                    charStats.MaxDamage.Data[j] - charStats.MinDamage.Data[j] + 1);

                if (rng.Next(100) < charStats.PercentToCritHit.Data[j])
                {
                    critDamage[j] = baseDamage[j] * charStats.CritDamageMultiplier.Data[j];
                    attacker.RaiseAttackCritHit(j, critDamage[j]);
                }
                else
                {
                    critDamage[j] = baseDamage[j];
                }
            }

            return new Tuple<float[], float[]>(baseDamage, critDamage);
        }

        /// <summary>
        /// Takes a target and a tuple containing base and critical damage.
        /// Returns critical damage unless the target is immune to criticals,
        /// in which base damage is returned.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static List<float> ComputeTargetDamage(Character target, Tuple<float[], float[]> damage)
        {
            if (target.CombatStats.CriticalStatus.Data == Constants.CriticalStatuses.ImmuneToCriticalHits)
            {
                return damage.Item1.ToList();
            }

            return damage.Item2.ToList();
        }

        /// <summary>
        /// Modifies the damage array in-place to apply the attacker's damage
        /// modifiers based on range.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ComputeRangeDamageModifier(Character attacker, Character target, List<float> damage)
        {
            CombatStats charStats = attacker.CombatStats;

            // Multiplies damage by range.
            float deltaX = attacker.CharStats.Location.Data.Item1
                - target.CharStats.Location.Data.Item1;
            float deltaY = attacker.CharStats.Location.Data.Item2
                - target.CharStats.Location.Data.Item2;
            double dist = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            if (dist < charStats.MinRangeRequired.Data)
            {
                damage.ForEach(num => num *= charStats.MinRangeMultiplier.Data);
            }
            else if (dist > charStats.MaxRangeAllowed.Data)
            {
                damage.ForEach(num => num *= charStats.MaxRangeMultiplier.Data);
            }
            else
            {
                double bias = dist / charStats.MaxRangeAllowed.Data;
                double multiplier = Math.Round((1 - bias) * charStats.MinRangeMultiplier.Data
                    + bias * charStats.MaxRangeMultiplier.Data, 6);

                for (int i = 0; i < damage.Count; i++)
                {
                    damage[i] = damage[i] * (float)multiplier;
                }
            }
        }

        /// <summary>
        /// Modifies the damage array in-place to reduce damage based on the
        /// target's damage reduction and then resistance.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ComputeDamageReductions(Character target, List<float> damage)
        {
            for (int i = 0; i < damage.Count; i++)
            {
                // Damage reduction.
                damage[i] -= target.CombatStats.DamageReduction.Data[i];
                if (damage[i] < 0)
                {
                    damage[i] = 0;
                }

                // Damage resistance.
                float resistance = target.CombatStats.DamageResistance.Data[i];
                if (resistance < 0) { resistance = -resistance + 100; }
                else { resistance = 100 - resistance; }

                damage[i] *= resistance / 100;
            }
        }

        /// <summary>
        /// Applies the damage to the target. This is usually called after all
        /// damage modifiers and reductions.
        /// Triggers RaiseAttackBeforeDamage, RaiseAttackBeforeReceiveDamage, then RaiseAttackAfterDamage.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ApplyDamage(Character attacker, Character target, List<float> damage)
        {
            float damageSum = damage.Sum();

            attacker.RaiseAttackBeforeDamage(target, damage);
            target.RaiseAttackBeforeReceiveDamage(attacker, damage);
            target.CharStats.Health.Data -= (int)damageSum;
            attacker.RaiseAttackAfterDamage(target, damage);
        }

        /// <summary>
        /// Computes and returns knockback damage to the attacker after their
        /// damage reductions.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static float ComputeKnockbackDamage(Character attacker, Character target, float totalDamage)
        {
            float knockback = totalDamage
                * target.CombatStats.PercentKnockback.Data
                + target.CombatStats.ConstantKnockback.Data
                - attacker.CombatStats.DamageReduction.Data[0];

            if (knockback < 0)
            {
                knockback = 0;
            }
            else if (knockback > 0)
            {
                float resistance = attacker.CombatStats.DamageResistance.Data[0];
                if (resistance < 0) { resistance = -resistance + 100; }
                else { resistance = 100 - resistance; }

                knockback *= resistance / 100;
            }

            return knockback;
        }

        /// <summary>
        /// Applies knockback damage to the attacker.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ApplyKnockbackDamage(Character attacker, Character target, float knockback)
        {
            attacker.RaiseAttackKnockback(attacker, target, knockback);
            attacker.CharStats.Health.Data -= (int)knockback;
        }

        /// <summary>
        /// Computes and returns a tuple containing the recoil magnitude and
        /// the target's new location.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static Tuple<float, Tuple<float, float>> ComputeRecoil(Character attacker, Character target)
        {
            if (attacker.CombatStats.MinRecoil.Data > 0)
            {
                float recoil = attacker.CombatStats.MinRecoil.Data + rng.Next((int)(
                    attacker.CombatStats.MaxRecoil.Data -
                    attacker.CombatStats.MinRecoil.Data));

                double recoilDir = Math.Atan2(
                    target.CharStats.Location.Data.Item2 -
                    attacker.CharStats.Location.Data.Item2,
                    target.CharStats.Location.Data.Item1 -
                    attacker.CharStats.Location.Data.Item1);

                var newLocation = new Tuple<float, float>(
                    (float)Math.Round(target.CharStats.Location.Data.Item1 + recoil * Math.Cos(recoilDir), 10),
                    (float)Math.Round(target.CharStats.Location.Data.Item2 + recoil * Math.Sin(recoilDir), 10));

                return new Tuple<float, Tuple<float, float>>(recoil, newLocation);
            }

            return new Tuple<float, Tuple<float, float>>(0, target.CharStats.Location.Data);
        }

        /// <summary>
        /// Applies recoil to the target. Takes an attacker, target, and tuple
        /// containing recoil magnitude and new location.
        /// Triggers RaiseAttackReceiveRecoil, then RaiseAttackRecoil.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ApplyRecoil(Character attacker, Character target, Tuple<float, Tuple<float, float>> recoil)
        {
            target.RaiseAttackReceiveRecoil(attacker, recoil.Item1, recoil.Item2);
            attacker.RaiseAttackRecoil(target, recoil.Item1, recoil.Item2);
            target.CharStats.Location.Data = recoil.Item2;
        }
        #endregion
    }
}
