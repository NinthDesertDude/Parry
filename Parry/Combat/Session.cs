using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry.Combat
{
    /// <summary>
    /// Automates combat between any number of combatants.
    /// </summary>
    public class Session
    {
        #region Static Variables
        /// <summary>
        /// Used with preset logic.
        /// </summary>
        private static Random rng;
        #endregion

        #region Private Variables
        /// <summary>
        /// Lists all combatants for the current round (most recent) and
        /// previous rounds. Position 0 is the state before combat. Combatants
        /// are organized by combat speed, with simultaneous combatants
        /// organized by original relative position in the list.
        /// </summary>
        public List<List<Combatant>> Combatants
        {
            private set;
            get;
        }

        /// <summary>
        /// Stores current combatants in turn order.
        /// </summary>
        private List<Combatant> combatantsInOrder;

        /// <summary>
        /// The combatant whose turn is currently in effect.
        /// </summary>
        private Combatant combatant;

        /// <summary>
        /// These characters are removed after finishing any character's moves.
        /// </summary>
        private List<Character> combatantsToRemove;

        /// <summary>
        /// These characters are added after finishing any character's moves.
        /// </summary>
        private List<Combatant> combatantsToAdd;

        /// <summary>
        /// Tracks all walls and zones on the battlefield.
        /// </summary>
        private List<Geometry> geometry;
        #endregion

        #region Public Variables
        /// <summary>
        /// When true, instead of stopping combat when there's only one team
        /// left, each remaining combatant's team ID will be set to a unique
        /// value starting at 0 and counting up. Then combat will continue
        /// until there is only one character left or it's manually stopped.
        /// False by default.
        /// </summary>
        public bool FreeForAllEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When true, the minimum speed among all combatants is subtracted
        /// from each combatant's speed last round, and the remainder is added
        /// to the next round.
        /// False by default.
        /// </summary>
        public bool SpeedCarriesOver
        {
            get;
            set;
        }

        /// <summary>
        /// After each combatant executes their move, they are reinserted into
        /// combat if they're remaining speed is greater than the minimum speed
        /// among combatants that round.
        /// False by default.
        /// </summary>
        public bool ExtraTurnsEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When enabled, combatants with the same speed will take their turns
        /// in order without flushing the add and remove queues between. This
        /// allows e.g. two combatants to deal lethal damage to each other.
        /// False by default.
        /// </summary>
        public bool SimultaneousTurnsEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// The number of previous rounds to store in memory, which enables
        /// users to create AI behavior based on previous combatant behaviors
        /// and performances. Can't be negative.
        /// 10 by default.
        /// </summary>
        public int RoundHistoryLimit
        {
            get;
            set;
        }
        #endregion

        #region Events
        /// <summary>
        /// The event raised when a new round begins.
        /// </summary>
        public event Action RoundStarting;

        /// <summary>
        /// The event raised after a round ends.
        /// </summary>
        public event Action RoundEnding;

        /// <summary>
        /// The event raised just after a character's turn starts.
        /// </summary>
        public event Action<Combatant> TurnStart;

        /// <summary>
        /// The event raised just before a character's turn ends.
        /// </summary>
        public event Action<Combatant> TurnEnd;

        /// <summary>
        /// The event raised when a character selects a move.
        /// First argument is the move the active character selected.
        /// </summary>
        public event Action<Move> MoveSelected;

        /// <summary>
        /// The event raised when a character selects their targets.
        /// First argument is the list of targets selected.
        /// </summary>
        public event Action<List<Combatant>> TargetsSelected;

        /// <summary>
        /// The event raised after a character selects their movement before
        /// their turn.
        /// First argument is the (x,y) location moved from.
        /// </summary>
        public event Action<Tuple<float, float>> MovementBeforeSelected;

        /// <summary>
        /// The event raised after a character selects their movement after
        /// their turn.
        /// First argument is the (x,y) location moved from.
        /// </summary>
        public event Action<Tuple<float, float>> MovementAfterSelected;

        /// <summary>
        /// The event raised just before the active character executes
        /// their move, which is the last step of their turn.
        /// </summary>
        public event Action BeforeMove;

        /// <summary>
        /// The event raised just after the active character executes
        /// their move, which is the last step of their turn.
        /// </summary>
        public event Action AfterMove;

        /// <summary>
        /// The event raised after a character is added to combat.
        /// First argument is the combatant that was added.
        /// </summary>
        public event Action<Combatant> CharacterAdded;

        /// <summary>
        /// The event raised after a character is removed from combat.
        /// First argument is the character that was removed.
        /// </summary>
        public event Action<Character> CharacterRemoved;

        /// <summary>
        /// The event raised when a character enters a zone.
        /// First argument is the zone which was entered.
        /// Second argument is the character that entered the zone.
        /// </summary>
        public event Action<Geometry, Combatant> ZoneEntered;

        /// <summary>
        /// The event raised when a character exits a zone.
        /// First argument is the zone which was exited.
        /// Second argument is the character that exited the zone.
        /// </summary>
        public event Action<Geometry, Combatant> ZoneExited;
        #endregion

        #region Constructors
        /// <summary>
        /// Sets static variables.
        /// </summary>
        static Session()
        {
            rng = new Random();
        }

        /// <summary>
        /// Initializes a combat module to handle turn-based combat logic.
        /// </summary>
        public Session()
        {
            ResetSession();
            FreeForAllEnabled = false;
            SpeedCarriesOver = false;
            ExtraTurnsEnabled = false;
            SimultaneousTurnsEnabled = false;
            RoundHistoryLimit = 10;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Returns combatants in turn order, or an empty list if combat has
        /// not been initialized yet.
        /// </summary>
        private List<Combatant> GetTurnOrder()
        {
            if (Combatants.Count == 0)
            {
                return new List<Combatant>();
            }

            Combatants[0].ForEach(o =>
            {
                o.Speed = o.WrappedChar.Stats.MoveSpeed.Data
                    + o.WrappedChar.MoveSelectBehavior.Perform(Combatants).MoveSpeed;
            });

            int minSpeed = Combatants[0].Min(o => o.Speed);
            Combatants[0].ForEach(o =>
            {
                if (SpeedCarriesOver)
                {
                    o.Speed += o.AccumulatedSpeed - minSpeed;
                    o.AccumulatedSpeed = o.Speed;
                }
            });

            return Combatants[0].OrderByDescending(o => o.Speed).ToList();
        }

        /// <summary>
        /// Executes move logic for the active combatant.
        /// This is the fourth and last step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private void PerformMove(
            List<Combatant> targets,
            Move chosenMove)
        {
            chosenMove.Perform(combatant, Combatants[0], targets);
        }

        /// <summary>
        /// Executes pre-move movement logic for the active combatant.
        /// This is the second step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private Tuple<float, float> PerformMovementBefore(Move chosenMove)
        {
            if (!combatant.WrappedChar.CombatMovementEnabled.Data ||
                !combatant.WrappedChar.CombatMovementBeforeEnabled.Data)
            {
                return combatant.WrappedChar.Location.Data;
            }

            if (chosenMove.MovementBeforeBehavior == null)
            {
                return combatant.WrappedChar.DefaultMovementBeforeBehavior.Perform(Combatants[0], combatant, chosenMove);
            }

            return chosenMove.MovementBeforeBehavior.Perform(Combatants[0], combatant, chosenMove);
        }

        /// <summary>
        /// Executes post-move movement logic for the active combatant.
        /// This is the third step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private Tuple<float, float> PerformMovementAfter(Move chosenMove)
        {
            if (!combatant.WrappedChar.CombatMovementEnabled.Data ||
                !combatant.WrappedChar.CombatMovementAfterEnabled.Data)
            {
                return combatant.WrappedChar.Location.Data;
            }

            if (chosenMove.MovementAfterBehavior == null)
            {
                return combatant.WrappedChar.DefaultMovementAfterBehavior.Perform(Combatants[0], combatant, chosenMove);
            }

            return chosenMove.MovementAfterBehavior.Perform(Combatants[0], combatant, chosenMove);
        }

        /// <summary>
        /// Executes move selection logic for the active combatant.
        /// This is the first step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private Move PerformMoveSelect()
        {
            return combatant.WrappedChar.MoveSelectBehavior.Perform(Combatants);
        }

        /// <summary>
        /// Executes targeting logic for the active combatant.
        /// This is the third step in executing a turn.
        /// </summary>
        private List<Combatant> PerformTargeting(Move chosenMove)
        {
            if (!combatant.WrappedChar.CombatTargetingEnabled.Data)
            {
                return new List<Combatant>();
            }

            if (chosenMove.TargetBehavior == null)
            {
                return combatant.WrappedChar.DefaultTargetBehavior.Perform(Combatants, combatant);
            }

            return chosenMove.TargetBehavior.Perform(Combatants, combatant);
        }

        /// <summary>
        /// Recomputes targets after adjusting all distance-based factors. This
        /// is not a discrete step in a turn.
        /// </summary>
        private List<Combatant> PerformAdjustTargets(Move chosenMove)
        {
            if (!combatant.WrappedChar.CombatTargetingEnabled.Data)
            {
                return new List<Combatant>();
            }

            if (chosenMove.TargetBehavior == null)
            {
                return combatant.WrappedChar.DefaultTargetBehavior.PostMovePerform(Combatants, combatant);
            }

            return chosenMove.TargetBehavior.PostMovePerform(Combatants, combatant);
        }

        /// <summary>
        /// The function used to subscribe to any geometry's zone entered
        /// event, which is done so the general zone event can be triggered.
        /// In order to unsubscribe cleanly, the function is encapsulated
        /// here.
        /// </summary>
        private void SubscribeGeometryZoneEntered(
            Geometry theGeometry,
            Combatant combatant)
        {
            ZoneEntered?.Invoke(theGeometry, combatant);
        }

        /// <summary>
        /// The function used to subscribe to any geometry's zone exited
        /// event, which is done so the general zone event can be triggered.
        /// In order to unsubscribe cleanly, the function is encapsulated
        /// here.
        /// </summary>
        private void SubscribeGeometryZoneExited(
            Geometry theGeometry,
            Combatant combatant)
        {
            ZoneExited?.Invoke(theGeometry, combatant);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Queues to add the given combatant to combat. They are added as soon
        /// as the next turn begins. Takes off the removal queue.
        /// </summary>
        /// <param name="combatant">
        /// The combatant to be added to combat.
        /// </param>
        public void AddCharacter(Combatant combatant)
        {
            combatantsToAdd.Add(combatant);
            combatantsToRemove.Remove(combatant.WrappedChar);
        }

        /// <summary>
        /// Queues to add the given character to combat. They are added as soon
        /// as the next turn begins. Takes off the removal queue.
        /// </summary>
        /// <param name="newChar">
        /// The character to be added to combat.
        /// </param>
        public void AddCharacter(Character newChar)
        {
            combatantsToAdd.Add(new Combatant(newChar));
            combatantsToRemove.Remove(newChar);
        }

        /// <summary>
        /// Adds a piece of geometry to the list of geometry included in
        /// combat. It may function as a wall or zone.
        /// </summary>
        /// <param name="newGeometry">
        /// The geometry to be added.
        /// </param>
        public void AddGeometry(Geometry newGeometry)
        {
            geometry.Add(newGeometry);
            newGeometry.ZoneEntered += SubscribeGeometryZoneEntered;
            newGeometry.ZoneExited += SubscribeGeometryZoneExited;
        }

        /// <summary>
        /// Starts combat. Call this function after adding all characters that
        /// will participate in the first round, and before any functions that
        /// deal with turns and rounds. Unlike resetting the session, this does
        /// not clear the geometry or combatants to add (which is how to add
        /// characters at the start of the combat).
        /// </summary>
        public void StartSession()
        {
            combatant = null;
            Combatants.Clear();
            Combatants.Add(new List<Combatant>());
            
            for (int i = 0; i < combatantsToAdd.Count; i++)
            {
                Combatants[0].Add(combatantsToAdd[i]);
                CharacterAdded?.Invoke(combatantsToAdd[i]);
                combatantsToAdd[i].WrappedChar.RaiseCharacterAdded();
            }

            combatantsToRemove.Clear();
            combatantsToAdd.Clear();
            combatantsInOrder = GetTurnOrder();
        }

        /// <summary>
        /// Plays all remaining turns in the round. Advances the round if
        /// doAdvance is true. Returns true unless there isn't another round
        /// and combat ends, or there's no turns left in the round and
        /// doAdvance is false.
        /// </summary>
        public void ExecuteRound()
        {
            RoundStarting?.Invoke();

            while (NextTurn())
            {
                ExecuteTurn();
            }

            RoundEnding?.Invoke();
            combatantsInOrder.Clear();

            //Adds to history.
            if (Combatants.Count > 0)
            {
                List<Combatant> previousRound = new List<Combatant>();
                for (int i = 0; i < Combatants[0].Count; i++)
                {
                    previousRound.Add(new Combatant(new Character(Combatants[0][i].WrappedChar, true)));
                }

                Combatants.Insert(1, previousRound);
                if (Combatants.Count - 1 > RoundHistoryLimit)
                {
                    Combatants.RemoveAt(Combatants.Count - 1);
                }

                //Removes characters.
                for (int i = 0; i < combatantsToRemove.Count; i++)
                {
                    CharacterRemoved?.Invoke(combatantsToRemove[i]);
                    combatantsToRemove[i].RaiseCharacterRemoved();
                    Combatants[0].RemoveAll(o => o.WrappedChar == combatantsToRemove[i]);
                }
            }
            else
            {
                Combatants.Add(new List<Combatant>());
            }

            //Adds characters.
            for (int i = 0; i < combatantsToAdd.Count; i++)
            {
                Combatants[0].Add(combatantsToAdd[i]);
                CharacterAdded?.Invoke(combatantsToAdd[i]);
                combatantsToAdd[i].WrappedChar.RaiseCharacterAdded();
            }

            combatantsToRemove.Clear();
            combatantsToAdd.Clear();
        }

        /// <summary>
        /// Plays the turn of the current combatant. Advances the turn if
        /// doAdvance is true, or round if on the last turn.
        /// Returns true unless there isn't another round and combat ends.
        /// Turn structure:
        /// - Turn begins
        /// - Motive and move are selected
        /// - Pre-move Movement occurs
        /// - Targets are selected
        /// - Move executes
        /// - Post-move Movement occurs
        /// - Turn ends
        /// </summary>
        public void ExecuteTurn()
        {
            // Start of turn
            TurnStart?.Invoke(combatant);
            combatant.WrappedChar.RaiseTurnStart();

            // Move selection
            Move move = PerformMoveSelect();
            MoveSelected?.Invoke(move);
            combatant.WrappedChar.RaiseMoveSelected(move);

            // Targeting
            List<Combatant> targets = PerformTargeting(move);
            TargetsSelected?.Invoke(targets);
            combatant.WrappedChar.RaiseTargetsSelected(targets);

            foreach (Combatant target in targets)
            {
                target.WrappedChar.RaiseTargeted();
            }

            // Movement 1
            Tuple<float, float> movementBefore = PerformMovementBefore(move);
            Tuple<float, float> oldMovement = combatant.WrappedChar.Location.Data;
            combatant.WrappedChar.Location.Data = movementBefore;
            MovementBeforeSelected?.Invoke(oldMovement);
            combatant.WrappedChar.RaiseMovementBeforeSelected(oldMovement);

            // Re-targeting
            List<Combatant> adjustedTargets = PerformAdjustTargets(move);
            List<Combatant> newTargets = targets.Except(adjustedTargets).ToList();
            TargetsSelected?.Invoke(newTargets);
            combatant.WrappedChar.RaiseTargetsSelected(newTargets);

            foreach (Combatant target in newTargets)
            {
                target.WrappedChar.RaiseTargeted();
            }

            // Action
            BeforeMove?.Invoke();
            combatant.WrappedChar.RaiseBeforeMove();
            move.UsesPerTurnProgress = move.UsesPerTurn;
            PerformMove(adjustedTargets, move);
            combatant.WrappedChar.RaiseAfterMove();
            AfterMove?.Invoke();

            // Movement 2
            Tuple<float, float> movementAfter = PerformMovementAfter(move);
            oldMovement = combatant.WrappedChar.Location.Data;
            combatant.WrappedChar.Location.Data = movementAfter;
            MovementAfterSelected?.Invoke(oldMovement);
            combatant.WrappedChar.RaiseMovementAfterSelected(oldMovement);

            // End of turn
            TurnEnd?.Invoke(combatant);
            combatant.WrappedChar.RaiseTurnEnd();

            // Handles actions after each turn
            int indexOfCombatant = combatantsInOrder.IndexOf(combatant);

            if (!SimultaneousTurnsEnabled ||
                indexOfCombatant == combatantsInOrder.Count - 1 ||
                combatantsInOrder[indexOfCombatant].Speed >
                combatantsInOrder[indexOfCombatant + 1].Speed)
            {
                for (int i = 0; i < Combatants[0].Count; i++)
                {
                    if (Combatants[0][i].CurrentHealth.Data <= 0 &&
                        Combatants[0][i].WrappedChar.Stats.HealthStatus.Data ==
                        Constants.HealthStatuses.RemoveAtZero)
                    {
                        combatantsToRemove.Add(Combatants[0][i].WrappedChar);
                    }
                }

                FlushCombatantQueue();
            }
        }

        /// <summary>
        /// Advances to the next round, recomputing the combatant order,
        /// storing character history and triggering round events.
        /// </summary>
        public bool NextRound()
        {
            if (HasNextRound())
            {
                combatantsInOrder = GetTurnOrder();
                combatant = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if there are still 2+ non-allied combatants in combat.
        /// </summary>
        public bool HasNextRound()
        {
            return Combatants[0].Count != 0 && Combatants[0].Any(o =>
                o.WrappedChar.TeamID != Combatants[0][0].WrappedChar.TeamID);
        }

        /// <summary>
        /// Advances to the next turn without performing it. Returns false if
        /// there is no next turn, else true.
        /// </summary>
        public bool NextTurn()
        {
            int indexOfCombatant = combatantsInOrder.IndexOf(combatant);

            if (indexOfCombatant < combatantsInOrder.Count - 1)
            {
                combatant = combatantsInOrder[indexOfCombatant + 1];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if there is another turn left in the round.
        /// </summary>
        public bool HasNextTurn()
        {
            int indexOfCombatant = combatantsInOrder.IndexOf(combatant);
            return indexOfCombatant < combatantsInOrder.Count - 1;
        }

        /// <summary>
        /// Adds and removes combatants in queue, then clears queues.
        /// </summary>
        private void FlushCombatantQueue()
        {
            if (Combatants.Count == 0 || combatantsInOrder.Count == 0)
            {
                return;
            }

            for (int i = 0; i < combatantsToRemove.Count; i++)
            {
                Combatants[0].RemoveAll(o => o.WrappedChar == combatantsToRemove[i]);
                combatantsInOrder.RemoveAll(o => o.WrappedChar == combatantsToRemove[i]);
                CharacterRemoved?.Invoke(combatantsToRemove[i]);
                combatantsToRemove[i].RaiseCharacterRemoved();
            }

            combatantsToAdd.ForEach(o =>
            {
                o.Speed = o.WrappedChar.Stats.MoveSpeed.Data
                    + o.WrappedChar.MoveSelectBehavior.Perform(Combatants).MoveSpeed;
            });

            int minSpeed = (Combatants[0].Count > 0)
                ? Combatants[0].Min(o => o.Speed)
                : 0;

            combatantsToAdd.ForEach(o =>
            {
                Combatants[0].Add(o);
                combatantsInOrder.Add(o);
                CharacterAdded?.Invoke(o);
                o.WrappedChar.RaiseCharacterAdded();

                if (SpeedCarriesOver)
                {
                    o.Speed += o.AccumulatedSpeed - minSpeed;
                    o.AccumulatedSpeed = o.Speed;
                }
            });

            combatantsInOrder = (Combatants[0].Count > 0)
                ? Combatants[0].OrderByDescending(o => o.Speed).ToList()
                : new List<Combatant>();
        }

        /// <summary>
        /// Queues to remove the given character from combat. They are removed
        /// as soon as the next turn begins. Takes off the addition queue.
        /// </summary>
        /// <param name="oldChar">
        /// The character to be removed from combat.
        /// </param>
        public void RemoveCharacter(Character oldChar)
        {
            combatantsToAdd.RemoveAll(o => o.WrappedChar == oldChar);
            combatantsToRemove.Add(oldChar);
        }

        /// <summary>
        /// Queues to remove the given combatant from combat. They are removed
        /// as soon as the next turn begins. Takes off the addition queue.
        /// </summary>
        /// <param name="oldChar">
        /// The combatant to be removed from combat.
        /// </param>
        public void RemoveCharacter(Combatant oldChar)
        {
            combatantsToAdd.Remove(oldChar);
            combatantsToRemove.Add(oldChar.WrappedChar);
        }

        /// <summary>
        /// Removes a piece of geometry from the list of geometry included
        /// in combat. Returns true if removed, false if not present.
        /// </summary>
        /// <param name="oldGeometry">
        /// The geometry to be removed.
        /// </param>
        public bool RemoveGeometry(Geometry oldGeometry)
        {
            oldGeometry.ZoneEntered -= SubscribeGeometryZoneEntered;
            oldGeometry.ZoneExited -= SubscribeGeometryZoneExited;
            return geometry.Remove(oldGeometry);
        }

        /// <summary>
        /// Queues to remove all characters in the current combat round. They
        /// are removed as soon as the next turn begins.
        /// </summary>
        public void RemoveAllCharacters()
        {
            for (int i = 0; i < Combatants.Count; i++)
            {
                combatantsToRemove.Add(Combatants[0][i].WrappedChar);
            }
        }

        /// <summary>
        /// Clears combatants, history and geometry.
        /// </summary>
        public void ResetSession()
        {
            Combatants = new List<List<Combatant>>();
            combatantsInOrder = new List<Combatant>();
            combatant = null;
            combatantsToRemove = new List<Character>();
            combatantsToAdd = new List<Combatant>();
            geometry = new List<Geometry>();
        }
        #endregion

        /*
        #region PART OF REVISION
        /// <summary>
        /// Computes rounds of combat until only one team is left.
        /// </summary>
        /// <param name="beforeRound">
        /// An optional action that will be executed before each round begins.
        /// A list of all combatants will be given.
        /// </param>
        public void ComputeCombat(Action<List<Character>> beforeRound)
        {
            while (true)
            {
                //Stops combat if there's one or no characters left.
                if (combatants.Count <= 1)
                {
                    break;
                }

                int teamId = combatants.Where(o => o != null).First().TeamID;
                if (combatants.TrueForAll(o => o.TeamID == teamId))
                {
                    //Reassigns all characters to different teams.
                    if (FreeForAllEnabled)
                    {
                        for (int i = 0; i < combatants.Count; i++)
                        {
                            combatants[i].TeamID = i;
                        }
                    }

                    //Exits combat.
                    else
                    {
                        break;
                    }
                }

                beforeRound?.Invoke(new List<Character>(combatants));
                ComputeCombatRound();
            }
        }

        /// <summary>
        /// Computes a full round of combat. Attempts to compute multiple
        /// rounds of combat at once will fail.
        /// </summary>
        public void ComputeCombatRound()
        {
            //Prevents concurrent execution.
            if (isInCombat)
            {
                return;
            }

            isInCombat = true;

            //Organizes characters by combat speeds and statuses.
            List<Character> charsInOrder = GetInitiative();
            var charsFirst = charsInOrder.Where(o => o.CombatSpeedStatus.Data ==
                Constants.SpeedStatuses.AlwaysFirstc);
            var charsNormal = charsInOrder.Where(o => o.CombatSpeedStatus.Data ==
                Constants.SpeedStatuses.Normal);
            var charsLast = charsInOrder.Where(o => o.CombatSpeedStatus.Data ==
                Constants.SpeedStatuses.AlwaysLast);
            charsInOrder = new List<Character>(charsFirst);
            charsInOrder.AddRange(charsNormal);
            charsInOrder.AddRange(charsLast);

            //Fires round-starting hooks for each character.
            charsInOrder.ForEach((Action<Character>)((c) =>
            {
                c.Actions[Constants.CharacterEvents.RoundStarting]
                    .ForEach((object o) => o?.Invoke(combatants));
            }));

            //Executes the current action of each character in order.
            for (int i = 0; i < charsInOrder.Count; i++)
            {
                combatant = charsInOrder[i];

                //Skips invalid combatants.
                if (combatantsToRemove.Contains(combatant) ||
                    !combatants.Contains(combatant))
                {
                    continue;
                }

                //Removes combatants that aren't combat-enabled.
                var charsLeft = charsInOrder.Where(o => o.DoRemoveFromCombat.Data == true).ToList();
                charsInOrder.Except(charsLeft).ToList().ForEach((chr) => RemoveFromCombat(chr));

                //Executes the current action.
                if (combatant.CombatActionsEnabled?.Data == true)
                {
                    combatant.Actions[Constants.CharacterEvents.TurnStart]
                        .ForEach(o => o?.Invoke(combatants));
                    combatant.Actions[Constants.CharacterEvents.CombatTarget]
                        .ForEach(o => o?.Invoke(combatants));
                    combatant.Actions[Constants.CharacterEvents.CombatMovement]
                        .ForEach(o => o?.Invoke(combatants));
                    combatant.Actions[Constants.CharacterEvents.CombatAction]
                        .ForEach(o => o?.Invoke(combatants));
                }

                //Stops combat if set from within an invoked action.
                if (doStopCombat)
                {
                    break;
                }
            }

            isInCombat = false;
            combatant = null;

            //Fires round-ending hooks for each character.
            charsInOrder.ForEach((Action<Character>)((c) =>
            {
                c.Actions[Constants.CharacterEvents.RoundEnding]
                    .ForEach((object o) => o?.Invoke(combatants));
            }));

            //Handles queued addition and removal from combat.
            for (int i = 0; i < combatantsToAdd.Count; i++)
            {
                AddToCombat(combatantsToAdd[i]);
            }
            for (int i = 0; i < combatantsToRemove.Count; i++)
            {
                RemoveFromCombat(combatantsToRemove[i]);
            }
            combatantsToAdd.Clear();
            combatantsToRemove.Clear();
        }

        /// <summary>
        /// After having specified targets, the default action attacks the
        /// chosen targets and characters in chosen splash areas with support
        /// for all character combat properties. Calls related user actions.
        /// </summary>
        public static Action<List<Character>> AttackAction(Session session)
        {
            //Returns an action that handles attacks on targets and locations.
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();

                //Performs attacks on each targeted character.
                if (combatant.TargetChars?.Data.Count > 0)
                {
                    for (int i = 0; i < combatant.TargetChars.Data.Count; i++)
                    {
                        Attack(session, combatant, combatant.TargetChars.Data[i], false, 0);
                    }
                }

                //Performs splash attacks based on a radius.
                if (combatant.TargetAreas?.Data.Count > 0)
                {
                    for (int i = 0; i < combatant.TargetAreas.Data.Count; i++)
                    {
                        for (int j = 0; j < combatants.Count; j++)
                        {
                            if (combatants[j] == combatant)
                            {
                                continue;
                            }

                            int x1 = combatant.TargetAreas?.Data[i]?.Item1 ?? 0;
                            int y1 = combatant.TargetAreas?.Data[i]?.Item2 ?? 0;
                            float rad = combatant.TargetAreas?.Data[i]?.Item3 ?? 0;
                            float x2 = combatants[j].Location?.Data?.Item1 ?? 0;
                            float y2 = combatants[j].Location?.Data?.Item2 ?? 0;
                            double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                            if (dist <= rad && rad > 0)
                            {
                                if (combatant.SplashBehavior?.Data ==
                                    Constants.CombatSplashBehaviors.All)
                                {
                                    Attack(session, combatant, combatants[j], true, (dist / rad) * 100);
                                }
                                else if (combatant.SplashBehavior?.Data ==
                                    Constants.CombatSplashBehaviors.Allies &&
                                    combatants[j].TeamID == combatant.TeamID)
                                {
                                    Attack(session, combatant, combatants[j], true, (dist / rad) * 100);
                                }
                                else if (combatant.SplashBehavior?.Data ==
                                    Constants.CombatSplashBehaviors.Enemies &&
                                    combatants[j].TeamID != combatant.TeamID)
                                {
                                    Attack(session, combatant, combatants[j], true, (dist / rad) * 100);
                                }
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Moves within attack range of the nearest enemy, making them the
        /// target if persistent targeting is off or the old target is
        /// invalid.
        /// </summary>
        public static Action<List<Character>> MoveAction(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();
                float x1 = combatant.Location.Data.Item1;
                float y1 = combatant.Location.Data.Item1;

                //Moves towards the locked-on target as needed.
                if (combatant.TargetChars.Data.Count > 0 &&
                    combatants.Contains(combatant.TargetChars.Data[0]) &&
                    combatant.TargetChars.Data[0].CombatEnabled.Data &&
                    combatant.TargetPersistent.Data)
                {
                    float x2 = combatant.TargetChars.Data[0].Location.Data.Item1;
                    float y2 = combatant.TargetChars.Data[0].Location.Data.Item1;
                    double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                    double angle = Math.Atan2(y2 - y1, x2 - x1);

                    //Moves towards the combatant.
                    double moveDist = distance - combatant.MaxRangeAllowed.Data;
                    if (Math.Abs(moveDist) > combatant.MovementRate.Data)
                    {
                        moveDist = combatant.MovementRate.Data * Math.Sign(moveDist);
                    }

                    combatant.Location.Data = new Tuple<float, float>(
                        x1 + (float)(Math.Cos(angle) * moveDist),
                        y1 + (float)(Math.Sin(angle) * moveDist));
                }

                //Moves towards the nearest combatant; sets to target when in range.
                else
                {
                    double angle = 0;
                    double distance = double.MaxValue;
                    int charIndex = -1;

                    //Records the nearest combatant with magnitude and angle.
                    for (int i = 0; i < combatants.Count; i++)
                    {
                        if (combatants[i] == combatant)
                        {
                            continue;
                        }

                        float x2 = combatants[i].Location.Data.Item1;
                        float y2 = combatants[i].Location.Data.Item2;
                        double newDistance = Math.Sqrt(
                            Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

                        if (newDistance < distance)
                        {
                            angle = Math.Atan2(y2 - y1, x2 - x1);
                            distance = newDistance;
                            charIndex = i;
                        }
                    }

                    if (charIndex != -1)
                    {
                        //Adds or removes from target list based on range.
                        if (distance >= combatant.MinRangeRequired.Data &&
                            distance <= combatant.MaxRangeAllowed.Data)
                        {
                            if (!combatant.TargetChars.Data.Contains(combatants[charIndex]))
                            {
                                combatant.TargetChars.Data.Add(combatants[charIndex]);
                            }
                        }
                        else if (combatant.TargetChars.Data.Contains(combatants[charIndex]))
                        {
                            combatant.TargetChars.Data.Remove(combatants[charIndex]);
                        }

                        //Moves towards the combatant.
                        double moveDist = distance - combatant.MaxRangeAllowed.Data;
                        if (Math.Abs(moveDist) > combatant.MovementRate.Data)
                        {
                            moveDist = combatant.MovementRate.Data * Math.Sign(moveDist);
                        }

                        combatant.Location.Data = new Tuple<float, float>(
                            x1 + (float)(Math.Cos(angle) * moveDist),
                            y1 + (float)(Math.Sin(angle) * moveDist));
                    }
                }
            });
        }

        /// <summary>
        /// Moves a combatant towards the first target in their targets list.
        /// </summary>
        public static Action<List<Character>> MoveToTarget(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();
                var firstTarget = combatant.TargetChars.Data.FirstOrDefault();

                if (combatant.MovementRate.Data > 0 && firstTarget != null)
                {
                    float x1 = combatant.Location?.Data?.Item1 ?? 0;
                    float y1 = combatant.Location?.Data?.Item2 ?? 0;
                    float x2 = firstTarget.Location?.Data?.Item1 ?? 0;
                    float y2 = firstTarget.Location?.Data?.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveDist = combatant.MovementRate.Data;
                    if (moveDist > dist)
                    {
                        moveDist = dist;
                    }
                    double moveX = Math.Cos(moveAngle) * moveDist;
                    double moveY = Math.Sin(moveAngle) * moveDist;

                    combatant.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Moves a combatant until their first target is just within attack
        /// range.
        /// </summary>
        public static Action<List<Character>> MoveToTargetMaxRange(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();
                var firstTarget = combatant.TargetChars.Data.FirstOrDefault();

                if (combatant.MovementRate.Data > 0 && firstTarget != null)
                {
                    float x1 = combatant.Location?.Data?.Item1 ?? 0;
                    float y1 = combatant.Location?.Data?.Item2 ?? 0;
                    float x2 = firstTarget.Location?.Data?.Item1 ?? 0;
                    float y2 = firstTarget.Location?.Data?.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveDist = combatant.MovementRate.Data;
                    if (moveDist > dist - combatant.MaxRangeAllowed.Data)
                    {
                        moveDist = dist - combatant.MaxRangeAllowed.Data;
                    }

                    double moveX = Math.Cos(moveAngle) * moveDist;
                    double moveY = Math.Sin(moveAngle) * moveDist;

                    combatant.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Moves a combatant towards the average location computed from the
        /// position of all their targets.
        /// </summary>
        public static Action<List<Character>> MoveToTargets(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();

                if (combatant.MovementRate.Data > 0 &&
                    combatant.TargetChars.Data.Count > 0)
                {
                    float x1 = combatant.Location?.Data?.Item1 ?? 0;
                    float y1 = combatant.Location?.Data?.Item2 ?? 0;
                    float x2 = combatant.TargetChars.Data
                        .Average(o => o?.Location?.Data?.Item1 ?? 0);
                    float y2 = combatant.TargetChars.Data
                        .Average(o => o?.Location?.Data?.Item2 ?? 0);
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveX = Math.Cos(moveAngle) * combatant.MovementRate.Data;
                    double moveY = Math.Sin(moveAngle) * combatant.MovementRate.Data;

                    combatant.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Moves the combatant within range of the nearest other if it has
        /// no targets.
        /// </summary>
        /// <param name="filter">
        /// Filters the candidates for the nearest combatant by team
        /// allegiance.
        /// </param>
        public static Action<List<Character>> MoveToNearestUntargeted(
            Session session,
            Constants.CombatTargetsAllowed filter)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();

                //Exits if there are targets.
                if (combatant.TargetChars.Data.Count != 0)
                {
                    return;
                }

                //Filters possible combatants by team allegiance.
                if (filter == Constants.CombatTargetsAllowed.OnlyAllies)
                {
                    combatants = combatants.Where(o => combatant.TeamID == o.TeamID && combatant != o).ToList();
                }
                else if (filter == Constants.CombatTargetsAllowed.OnlyEnemies)
                {
                    combatants = combatants.Where(o => combatant.TeamID != o.TeamID).ToList();
                }
                else
                {
                    combatants = combatants.Where(o => combatant != o).ToList();
                }

                //Organize combatants by distance to the combatant.
                float x1 = combatant.Location?.Data?.Item1 ?? float.MinValue;
                float y1 = combatant.Location?.Data?.Item2 ?? float.MinValue;
                double minimumDist = -1;
                Tuple<float, float> minimumLoc = null;

                //Records the minimum location to the nearest other combatant.
                for (int i = 0; i < combatants.Count; i++)
                {
                    float x2 = combatants[i].Location?.Data.Item1 ?? 0;
                    float y2 = combatants[i].Location?.Data.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                    if (dist < minimumDist || minimumDist == -1)
                    {
                        minimumDist = dist;
                        minimumLoc = new Tuple<float, float>(x2, y2);
                    }
                }

                //Moves towards the recorded location.
                if (minimumDist != -1)
                {
                    double moveAngle = Math.Atan2(
                        minimumLoc.Item2 - combatant.Location.Data.Item2,
                        minimumLoc.Item1 - combatant.Location.Data.Item1);

                    double moveDist = combatant.MovementRate.Data;
                    if (moveDist > minimumDist - combatant.MaxRangeAllowed.Data)
                    {
                        moveDist = minimumDist - combatant.MaxRangeAllowed.Data;
                    }

                    double moveX = Math.Cos(moveAngle) * moveDist;
                    double moveY = Math.Sin(moveAngle) * moveDist;

                    combatant.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Targets the nearest X combatants based on the chosen filter
        /// that fill within the combatant's attack range. Does not clear
        /// old targets.
        /// </summary>
        /// <param name="filter">
        /// Filters possible targets by team allegiance.
        /// </param>
        /// <param name="numberOfTargets">
        /// Up to this many of the nearest targets will be selected.
        /// </param>
        public static Action<List<Character>> TargetNearest(
            Session session,
            Constants.CombatTargetsAllowed filter,
            int numberOfTargets)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();

                //Filters possible combatants by team allegiance.
                if (filter == Constants.CombatTargetsAllowed.OnlyAllies)
                {
                    combatants = combatants.Where(o => combatant.TeamID == o.TeamID && combatant != o).ToList();
                }
                else if (filter == Constants.CombatTargetsAllowed.OnlyEnemies)
                {
                    combatants = combatants.Where(o => combatant.TeamID != o.TeamID).ToList();
                }

                //Organize combatants by distance to the combatant.
                float x1 = combatant.Location?.Data?.Item1 ?? float.MinValue;
                float y1 = combatant.Location?.Data?.Item2 ?? float.MinValue;
                var charDists = new List<Tuple<Character, double>>();

                for (int i = 0; i < combatants.Count; i++)
                {
                    float x2 = combatants[i].Location?.Data.Item1 ?? 0;
                    float y2 = combatants[i].Location?.Data.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                    if (combatant.MinRangeRequired.Data <= dist &&
                        combatant.MaxRangeAllowed.Data >= dist)
                    {
                        charDists.Add(new Tuple<Character, double>(combatants[i], dist));
                    }
                }

                charDists = charDists.OrderBy(o => o.Item2).ToList();

                //Sets the targets, skipping redundant targets.
                for (int i = 0; i < charDists.Count && i < numberOfTargets; i++)
                {
                    if (!combatant.TargetChars.Data.Contains(charDists[i].Item1))
                    {
                        combatant.TargetChars.Data.Add(charDists[i].Item1);
                    }
                }
            });
        }

        /// <summary>
        /// Targets the farthest X combatants based on the chosen filter
        /// that fall within the combatant's attack range. Does not clear
        /// old targets.
        /// </summary>
        /// <param name="filter">
        /// Filters possible targets by team allegiance.
        /// </param>
        /// <param name="numberOfTargets">
        /// Up to this many of the nearest targets will be selected.
        /// </param>
        public static Action<List<Character>> TargetFarthest(
            Session session,
            Constants.CombatTargetsAllowed filter,
            int numberOfTargets)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character combatant = session.GetCombatant();
                List<Character> combatants = session.GetCombatants();

                //Filters possible combatants by team allegiance.
                if (filter == Constants.CombatTargetsAllowed.OnlyAllies)
                {
                    combatants = combatants.Where(o => combatant.TeamID == o.TeamID && combatant != o).ToList();
                }
                else if (filter == Constants.CombatTargetsAllowed.OnlyEnemies)
                {
                    combatants = combatants.Where(o => combatant.TeamID != o.TeamID).ToList();
                }

                //Organize combatants by distance to the combatant.
                float x1 = combatant.Location?.Data?.Item1 ?? float.MinValue;
                float y1 = combatant.Location?.Data?.Item2 ?? float.MinValue;
                var charDists = new List<Tuple<Character, double>>();

                for (int i = 0; i < combatants.Count; i++)
                {
                    float x2 = combatants[i].Location?.Data.Item1 ?? 0;
                    float y2 = combatants[i].Location?.Data.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                    if (combatant.MinRangeRequired.Data <= dist &&
                        combatant.MaxRangeAllowed.Data >= dist)
                    {
                        charDists.Add(new Tuple<Character, double>(combatants[i], dist));
                    }
                }

                charDists = charDists.OrderByDescending(o => o.Item2).ToList();

                //Sets the targets, skipping redundant targets.
                for (int i = 0; i < charDists.Count && i < numberOfTargets; i++)
                {
                    if (!combatant.TargetChars.Data.Contains(charDists[i].Item1))
                    {
                        combatant.TargetChars.Data.Add(charDists[i].Item1);
                    }
                }
            });
        }

        /// <summary>
        /// Performs an attack against the given opponent with the combatant.
        /// Used by GetDefaultAttackAction().
        /// </summary>
        /// <param name="attacker">
        /// The character that performs the attack.
        /// </param>
        /// <param name="opponent">
        /// The character that receives the attack, if not a splash attack.
        /// </param>
        /// <param name="isSplashAttack">
        /// Whether the attack is area-based. An area-based attack hits
        /// combatants within a radius of its epicenter given in x,y coords.
        /// </param>
        /// <param name="splashAttackPercentDistance">
        /// For splash attacks, this is the opponent's distance from the
        /// center of the area attack. 0% if at the center, 100% if on the
        /// perimeter of the area attack.
        /// </param>
        private static void Attack(
            Session session,
            Character attacker,
            Character opponent,
            bool isSplashAttack,
            double splashAttackPercentDistance)
        {
            Character combatant = session.GetCombatant();
            List<Character> combatants = session.GetCombatants();

            //Exits if any character is invalid.
            if (!combatants.Contains(attacker) ||
                !combatants.Contains(opponent))
            {
                attacker?.Actions[Constants.CharacterEvents.TargetInvalid]
                    ?.ForEach(o => o?.Invoke(combatants));
                return;
            }

            //Event hooks for initializing an attack.
            attacker?.Actions[Constants.CharacterEvents.TargetsSelected]
                ?.ForEach(o => o?.Invoke(combatants));

            if (isSplashAttack)
            {
                attacker?.Actions[Constants.CharacterEvents.AttackingByArea]
                    ?.ForEach(o => o?.Invoke(combatants));
            }
            else
            {
                attacker?.Actions[Constants.CharacterEvents.AttackingTarget]
                    ?.ForEach(o => o?.Invoke(combatants));
            }

            opponent?.Actions[Constants.CharacterEvents.Targeted]
                ?.ForEach(o => o?.Invoke(combatants));

            //Ignores character targets that are out-of-range.
            float x1, y1, x2, y2;
            x1 = y1 = x2 = y2 = 0;
            if (!isSplashAttack)
            {
                x1 = attacker?.Location?.Data.Item1 ?? 0;
                y1 = attacker?.Location?.Data.Item1 ?? 0;
                x2 = opponent?.Location?.Data?.Item1 ?? 0;
                y2 = opponent?.Location?.Data?.Item2 ?? 0;
                double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                if (dist < attacker.MinRangeRequired.Data ||
                    dist > attacker.MaxRangeAllowed.Data)
                {
                    attacker?.Actions[Constants.CharacterEvents.TargetOutOfRange]
                        ?.ForEach(o => o?.Invoke(combatants));
                    return;
                }
            }

            //Attacks the character.
            //Attempts to hit the opponent.
            if (rng.Next(100) <
                (attacker.PercentToHit.Data -
                opponent.PercentToDodge.Data))
            {
                attacker?.Actions[Constants.CharacterEvents.AttackHit]
                    ?.ForEach(o => o?.Invoke(combatants));

                //Administers physical knockback effects.
                int moveMagnitude = attacker.MinLocationKnockback.Data +
                    rng.Next(attacker.MaxLocationKnockback.Data -
                    attacker.MinLocationKnockback.Data);

                if (moveMagnitude > 0)
                {
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveX = Math.Cos(moveAngle) * moveMagnitude;
                    double moveY = Math.Sin(moveAngle) * moveMagnitude;

                    opponent.Location.Data = new Tuple<float, float>(
                        opponent.Location.Data.Item1 + (float)moveX,
                        opponent.Location.Data.Item2 + (float)moveY);
                }

                //Computes physical damage for each type of damage.
                for (int j = 0; j < attacker.MinDamage.Data.Length &&
                    j < attacker.MaxDamage.Data.Length; j++)
                {
                    //Factors in base damage.
                    double dmg = attacker.MinDamage.Data[j] +
                        rng.Next(attacker.MaxDamage.Data[j] - attacker.MinDamage.Data[j]);

                    //Factors in splash range.
                    if (isSplashAttack)
                    {
                        dmg *= (attacker.MaxRangeMultiplier.Data +
                            (splashAttackPercentDistance / 100) *
                            (attacker.MinRangeMultiplier.Data -
                            attacker.MaxRangeMultiplier.Data));
                    }

                    //Factors in critical hits.
                    if (rng.Next(100) < attacker.PercentToCritHit.Data[j] &&
                        opponent.CombatCriticalStatus.Data ==
                        Constants.CriticalStatuses.Normal)
                    {
                        attacker?.Actions[Constants.CharacterEvents.AttackCritHit]
                            ?.ForEach(o => o?.Invoke(combatants));

                        dmg *= attacker.CritDamageMultiplier.Data[j];
                    }

                    //Factors in damage reduction and resistance.
                    dmg = (dmg - opponent.DamageReduction.Data[j]) *
                        (1 - (opponent.DamageResistance.Data[j] / 100));

                    //Administers computed damage.
                    if (dmg > 0)
                    {
                        int attHealth = attacker.Health.Data;
                        int oppHealth = opponent.Health.Data;

                        opponent.Health.Data -= (int)dmg;

                        //Factors in knockback.
                        if (opponent.ConstantKnockback.Data +
                            (int)opponent.PercentKnockback.Data != 0)
                        {
                            opponent.Actions[Constants.CharacterEvents.AttackKnockback]
                                ?.ForEach(o => o?.Invoke(combatants));

                            attacker.Health.Data -=
                                (opponent.ConstantKnockback.Data +
                                (int)(opponent.PercentKnockback.Data * dmg / 100));
                        }

                        if (attHealth > 0 && attacker.Health.Data <= 0)
                        {
                            if (attacker.CombatHeathStatus.Data == Constants.HealthStatuses.RemoveAtZero)
                            {
                                attacker?.Actions[Constants.CharacterEvents.NoHealth]
                                    ?.ForEach(o => o?.Invoke(combatants));
                                session.RemoveFromCombat(attacker);
                            }
                        }
                        if (oppHealth > 0 && opponent.Health.Data <= 0)
                        {
                            if (opponent.CombatHeathStatus.Data == Constants.HealthStatuses.RemoveAtZero)
                            {
                                opponent?.Actions[Constants.CharacterEvents.NoHealth]
                                    ?.ForEach(o => o?.Invoke(combatants));
                                session.RemoveFromCombat(opponent);
                            }
                        }
                    }
                }
            }
            else
            {
                attacker?.Actions[Constants.CharacterEvents.AttackMissed]
                    ?.ForEach(o => o?.Invoke(combatants));
            }
        }
        #endregion
        */
    }
}
