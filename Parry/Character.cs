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
        #region Properties
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
        /// True if the AI can change equipment to suit battle. No effect
        /// if disabled for all in combat.
        /// True by default.
        /// </summary>
        public Stat<bool> CanChangeEquipment
        {
            get;
            set;
        }

        /// <summary>
        /// True if the AI can loot equipment from the ground for battle. No
        /// effect if disabled for all in combat.
        /// True by default.
        /// </summary>
        public Stat<bool> CanLootEquipment
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
        /// targeting and moves. When moves don't specify their own movement
        /// behavior, AI will default to the behavior associated with the
        /// combatant.
        /// </summary>
        public MovementBehavior DefaultMovementBehavior
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
        /// When false, the character will not perform a move.
        /// True by default.
        /// </summary>
        public Stat<bool> CombatMoveEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When true, the character is removed from combat. Don't remove
        /// characters directly, since it can affect the logic of other
        /// characters if e.g. they depend on the number of combatants or
        /// position in the array.
        /// False by default.
        /// </summary>
        public Stat<bool> DoRemoveFromCombat
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
        /// The event raised when this character selects their movement.
        /// First argument is the (x, y) location to move to.
        /// </summary>
        public event Action<Tuple<float, float>> MovementSelected;

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

        #region Constructors
        /// <summary>
        /// Creates a new profile.
        /// </summary>
        public Character()
        {
            TeamID = 0;
            Health = new Stat<int>(100);
            Location = new Stat<Tuple<float, float>>(new Tuple<float, float>(0, 0));
            Stats = new Stats();
            DefaultTargetBehavior = TargetBehavior.Normal;
            MoveSelectBehavior = new MoveSelector();
            DefaultMovementBehavior = new MovementBehavior(true, true);
            CanChangeEquipment = new Stat<bool>(true);
            CanLootEquipment = new Stat<bool>(true);
            CombatMoveEnabled = new Stat<bool>(true);
            DoRemoveFromCombat = new Stat<bool>(true);
        }

        /// <summary>
        /// Creates a deep copy of another profile if isDeepCopy is true, else
        /// shallow.
        /// </summary>
        public Character(Character other, bool isDeepCopy = false)
        {
            if (!isDeepCopy)
            {
                TeamID = other.TeamID;
                Health = other.Health;
                Location = other.Location;
                Stats = other.Stats;
                DefaultTargetBehavior = other.DefaultTargetBehavior;
                MoveSelectBehavior = other.MoveSelectBehavior;
                DefaultMovementBehavior = other.DefaultMovementBehavior;
                CanChangeEquipment = other.CanChangeEquipment;
                CanLootEquipment = other.CanLootEquipment;
                CombatMoveEnabled = other.CombatMoveEnabled;
                DoRemoveFromCombat = other.DoRemoveFromCombat;
            }
            else
            {
                TeamID = other.TeamID;
                Health = new Stat<int>(other.Health.Data);
                Location = new Stat<Tuple<float, float>>(other.Location.Data);
                Stats = new Stats(other.Stats);
                DefaultTargetBehavior = new TargetBehavior(other.DefaultTargetBehavior);
                MoveSelectBehavior = new MoveSelector(other.MoveSelectBehavior);
                DefaultMovementBehavior = new MovementBehavior(other.DefaultMovementBehavior);
                CanChangeEquipment = new Stat<bool>(other.CanChangeEquipment.Data);
                CanLootEquipment = new Stat<bool>(other.CanLootEquipment.Data);
                CombatMoveEnabled = new Stat<bool>(other.CombatMoveEnabled.Data);
                DoRemoveFromCombat = new Stat<bool>(other.DoRemoveFromCombat.Data);
            }
        }
        #endregion

        #region Event-raising Methods
        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseCharacterAdded()
        {
            CharacterAdded?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseCharacterRemoved()
        {
            CharacterRemoved?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseTurnStart()
        {
            TurnStart?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseTurnEnd()
        {
            TurnEnd?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseMoveSelected(Move move)
        {
            MoveSelected?.Invoke(move);
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseTargetsSelected(List<Combatant> targets)
        {
            TargetsSelected?.Invoke(targets);
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseMovementSelected(Tuple<float, float> location)
        {
            MovementSelected?.Invoke(location);
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseBeforeMove()
        {
            BeforeMove?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseAfterMove()
        {
            AfterMove?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseEnterZone(Geometry zone)
        {
            EnterZone?.Invoke(zone);
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseExitZone(Geometry zone)
        {
            ExitZone?.Invoke(zone);
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseDetected()
        {
            Detected?.Invoke();
        }

        /// <summary>
        /// Raises the event named in the function from anywhere.
        /// </summary>
        public void RaiseTargeted()
        {
            Targeted?.Invoke();
        }
        #endregion
    }
}