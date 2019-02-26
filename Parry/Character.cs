using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry
{
    /// <summary>
    /// Characters store collections and related user-defined stats so they're
    /// easily accessible when defining combat logic.
    /// </summary>
    public class Character
    {
        #region Properties
        /// <summary>
        /// A unique identifier between unrelated characters, though deep
        /// copies of this character may share the same guid so they can be
        /// directly associated.
        /// </summary>
        private static long id = 0;

        /// <summary>
        /// A unique character ID, shared only by deep clones that copy guids.
        /// </summary>
        public readonly long Id;

        /// <summary>
        /// Values used in determining behavior in combat during moves,
        /// including built-in stats for common concepts, like knockback.
        /// </summary>
        public CombatStats CombatStats
        {
            get;
            set;
        }

        /// <summary>
        /// Values related to a character's state in combat which cannot be
        /// considered a combat stat, such as location or current health.
        /// </summary>
        public CharacterStats CharStats
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
        /// default to the behavior associated with the character.
        /// Default value is <see cref="TargetBehavior.Normal"/>.
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
        /// the character.
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
        /// the character.
        /// </summary>
        public MovementBehavior DefaultMovementAfterBehavior
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
        public event Action<List<Character>> TargetsSelected;

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
        /// before any logic is executed. Triggers separately for every move,
        /// even if two moves target the same player.
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
        /// First argument: The attacking character.
        /// </summary>
        public event Action<Character> AttackDodged;

        /// <summary>
        /// The event raised when a character's attack misses or is dodged by
        /// a target. This is intended to be raised by a move action, and will
        /// only be raised if implemented.
        /// First argument: The target that dodged the attack, or null if the
        /// character missed all targets by failing their chance to hit.
        /// </summary>
        public event Action<Character> AttackMissed;

        /// <summary>
        /// The event raised before a character deals damage to a target. This
        /// is intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The target to receive the damage.
        /// Second argument: The amount of damage for each type of damage.
        /// </summary>
        public event Action<Character, List<float>> AttackBeforeDamage;

        /// <summary>
        /// The event raised before a character takes damage. This is intended
        /// to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The character dealing the damage.
        /// Second argument: The amount of damage for each type of damage.
        /// </summary>
        public event Action<Character, List<float>> AttackBeforeReceiveDamage;

        /// <summary>
        /// The event raised after a character deals damage to a target. This
        /// is intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The target that received the damage.
        /// Second argument: The amount of damage for each type of damage.
        /// </summary>
        public event Action<Character, List<float>> AttackAfterDamage;

        /// <summary>
        /// The event raised before a character deals knockback damage. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The attacker.
        /// Second argument: The targeted character.
        /// Third argument: The amount of damage to deal.
        /// </summary>
        public event Action<Character, Character, float> AttackKnockback;

        /// <summary>
        /// The event raised before a character knocks back a target. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The target.
        /// Second argument: The magnitude of recoil.
        /// Third argument: The new location of the target.
        /// </summary>
        public event Action<Character, float, Tuple<float, float>> AttackRecoil;

        /// <summary>
        /// The event raised before a character is knocked back. This is
        /// intended to be raised by a move action, and will only be raised if
        /// implemented.
        /// First argument: The attacker.
        /// Second argument: The magnitude of recoil.
        /// Third argument: The new location.
        /// </summary>
        public event Action<Character, float, Tuple<float, float>> AttackReceiveRecoil;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new profile.
        /// </summary>
        public Character()
        {
            Id = id++;
            TeamID = 0;
            CharStats = new CharacterStats();
            CombatStats = new CombatStats();
            DefaultTargetBehavior = TargetBehavior.Normal;
            MoveSelectBehavior = new MoveSelector();
            DefaultMovementBeforeBehavior = new MovementBehavior(MovementBehavior.MotionOrigin.Nearest, MovementBehavior.Motion.Towards);
            DefaultMovementAfterBehavior = new MovementBehavior(MovementBehavior.MotionOrigin.Nearest, MovementBehavior.Motion.Towards);
            CombatMoveEnabled = new Stat<bool>(true);
            CombatMoveSelectEnabled = new Stat<bool>(true);
            CombatTargetingEnabled = new Stat<bool>(true);
            CombatMovementBeforeEnabled = new Stat<bool>(true);
            CombatMovementAfterEnabled = new Stat<bool>(true);
        }

        /// <summary>
        /// Creates a shallow or deep copy of another character, generating a
        /// new id if desired. Having characters with the same id allows for an
        /// efficent way of identifying related clones, as with combat history.
        /// Deep copies have no snapshots.
        /// </summary>
        public Character(Character other, bool isDeepCopy = false, bool newId = false)
        {
            if (!isDeepCopy)
            {
                Id = (newId) ? id++ : other.Id;
                TeamID = other.TeamID;
                CharStats = other.CharStats;
                CombatStats = other.CombatStats;
                DefaultTargetBehavior = other.DefaultTargetBehavior;
                MoveSelectBehavior = other.MoveSelectBehavior;
                DefaultMovementBeforeBehavior = other.DefaultMovementBeforeBehavior;
                DefaultMovementAfterBehavior = other.DefaultMovementAfterBehavior;
                CombatMoveEnabled = other.CombatMoveEnabled;
                CombatMoveSelectEnabled = other.CombatMoveSelectEnabled;
                CombatTargetingEnabled = other.CombatTargetingEnabled;
                CombatMovementBeforeEnabled = other.CombatMovementBeforeEnabled;
                CombatMovementAfterEnabled = other.CombatMovementAfterEnabled;
            }
            else
            {
                Id = (newId) ? id++ : other.Id;
                TeamID = other.TeamID;
                CharStats = new CharacterStats(other.CharStats);
                CombatStats = new CombatStats(other.CombatStats);
                DefaultTargetBehavior = new TargetBehavior(other.DefaultTargetBehavior);
                MoveSelectBehavior = new MoveSelector(other.MoveSelectBehavior);
                DefaultMovementBeforeBehavior = new MovementBehavior(other.DefaultMovementBeforeBehavior);
                DefaultMovementAfterBehavior = new MovementBehavior(other.DefaultMovementAfterBehavior);
                CombatMoveEnabled = new Stat<bool>(other.CombatMoveEnabled.RawData);
                CombatMoveSelectEnabled = new Stat<bool>(other.CombatMoveSelectEnabled.RawData);
                CombatTargetingEnabled = new Stat<bool>(other.CombatTargetingEnabled.RawData);
                CombatMovementBeforeEnabled = new Stat<bool>(other.CombatMovementBeforeEnabled.RawData);
                CombatMovementAfterEnabled = new Stat<bool>(other.CombatMovementAfterEnabled.RawData);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// If targets have been computed, returns a copy of the appropriate set
        /// of computed targets. The chosen move's targeting behavior is
        /// preferred to the default targeting behavior, and OverrideTargets is
        /// preferred to Targets. Returns a list of target lists, one for each
        /// move. If no suitable list is found, returns an empty list. 
        /// </summary>
        public List<List<Character>> GetTargets()
        {
            List<List<Character>> targetLists = new List<List<Character>>();

            for (int i = 0; i < MoveSelectBehavior.ChosenMoves.Count; i++)
            {
                if (MoveSelectBehavior.ChosenMoves[i]?.TargetBehavior?.OverrideTargets != null)
                {
                    targetLists.Add(new List<Character>(MoveSelectBehavior.ChosenMoves[i].TargetBehavior.OverrideTargets));
                }

                if (MoveSelectBehavior.ChosenMoves[i]?.TargetBehavior?.Targets != null)
                {
                    targetLists.Add(new List<Character>(MoveSelectBehavior.ChosenMoves[i]?.TargetBehavior?.Targets));
                }

                if (DefaultTargetBehavior?.OverrideTargets != null)
                {
                    targetLists.Add(new List<Character>(DefaultTargetBehavior.OverrideTargets));
                }

                if (DefaultTargetBehavior?.Targets != null)
                {
                    targetLists.Add(new List<Character>(DefaultTargetBehavior.Targets));
                }

                targetLists.Add(new List<Character>());
            }

            return targetLists;
        }

        /// <summary>
        /// If targets have been computed, returns a flat list of all unique
        /// targets. Use <see cref="GetTargets"/> to preserve information about
        /// which move each set of targets belongs to. Use this to check e.g.
        /// if a character is targeted.
        /// </summary>
        public List<Character> GetTargetsFlat()
        {
            List<List<Character>> targets = GetTargets();
            List<Character> targetsFlat = new List<Character>();

            for (int i = 0; i < targets.Count; i++)
            {
                targetsFlat.AddRange(targets[i]);
            }

            return targetsFlat.Distinct().ToList();
        }
        #endregion

        #region Event-raising Methods
        public void RaiseAttackCritHit(int index, float damage)
        {
            AttackCritHit?.Invoke(index, damage);
        }

        public void RaiseAttackDodged(Character assailant)
        {
            AttackDodged?.Invoke(assailant);
        }

        public void RaiseAttackMissed(Character targetMissed)
        {
            AttackMissed?.Invoke(targetMissed);
        }

        public void RaiseAttackBeforeDamage(Character targetHit, List<float> damage)
        {
            AttackBeforeDamage?.Invoke(targetHit, damage);
        }

        public void RaiseAttackBeforeReceiveDamage(Character attacker, List<float> damage)
        {
            AttackBeforeReceiveDamage?.Invoke(attacker, damage);
        }

        public void RaiseAttackAfterDamage(Character targetHit, List<float> damage)
        {
            AttackAfterDamage?.Invoke(targetHit, damage);
        }

        public void RaiseAttackKnockback(Character attacker, Character target, float damage)
        {
            AttackKnockback?.Invoke(attacker, target, damage);
        }

        public void RaiseAttackRecoil(Character target, float recoil, Tuple<float, float> newLocation)
        {
            AttackRecoil?.Invoke(target, recoil, newLocation);
        }

        public void RaiseAttackReceiveRecoil(Character attacker, float recoil, Tuple<float, float> newLocation)
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

        public void RaiseTargetsSelected(List<Character> targets)
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