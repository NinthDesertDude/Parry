using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry.Combat
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
        /// The base speed of the move. Higher speeds will cause the move to
        /// take precedence in turn order. Default value is 0.
        /// </summary>
        public int MoveSpeed
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
        /// combatant's default behavior.
        /// Default value is null.
        /// </summary>
        public TargetBehavior TargetBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, this movement behavior is used instead of the
        /// combatant's default pre-move behavior.
        /// Default value is null.
        /// </summary>
        public MovementBehavior MovementBeforeBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// If non-null, this movement behavior is used instead of the
        /// combatant's default post-move behavior.
        /// Default value is null.
        /// </summary>
        public MovementBehavior MovementAfterBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// The action to perform when the move is executed.
        /// Takes a list of all combatants, followed by a list of the
        /// targeted combatants.
        /// </summary>
        public Action<Combatant, List<Combatant>, List<Combatant>> PerformAction
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
        /// After using a move with a turn fraction greater than 1, this is set
        /// to the turn fraction and it falls by one each turn. At 0, the move
        /// is performed.
        /// </summary>
        public float TurnFractionProgress
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
        public static readonly Action<Combatant, List<Combatant>, List<Combatant>> DefaultAction;
        #endregion

        #region Constructors
        static Move()
        {
            rng = new Random();
            DefaultAction = new Action<Combatant, List<Combatant>, List<Combatant>>((attacker, combatants, targets) =>
            {
                List<Combatant> combatantsHit = ComputeTargetsHit(attacker, targets);

                for (int i = 0; i < combatantsHit.Count; i++)
                {
                    Combatant target = combatantsHit[i];
                    List<float> targetDamage = ComputeTargetDamage(target, ComputeAttackerDamage(attacker));

                    ComputeRangeDamageModifier(attacker, target, targetDamage);
                    ComputeDamageReductions(target, targetDamage);
                    ApplyDamage(attacker, target, targetDamage);

                    float knockback = ComputeKnockbackDamage(attacker, target, targetDamage.Sum());
                    attacker.CurrentHealth.Data -= (int)knockback;

                    ApplyRecoil(attacker, target, ComputeRecoil(attacker, target));
                }
            });
        }

        /// <summary>
        /// Constructs a move with defaults and the given action, which
        /// takes a list of combatants followed by a list of targets.
        /// </summary>
        public Move(Action<Combatant, List<Combatant>, List<Combatant>> action)
        {
            Cooldown = 0;
            CooldownProgress = 0;
            IsMoveEnabled = true;
            MoveSpeed = 0;
            Motives = new List<Constants.Motives>();
            TargetBehavior = null;
            MovementBeforeBehavior = null;
            MovementAfterBehavior = null;
            PerformAction = action;
            TurnFraction = 1;
            TurnFractionProgress = 0;
            UsesPerTurn = 1;
            UsesPerTurnProgress = 0;
            UsesRemainingTurn = false;
        }

        /// <summary>
        /// Constructs a move with all defaults.
        /// </summary>
        public Move()
        {
            Cooldown = 0;
            CooldownProgress = 0;
            IsMoveEnabled = true;
            MoveSpeed = 0;
            Motives = new List<Constants.Motives>();
            TargetBehavior = null;
            MovementBeforeBehavior = null;
            MovementAfterBehavior = null;
            PerformAction = DefaultAction;
            TurnFraction = 1;
            TurnFractionProgress = 0;
            UsesPerTurn = 1;
            UsesPerTurnProgress = 0;
            UsesRemainingTurn = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns true if the move can be performed.
        /// </summary>
        public bool CanPerform()
        {
            return IsMoveEnabled &&
                CooldownProgress == 0 &&
                TurnFractionProgress == 0 &&
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
            if (TurnFractionProgress > 0)
            {
                TurnFractionProgress -= 1;
            }
        }

        /// <summary>
        /// Performs the move with the given list of combatants and targets.
        /// Returns whether the move was performed or false if it couldn't be.
        /// </summary>
        /// <param name="combatants">
        /// A list of all combatants.
        /// </param>
        public bool Perform(Combatant current, List<Combatant> combatants, List<Combatant> targets)
        {
            //Manages charge-up moves.
            if (TurnFraction > 1 && TurnFractionProgress == 0)
            {
                TurnFractionProgress = TurnFraction + 1;
            }

            if (CanPerform())
            {
                UsesPerTurnProgress -= 1;
                PerformAction(current, combatants, targets);

                //Starts the cooldown period if nonzero.
                if (Cooldown != 0 && CooldownProgress == 0)
                {
                    CooldownProgress = Cooldown;
                }

                return true;
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
        public static List<Combatant> ComputeTargetsHit(Combatant attacker, List<Combatant> targets)
        {
            List<Combatant> combatantsHit = new List<Combatant>();

            if (attacker.WrappedChar.Stats.HitStatus.Data == Constants.HitStatuses.AlwaysHit)
            {
                combatantsHit.AddRange(targets);
            }
            else
            {
                // Chance to hit.
                if (attacker.WrappedChar.Stats.PercentToHit.Data < 1 ||
                    rng.Next(100) + 1 > attacker.WrappedChar.Stats.PercentToHit.Data)
                {
                    attacker.WrappedChar.RaiseAttackMissed(null);
                    return combatantsHit;
                }

                // Chance for targets to dodge.
                for (int i = 0; i < targets.Count; i++)
                {
                    if (rng.Next(100) < targets[i].WrappedChar.Stats.PercentToDodge.Data)
                    {
                        attacker.WrappedChar.RaiseAttackMissed(targets[i]);
                        targets[i].WrappedChar.RaiseAttackDodged(attacker);
                    }
                    else
                    {
                        combatantsHit.Add(targets[i]);
                    }
                }
            }

            return combatantsHit;
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
        public static Tuple<float[], float[]> ComputeAttackerDamage(Combatant attacker)
        {
            Stats charStats = attacker.WrappedChar.Stats;
            float[] baseDamage = new float[Stats.NUM_TYPES_DAMAGE];
            float[] critDamage = new float[Stats.NUM_TYPES_DAMAGE];

            // Compute damage and critical hits.
            for (int j = 0; j < Stats.NUM_TYPES_DAMAGE; j++)
            {
                if (rng.Next(100) < charStats.PercentToCritHit.Data[j])
                {
                    baseDamage[j] = charStats.MaxDamage.Data[j];
                    critDamage[j] = baseDamage[j] * charStats.CritDamageMultiplier.Data[j];
                    attacker.WrappedChar.RaiseAttackCritHit(j, critDamage[j]);
                }
                else
                {
                    baseDamage[j] = charStats.MinDamage.Data[j] + rng.Next(
                        charStats.MaxDamage.Data[j] - charStats.MinDamage.Data[j]);
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
        public static List<float> ComputeTargetDamage(Combatant target, Tuple<float[], float[]> damage)
        {
            if (target.WrappedChar.Stats.CriticalStatus.Data == Constants.CriticalStatuses.ImmuneToCriticalHits)
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
        public static void ComputeRangeDamageModifier(Combatant attacker, Combatant target, List<float> damage)
        {
            Stats charStats = attacker.WrappedChar.Stats;

            // Multiplies damage by range.
            float deltaX = attacker.WrappedChar.Location.Data.Item1
                - target.WrappedChar.Location.Data.Item1;
            float deltaY = attacker.WrappedChar.Location.Data.Item2
                - target.WrappedChar.Location.Data.Item2;
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
                double multiplier = (1 - bias) * charStats.MinRangeMultiplier.Data
                    + bias * charStats.MaxRangeMultiplier.Data;
                damage.ForEach(num => num *= (float)multiplier);
            }
        }

        /// <summary>
        /// Modifies the damage array in-place to reduce damage based on the
        /// target's damage reduction and then resistance.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ComputeDamageReductions(Combatant target, List<float> damage)
        {
            for (int i = 0; i < damage.Count; i++)
            {
                // Damage reduction.
                damage[i] -= target.WrappedChar.Stats.DamageReduction.Data[i];
                if (damage[i] < 0)
                {
                    damage[i] = 0;
                }

                // Damage resistance.
                float resistance = target.WrappedChar.Stats.DamageResistance.Data[i];
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
        public static void ApplyDamage(Combatant attacker, Combatant target, List<float> damage)
        {
            float damageSum = damage.Sum();

            attacker.WrappedChar.RaiseAttackBeforeDamage(target, damage);
            target.WrappedChar.RaiseAttackBeforeReceiveDamage(attacker, damage);
            target.CurrentHealth.Data -= (int)damageSum;
            attacker.WrappedChar.RaiseAttackAfterDamage(target, damage);
        }

        /// <summary>
        /// Computes and returns knockback damage to the attacker after their
        /// damage reductions.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static float ComputeKnockbackDamage(Combatant attacker, Combatant target, float totalDamage)
        {
            float knockback = totalDamage
                * target.WrappedChar.Stats.PercentKnockback.Data
                + target.WrappedChar.Stats.ConstantKnockback.Data
                - attacker.WrappedChar.Stats.DamageReduction.Data[0];

            if (knockback < 0)
            {
                knockback = 0;
            }
            else if (knockback > 0)
            {
                float resistance = attacker.WrappedChar.Stats.DamageResistance.Data[0];
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
        public static void ApplyKnockbackDamage(Combatant attacker, float knockback)
        {
            attacker.CurrentHealth.Data -= (int)knockback;
        }

        /// <summary>
        /// Computes and returns a tuple containing the recoil magnitude and
        /// the target's new location.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static Tuple<float, Tuple<float, float>> ComputeRecoil(Combatant attacker, Combatant target)
        {
            if (attacker.WrappedChar.Stats.MinRecoil.Data > 0)
            {
                float recoil = attacker.WrappedChar.Stats.MinRecoil.Data + rng.Next((int)(
                    attacker.WrappedChar.Stats.MaxRecoil.Data -
                    attacker.WrappedChar.Stats.MinRecoil.Data));

                double recoilDir = Math.Atan2(
                    target.WrappedChar.Location.Data.Item2 -
                    attacker.WrappedChar.Location.Data.Item2,
                    target.WrappedChar.Location.Data.Item1 -
                    attacker.WrappedChar.Location.Data.Item1);

                var newLocation = new Tuple<float, float>(
                    (float)(recoil * Math.Cos(recoilDir)),
                    (float)(recoil * Math.Sin(recoilDir)));

                return new Tuple<float, Tuple<float, float>>(recoil, newLocation);
            }

            return new Tuple<float, Tuple<float, float>>(0, target.WrappedChar.Location.Data);
        }

        /// <summary>
        /// Applies recoil to the target. Takes an attacker, target, and tuple
        /// containing recoil magnitude and new location.
        /// Triggers RaiseAttackReceiveRecoil, then RaiseAttackRecoil.
        /// Use these functions in custom move actions to avoid rewriting the
        /// logic to handle built-in stats.
        /// </summary>
        public static void ApplyRecoil(Combatant attacker, Combatant target, Tuple<float, Tuple<float, float>> recoil)
        {
            target.WrappedChar.RaiseAttackReceiveRecoil(attacker, recoil.Item1, recoil.Item2);
            attacker.WrappedChar.RaiseAttackRecoil(target, recoil.Item1, recoil.Item2);
            target.WrappedChar.Location.Data = recoil.Item2;
        }
        #endregion
    }
}
