using System;
using System.Collections.Generic;
using Parry.Combat;

namespace Parry
{
    /// <summary>
    /// Characters store collections and related user-defined stats so they're
    /// easily accessible when defining combat logic.
    /// </summary>
    public class Character
    {
        private static long Guid = 0;

        #region Properties
        /// <summary>
        /// A unique character ID, shared only by deep clones that copy guids.
        /// </summary>
        public readonly long Id;

        /// <summary>
        /// In combat, this is the character's total possible health.
        /// Default value is 100.
        /// </summary>
        public Stat<int> Health
        {
            get;
            set;
        }

        /// <summary>
        /// The character's position in combat, if you make use of
        /// distances.
        /// Default location is 0, 0.
        /// </summary>
        public Stat<Tuple<float, float>> Location
        {
            get;
            set;
        }

        /// <summary>
        /// Contains default values for all character stats.
        /// </summary>
        public Stats Stats
        {
            get;
            set;
        }

        /// <summary>
        /// In combat, characters with the same team ID are on the same team.
        /// </summary>
        public int TeamID
        {
            get;
            set;
        }

        /// <summary>
        /// Allows the AI to determine which move should be selected based on
        /// combat history and available moves.
        /// </summary>
        public MoveSelector MoveSelectBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// When false, the character will not select motives or moves.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatMoveSelectEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Allows the AI to choose targets based on weighted criteria.
        /// When moves don't specify their own targeting behavior, AI will
        /// default to the behavior associated with the combatant.
        /// </summary>
        public TargetBehavior DefaultTargetBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// When false, the character will have no targets and skip targeting.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatTargetingEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Allows the AI to determine their physical movement separate from
        /// targeting and moves. When moves don't specify their own pre-move
        /// movement behavior, AI will default to the behavior associated with
        /// the combatant.
        /// </summary>
        public MovementBehavior DefaultMovementBeforeBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// Allows the AI to determine their physical movement separate from
        /// targeting and moves. When moves don't specify their own post-move
        /// movement behavior, AI will default to the behavior associated with
        /// the combatant.
        /// </summary>
        public MovementBehavior DefaultMovementAfterBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// When false, the character will not move and skip movement AI.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatMovementEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When false, the character will skip movement AI and not move for
        /// the movement opportunity that occurs before the move is performed.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatMovementBeforeEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When false, the character will skip movement AI and not move for
        /// the movement opportunity that occurs after the move is performed.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatMovementAfterEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When false, the character will not perform a move.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatMoveEnabled
        {
            get;
            set;
        }
        #endregion

        #region Events
        /// <summary>
        /// The event raised when this character joins combat.
        /// </summary>
        public event Action CharacterAdded;

        /// <summary>
        /// The event raised when this character leaves combat.
        /// </summary>
        public event Action CharacterRemoved;

        /// <summary>
        /// The event raised just after this character's turn starts.
        /// </summary>
        public event Action TurnStart;

        /// <summary>
        /// The event raised just before this character's turn ends.
        /// </summary>
        public event Action TurnEnd;

        /// <summary>
        /// The event raised when this character selects a move.
        /// First argument is the move the character selected.
        /// </summary>
        public event Action<Move> MoveSelected;

        /// <summary>
        /// The event raised when this character selects targets.
        /// First argument is the list of targets selected.
        /// </summary>
        public event Action<List<Combatant>> TargetsSelected;

        /// <summary>
        /// The event raised when this character selects their pre-move movement.
        /// First argument is the (x, y) location to move to.
        /// </summary>
        public event Action<Tuple<float, float>> MovementBeforeSelected;

        /// <summary>
        /// The event raised when this character selects their post-move movement.
        /// First argument is the (x, y) location to move to.
        /// </summary>
        public event Action<Tuple<float, float>> MovementAfterSelected;

        /// <summary>
        /// The event raised just before this character executes
        /// their move, which is the last step of their turn.
        /// </summary>
        public event Action BeforeMove;

        /// <summary>
        /// The event raised just after this character executes
        /// their move, which is the last step of their turn.
        /// </summary>
        public event Action AfterMove;

        /// <summary>
        /// The event raised when this character enters a zone.
        /// First argument is the zone which was entered.
        /// </summary>
        public event Action<Geometry> EnterZone;

        /// <summary>
        /// The event raised when this character exits a zone.
        /// First argument is the zone which was exited.
        /// </summary>
        public event Action<Geometry> ExitZone;

        /// <summary>
        /// The event raised when the character becomes known to another.
        /// </summary>
        public event Action Detected;

        /// <summary>
        /// The event raised when the character becomes the target of an attack,
        /// before any logic is executed.
        /// </summary>
        public event Action Targeted;
        #endregion

        #region Events Called by Actions
        /// <summary>
        /// The event raised when a character's attack succeeds against at
        /// least one target and is critical. This is intended to be raised by
        /// a move action, and will only be raised if implemented.
        /// First argument: the index corresponding to the type of damage.
        /// Second argument: The damage before it's applied to targets.
        /// </summary>
        public event Action<int, float> AttackCritHit;

        /// <summary>
        /// The event raised when this character dodges an attacker. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The attacking combatant.
        /// </summary>
        public event Action<Combatant> AttackDodged;

        /// <summary>
        /// The event raised when a character's attack misses or is dodged by
        /// a target. This is intended to be raised by a move action, and will
        /// only be raised if implemented.
        /// First argument: The target that dodged the attack, or null if the
        /// character missed all targets by failing their chance to hit.
        /// </summary>
        public event Action<Combatant> AttackMissed;

        /// <summary>
        /// The event raised before a character deals damage to a target. This
        /// is intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The target to receive the damage.
        /// Second argument: The amount of damage for each type of damage.
        /// </summary>
        public event Action<Combatant, List<float>> AttackBeforeDamage;

        /// <summary>
        /// The event raised before a character takes damage. This is intended
        /// to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The combatant dealing the damage.
        /// Second argument: The amount of damage for each type of damage.
        /// </summary>
        public event Action<Combatant, List<float>> AttackBeforeReceiveDamage;

        /// <summary>
        /// The event raised after a character deals damage to a target. This
        /// is intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The target that received the damage.
        /// Second argument: The amount of damage for each type of damage.
        /// </summary>
        public event Action<Combatant, List<float>> AttackAfterDamage;

        /// <summary>
        /// The event raised before a character deals knockback damage. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The attacker.
        /// Second argument: The targeted combatant.
        /// Third argument: The amount of damage to deal.
        /// </summary>
        public event Action<Combatant, Combatant, float> AttackKnockback;

        /// <summary>
        /// The event raised before a character knocks back a target. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The target.
        /// Second argument: The magnitude of recoil.
        /// Third argument: The new location of the target.
        /// </summary>
        public event Action<Combatant, float, Tuple<float, float>> AttackRecoil;

        /// <summary>
        /// The event raised before a character is knocked back. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The attacker.
        /// Second argument: The magnitude of recoil.
        /// Third argument: The new location.
        /// </summary>
        public event Action<Combatant, float, Tuple<float, float>> AttackReceiveRecoil;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new profile.
        /// </summary>
        public Character()
        {
            Id = Guid++;
            TeamID = 0;
            Health = new Stat<int>(100);
            Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(0, 0));
            Stats = new Stats();
            DefaultTargetBehavior = TargetBehavior.Normal;
            MoveSelectBehavior = new MoveSelector();
            DefaultMovementBeforeBehavior = new MovementBehavior(MovementBehavior.MotionOrigin.Nearest, MovementBehavior.Motion.Towards);
            DefaultMovementAfterBehavior = new MovementBehavior(MovementBehavior.MotionOrigin.Nearest, MovementBehavior.Motion.Towards);
            CombatMoveEnabled = new Stat<bool>(true);
            CombatMoveSelectEnabled = new Stat<bool>(true);
            CombatTargetingEnabled = new Stat<bool>(true);
            CombatMovementEnabled = new Stat<bool>(true);
            CombatMovementBeforeEnabled = new Stat<bool>(true);
            CombatMovementAfterEnabled = new Stat<bool>(true);
        }

        /// <summary>
        /// Creates a shallow or deep copy of another character, generating a
        /// new id if desired. Having characters with the same id allows for an
        /// efficent way of identifying related clones, as with combat history.
        /// </summary>
        public Character(Character other, bool isDeepCopy = false, bool newId = false)
        {
            if (!isDeepCopy)
            {
                Id = (newId) ? Guid++ : other.Id;
                TeamID = other.TeamID;
                Health = other.Health;
                Location = other.Location;
                Stats = other.Stats;
                DefaultTargetBehavior = other.DefaultTargetBehavior;
                MoveSelectBehavior = other.MoveSelectBehavior;
                DefaultMovementBeforeBehavior = other.DefaultMovementBeforeBehavior;
                DefaultMovementAfterBehavior = other.DefaultMovementAfterBehavior;
                CombatMoveEnabled = other.CombatMoveEnabled;
                CombatMoveSelectEnabled = other.CombatMoveSelectEnabled;
                CombatTargetingEnabled = other.CombatTargetingEnabled;
                CombatMovementEnabled = other.CombatMovementEnabled;
                CombatMovementBeforeEnabled = other.CombatMovementBeforeEnabled;
                CombatMovementAfterEnabled = other.CombatMovementAfterEnabled;
            }
            else
            {
                Id = (newId) ? Guid++ : other.Id;
                TeamID = other.TeamID;
                Health = new Stat<int>(other.Health.RawData);
                Location = new Stat<Tuple<float, float>>(other.Location.RawData);
                Stats = new Stats(other.Stats);
                DefaultTargetBehavior = new TargetBehavior(other.DefaultTargetBehavior);
                MoveSelectBehavior = new MoveSelector(other.MoveSelectBehavior);
                DefaultMovementBeforeBehavior = new MovementBehavior(other.DefaultMovementBeforeBehavior);
                DefaultMovementAfterBehavior = new MovementBehavior(other.DefaultMovementAfterBehavior);
                CombatMoveEnabled = new Stat<bool>(other.CombatMoveEnabled.RawData);
                CombatMoveSelectEnabled = new Stat<bool>(other.CombatMoveSelectEnabled.RawData);
                CombatTargetingEnabled = new Stat<bool>(other.CombatTargetingEnabled.RawData);
                CombatMovementEnabled = new Stat<bool>(other.CombatMovementEnabled.RawData);
                CombatMovementBeforeEnabled = new Stat<bool>(other.CombatMovementBeforeEnabled.RawData);
                CombatMovementAfterEnabled = new Stat<bool>(other.CombatMovementAfterEnabled.RawData);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// If targets have been computed, returns the appropriate set of
        /// computed targets. The chosen move's targeting behavior is preferred
        /// to the default targeting behavior, and OverrideTargets is preferred
        /// to Targets. If no suitable list is found, returns an empty list.
        /// </summary>
        public List<Combatant> GetTargets()
        {
            if (MoveSelectBehavior.ChosenMove?.TargetBehavior?.OverrideTargets != null)
            {
                return MoveSelectBehavior.ChosenMove.TargetBehavior.OverrideTargets;
            }

            if (MoveSelectBehavior.ChosenMove?.TargetBehavior?.Targets != null)
            {
                return MoveSelectBehavior.ChosenMove?.TargetBehavior?.Targets;
            }

            if (DefaultTargetBehavior?.OverrideTargets != null)
            {
                return DefaultTargetBehavior.OverrideTargets;
            }

            if (DefaultTargetBehavior?.Targets != null)
            {
                return DefaultTargetBehavior.Targets;
            }

            return new List<Combatant>();
        }
        #endregion

        #region Event-raising Methods
        public void RaiseAttackCritHit(int index, float damage)
        {
            AttackCritHit?.Invoke(index, damage);
        }

        public void RaiseAttackDodged(Combatant assailant)
        {
            AttackDodged?.Invoke(assailant);
        }

        public void RaiseAttackMissed(Combatant targetMissed)
        {
            AttackMissed?.Invoke(targetMissed);
        }

        public void RaiseAttackBeforeDamage(Combatant targetHit, List<float> damage)
        {
            AttackBeforeDamage?.Invoke(targetHit, damage);
        }

        public void RaiseAttackBeforeReceiveDamage(Combatant attacker, List<float> damage)
        {
            AttackBeforeReceiveDamage?.Invoke(attacker, damage);
        }

        public void RaiseAttackAfterDamage(Combatant targetHit, List<float> damage)
        {
            AttackAfterDamage?.Invoke(targetHit, damage);
        }

        public void RaiseAttackKnockback(Combatant attacker, Combatant target, float damage)
        {
            AttackKnockback?.Invoke(attacker, target, damage);
        }

        public void RaiseAttackRecoil(Combatant target, float recoil, Tuple<float, float> newLocation)
        {
            AttackRecoil?.Invoke(target, recoil, newLocation);
        }

        public void RaiseAttackReceiveRecoil(Combatant attacker, float recoil, Tuple<float, float> newLocation)
        {
            AttackReceiveRecoil?.Invoke(attacker, recoil, newLocation);
        }

        public void RaiseCharacterAdded()
        {
            CharacterAdded?.Invoke();
        }

        public void RaiseCharacterRemoved()
        {
            CharacterRemoved?.Invoke();
        }

        public void RaiseTurnStart()
        {
            TurnStart?.Invoke();
        }

        public void RaiseTurnEnd()
        {
            TurnEnd?.Invoke();
        }

        public void RaiseMoveSelected(Move move)
        {
            MoveSelected?.Invoke(move);
        }

        public void RaiseTargetsSelected(List<Combatant> targets)
        {
            TargetsSelected?.Invoke(targets);
        }

        public void RaiseMovementBeforeSelected(Tuple<float, float> location)
        {
            MovementBeforeSelected?.Invoke(location);
        }

        public void RaiseMovementAfterSelected(Tuple<float, float> location)
        {
            MovementAfterSelected?.Invoke(location);
        }

        public void RaiseBeforeMove()
        {
            BeforeMove?.Invoke();
        }

        public void RaiseAfterMove()
        {
            AfterMove?.Invoke();
        }

        public void RaiseEnterZone(Geometry zone)
        {
            EnterZone?.Invoke(zone);
        }

        public void RaiseExitZone(Geometry zone)
        {
            ExitZone?.Invoke(zone);
        }

        public void RaiseDetected()
        {
            Detected?.Invoke();
        }

        public void RaiseTargeted()
        {
            Targeted?.Invoke();
        }
        #endregion
    }
}