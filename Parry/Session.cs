using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry
{
    /// <summary>
    /// Automates combat between any number of characters.
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
        /// Lists all characters for the current round (most recent) and
        /// previous rounds. Position 0 is the state before combat. Characters
        /// are organized by combat speed, with simultaneous characters
        /// organized by original relative position in the list.
        /// </summary>
        private List<List<Character>> chars;

        /// <summary>
        /// Stores current characters in turn order.
        /// </summary>
        private List<Character> charsInOrder;

        /// <summary>
        /// The character whose turn is currently in effect.
        /// </summary>
        private Character character;

        /// <summary>
        /// These characters are removed after finishing any character's moves.
        /// </summary>
        private List<Character> charsToRemove;

        /// <summary>
        /// These characters are added after finishing any character's moves.
        /// </summary>
        private List<Character> charsToAdd;

        /// <summary>
        /// Tracks all walls and zones on the battlefield.
        /// </summary>
        private List<Geometry> geometry;
        #endregion

        #region Public Variables
        /// <summary>
        /// After each character executes their move, they are reinserted into
        /// combat if they're remaining speed is greater than the minimum speed
        /// among characters that round.
        /// False by default.
        /// </summary>
        public bool ExtraTurnsEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When true, instead of stopping combat when there's only one team
        /// left, each remaining character's team ID will be set to a unique
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
        /// The number of previous rounds to store in memory, which enables
        /// users to create AI behavior based on previous character behaviors
        /// and performances. Can't be negative.
        /// 10 by default.
        /// </summary>
        public int RoundHistoryLimit
        {
            get;
            set;
        }

        /// <summary>
        /// When enabled, characters with the same speed will take their turns
        /// in order without flushing the add and remove queues between. This
        /// allows e.g. two characters to deal lethal damage to each other.
        /// False by default.
        /// </summary>
        public bool SimultaneousTurnsEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// When true, the maximum speed delay among all characters is subtracted
        /// from each character's speed delay last round, and the remainder is added
        /// to the next round.
        /// False by default.
        /// </summary>
        public bool SpeedCarriesOver
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
        public event Action<Character> TurnStart;

        /// <summary>
        /// The event raised just before a character's turn ends.
        /// </summary>
        public event Action<Character> TurnEnd;

        /// <summary>
        /// The event raised when a character selects a move.
        /// First argument is the move the active character selected.
        /// </summary>
        public event Action<Move> MoveSelected;

        /// <summary>
        /// The event raised when a character selects their targets.
        /// First argument is the list of targets selected.
        /// </summary>
        public event Action<List<Character>> TargetsSelected;

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
        /// First argument is the character that was added.
        /// </summary>
        public event Action<Character> CharacterAdded;

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
        public event Action<Geometry, Character> ZoneEntered;

        /// <summary>
        /// The event raised when a character exits a zone.
        /// First argument is the zone which was exited.
        /// Second argument is the character that exited the zone.
        /// </summary>
        public event Action<Geometry, Character> ZoneExited;
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
        /// Returns characters in turn order, or an empty list if combat has
        /// not been initialized yet.
        /// </summary>
        private List<Character> GetTurnOrder()
        {
            if (chars.Count == 0)
            {
                return new List<Character>();
            }

            chars[0].ForEach(o =>
            {
                o.CharStats.SpeedDelay = -o.CombatStats.MoveSpeed.Data;
                if (o.MoveSelectBehavior.Perform(chars).Count != 0)
                {
                    o.CharStats.SpeedDelay += o.MoveSelectBehavior.ChosenMoves.Sum(p => p.MoveSpeedDelay);
                }
            });

            int maxDelay = chars[0].Max(o => o.CharStats.SpeedDelay);
            chars[0].ForEach(o =>
            {
                if (SpeedCarriesOver)
                {
                    o.CharStats.SpeedDelay += o.CharStats.AccumulatedSpeed - maxDelay;
                    o.CharStats.AccumulatedSpeed = o.CharStats.SpeedDelay;
                }
            });

            // Organizes by groups for first, normal priority, last all by speed.
            List<Character> orderedChars = chars[0].OrderBy(o => o.CharStats.SpeedDelay).ToList();
            List<Character> firstChars = new List<Character>();
            List<Character> lastChars = new List<Character>();
            
            for (int i = orderedChars.Count - 1; i >= 0; i--)
            {
                if (orderedChars[i].CombatStats.SpeedStatus.Data == Constants.SpeedStatuses.AlwaysFirst)
                {
                    firstChars.Insert(0, orderedChars[i]);
                    orderedChars.RemoveAt(i);
                }
                else if (orderedChars[i].CombatStats.SpeedStatus.Data == Constants.SpeedStatuses.AlwaysLast)
                {
                    lastChars.Insert(0, orderedChars[i]);
                    orderedChars.RemoveAt(i);
                }
            }

            firstChars.AddRange(orderedChars);
            firstChars.AddRange(lastChars);
            return firstChars;
        }

        /// <summary>
        /// Executes move logic for the active character.
        /// This is the fourth step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private void PerformMove(
            List<Character> targets,
            Move chosenMove)
        {
            chosenMove.Perform(character, chars[0], targets);
        }

        /// <summary>
        /// Executes pre-move movement logic for the active character.
        /// This is the third step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private Tuple<float, float> PerformMovementBefore()
        {
            if (!character.CombatMovementBeforeEnabled.Data)
            {
                return character.CharStats.Location.Data;
            }

            if (character.MoveSelectBehavior.MovementBeforeBehavior == null)
            {
                return character.DefaultMovementBeforeBehavior.Perform(chars[0], character);
            }

            return character.MoveSelectBehavior.MovementBeforeBehavior(character.MoveSelectBehavior.ChosenMoves)
                .Perform(chars[0], character);
        }

        /// <summary>
        /// Executes post-move movement logic for the active character.
        /// This is the fifth and last step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private Tuple<float, float> PerformMovementAfter()
        {
            if (!character.CombatMovementAfterEnabled.Data)
            {
                return character.CharStats.Location.Data;
            }

            if (character.MoveSelectBehavior.MovementAfterBehavior == null)
            {
                return character.DefaultMovementAfterBehavior.Perform(chars[0], character);
            }

            return character.MoveSelectBehavior.MovementAfterBehavior(character.MoveSelectBehavior.ChosenMoves)
                .Perform(chars[0], character);
        }

        /// <summary>
        /// Executes move selection logic for the active character.
        /// This is the first step in executing a turn.
        /// </summary>
        /// <param name="session">
        /// An active combat session.
        /// </param>
        private List<Move> PerformMoveSelect()
        {
            return character.MoveSelectBehavior.Perform(chars);
        }

        /// <summary>
        /// Executes targeting logic for the active character.
        /// This is the second step in executing a turn.
        /// </summary>
        private List<Character> PerformTargeting(Move chosenMove)
        {
            if (!character.CombatTargetingEnabled.Data)
            {
                return new List<Character>();
            }

            if (chosenMove?.TargetBehavior == null)
            {
                return character.DefaultTargetBehavior.Perform(chars, character);
            }

            return chosenMove?.TargetBehavior?.Perform(chars, character) ?? new List<Character>();
        }

        /// <summary>
        /// Recomputes targets after adjusting all distance-based factors. This
        /// is not a discrete step in a turn.
        /// </summary>
        private List<Character> PerformAdjustTargets(Move chosenMove)
        {
            if (!character.CombatTargetingEnabled.Data)
            {
                return new List<Character>();
            }

            if (chosenMove?.TargetBehavior == null)
            {
                return character.DefaultTargetBehavior.PostMovePerform(chars, character);
            }

            return chosenMove?.TargetBehavior?.PostMovePerform(chars, character) ?? new List<Character>();
        }

        /// <summary>
        /// The function used to subscribe to any geometry's zone entered
        /// event, which is done so the general zone event can be triggered.
        /// In order to unsubscribe cleanly, the function is encapsulated
        /// here.
        /// </summary>
        private void SubscribeGeometryZoneEntered(
            Geometry theGeometry,
            Character character)
        {
            ZoneEntered?.Invoke(theGeometry, character);
        }

        /// <summary>
        /// The function used to subscribe to any geometry's zone exited
        /// event, which is done so the general zone event can be triggered.
        /// In order to unsubscribe cleanly, the function is encapsulated
        /// here.
        /// </summary>
        private void SubscribeGeometryZoneExited(
            Geometry theGeometry,
            Character character)
        {
            ZoneExited?.Invoke(theGeometry, character);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Queues to add the given character to combat. They are added as soon
        /// as the next turn begins. Takes off the removal queue.
        /// </summary>
        /// <param name="character">
        /// The character to be added to combat.
        /// </param>
        public void AddCharacter(Character character)
        {
            charsToAdd.Add(character);
            charsToRemove.Remove(character);
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
        /// not clear the geometry or characters to add (which is how to add
        /// characters at the start of the combat).
        /// </summary>
        public void StartSession()
        {
            character = null;
            chars.Clear();
            chars.Add(new List<Character>());
            
            for (int i = 0; i < charsToAdd.Count; i++)
            {
                chars[0].Add(charsToAdd[i]);
                CharacterAdded?.Invoke(charsToAdd[i]);
                charsToAdd[i].RaiseCharacterAdded();
            }

            charsToRemove.Clear();
            charsToAdd.Clear();
            charsInOrder = GetTurnOrder();
        }

        /// <summary>
        /// Plays each remaining turn in the round in sequence. AI computes
        /// when its turn begins, so this is best used for turn-based games
        /// that let the players choose their actions mid-round when their turn
        /// starts, since that's how the AI works.
        /// </summary>
        public void ExecuteRound()
        {
            RoundStarting?.Invoke();

            while (NextTurn())
            {
                ExecuteTurn();
            }

            RoundEnding?.Invoke();
            charsInOrder.Clear();

            //Adds to history.
            if (chars.Count > 0)
            {
                List<Character> previousRound = new List<Character>();
                for (int i = 0; i < chars[0].Count; i++)
                {
                    previousRound.Add(new Character(chars[0][i], true));
                }

                chars.Insert(1, previousRound);
                if (chars.Count - 1 > RoundHistoryLimit)
                {
                    chars.RemoveAt(chars.Count - 1);
                }

                //Removes characters.
                for (int i = 0; i < charsToRemove.Count; i++)
                {
                    chars[0].Remove(charsToRemove[i]);
                    CharacterRemoved?.Invoke(charsToRemove[i]);
                    charsToRemove[i].RaiseCharacterRemoved();
                }
            }
            else
            {
                chars.Add(new List<Character>());
            }

            //Adds characters.
            for (int i = 0; i < charsToAdd.Count; i++)
            {
                chars[0].Add(charsToAdd[i]);
                CharacterAdded?.Invoke(charsToAdd[i]);
                charsToAdd[i].RaiseCharacterAdded();
            }

            charsToRemove.Clear();
            charsToAdd.Clear();
        }

        /// <summary>
        /// Plays the turn of the current character.
        /// Turn structure:
        /// - Turn begins
        /// - Motive and move are selected
        /// - Targets are selected
        /// - Pre-move Movement occurs
        /// - Move executes
        /// - Post-move Movement occurs
        /// - Turn ends
        /// </summary>
        public void ExecuteTurn()
        {
            // Start of turn
            TurnStart?.Invoke(character);
            character.RaiseTurnStart();

            List<Move> moves = PerformMoveSelect();
            List<List<Character>> targetLists = new List<List<Character>>();
            moves.ForEach(move =>
            {
                // Move selection
                MoveSelected?.Invoke(move);
                character.RaiseMoveSelected(move);

                // Targeting
                List<Character> targets = PerformTargeting(move);
                targetLists.Add(targets);

                TargetsSelected?.Invoke(targets);
                character.RaiseTargetsSelected(targets);

                foreach (Character target in targets)
                {
                    target.RaiseTargeted();
                }
            });

            // Movement 1
            Tuple<float, float> movementBefore = PerformMovementBefore();
            Tuple<float, float> oldMovement = character.CharStats.Location.Data;
            character.CharStats.Location.Data = movementBefore;
            MovementBeforeSelected?.Invoke(oldMovement);
            character.RaiseMovementBeforeSelected(oldMovement);

            // Re-targeting
            for (int i = 0; i < moves.Count; i++)
            {
                List<Character> targets = PerformAdjustTargets(moves[i]);
                List<Character> newTargets = targetLists[i].Except(targets).ToList();

                TargetsSelected?.Invoke(newTargets);
                character.RaiseTargetsSelected(newTargets);

                foreach (Character target in newTargets)
                {
                    target.RaiseTargeted();
                }

                targetLists[i] = targets;
            } ;

            // Action
            BeforeMove?.Invoke();
            character.RaiseBeforeMove();

            for (int i = 0; i < moves.Count; i++)
            {
                moves[i].UsesPerTurnProgress = moves[i].UsesPerTurn;
                PerformMove(targetLists[i], moves[i]);
            }

            character.MoveSelectBehavior.TurnFractionLeft = 1;

            character.RaiseAfterMove();
            AfterMove?.Invoke();

            // Movement 2
            Tuple<float, float> movementAfter = PerformMovementAfter();
            oldMovement = character.CharStats.Location.Data;
            character.CharStats.Location.Data = movementAfter;
            MovementAfterSelected?.Invoke(oldMovement);
            character.RaiseMovementAfterSelected(oldMovement);

            // End of turn
            TurnEnd?.Invoke(character);
            character.RaiseTurnEnd();

            // Handles actions after each turn
            int charIndex = charsInOrder.IndexOf(character);

            if (!SimultaneousTurnsEnabled ||
                charIndex == charsInOrder.Count - 1 ||
                charsInOrder[charIndex].CharStats.SpeedDelay >
                charsInOrder[charIndex + 1].CharStats.SpeedDelay)
            {
                for (int i = 0; i < chars[0].Count; i++)
                {
                    if (chars[0][i].CharStats.Health.Data <= 0 &&
                        chars[0][i].CombatStats.HealthStatus.Data ==
                        Constants.HealthStatuses.RemoveAtZero)
                    {
                        charsToRemove.Add(chars[0][i]);
                    }
                }

                FlushCharacterQueue();
            }
        }

        /// <summary>
        /// Advances to the next round, recomputing the character order,
        /// storing character history and triggering round events.
        /// </summary>
        public bool NextRound()
        {
            if (HasNextRound())
            {
                charsInOrder = GetTurnOrder();
                character = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if there are still 2+ non-allied characters in combat.
        /// </summary>
        public bool HasNextRound()
        {
            return chars[0].Count != 0 && chars[0].Any(o =>
                o.TeamID != chars[0][0].TeamID);
        }

        /// <summary>
        /// Advances to the next turn without performing it. Returns false if
        /// there is no next turn, else true.
        /// </summary>
        public bool NextTurn()
        {
            int charIndex = charsInOrder.IndexOf(character);

            if (charIndex < charsInOrder.Count - 1)
            {
                character = charsInOrder[charIndex + 1];
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if there is another turn left in the round.
        /// </summary>
        public bool HasNextTurn()
        {
            int charIndex = charsInOrder.IndexOf(character);
            return charIndex < charsInOrder.Count - 1;
        }

        /// <summary>
        /// Adds and removes characters in queue, then clears queues.
        /// </summary>
        private void FlushCharacterQueue()
        {
            if (chars.Count == 0 || charsInOrder.Count == 0)
            {
                return;
            }

            for (int i = 0; i < charsToRemove.Count; i++)
            {
                chars[0].RemoveAll(o => o == charsToRemove[i]);
                charsInOrder.RemoveAll(o => o == charsToRemove[i]);
                CharacterRemoved?.Invoke(charsToRemove[i]);
                charsToRemove[i].RaiseCharacterRemoved();
            }

            charsToAdd.ForEach(o =>
            {
                o.CharStats.SpeedDelay = -o.CombatStats.MoveSpeed.Data;
                if (o.MoveSelectBehavior.Perform(chars).Count != 0)
                {
                    o.CharStats.SpeedDelay += o.MoveSelectBehavior.ChosenMoves.Sum(p => p.MoveSpeedDelay);
                }
            });

            int maxDelay = (chars[0].Count > 0)
                ? chars[0].Max(o => o.CharStats.SpeedDelay)
                : 0;

            charsToAdd.ForEach(o =>
            {
                chars[0].Add(o);
                charsInOrder.Add(o);
                CharacterAdded?.Invoke(o);
                o.RaiseCharacterAdded();

                if (SpeedCarriesOver)
                {
                    o.CharStats.SpeedDelay += o.CharStats.AccumulatedSpeed - maxDelay;
                    o.CharStats.AccumulatedSpeed = o.CharStats.SpeedDelay;
                }
            });

            charsInOrder = (chars[0].Count > 0)
                ? chars[0].OrderBy(o => o.CharStats.SpeedDelay).ToList()
                : new List<Character>();
        }

        /// <summary>
        /// Returns a copy of the characters list.
        /// </summary>
        public List<List<Character>> GetChars()
        {
            List<List<Character>> combatHistory = new List<List<Character>>();
            for (int i = 0; i < chars.Count; i++)
            {
                combatHistory.Add(new List<Character>(chars[i]));
            }

            return combatHistory;
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
            charsToAdd.Remove(oldChar);
            charsToRemove.Add(oldChar);
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
            for (int i = 0; i < chars.Count; i++)
            {
                charsToRemove.Add(chars[0][i]);
            }
        }

        /// <summary>
        /// Clears characters, history and geometry.
        /// </summary>
        public void ResetSession()
        {
            chars = new List<List<Character>>();
            charsInOrder = new List<Character>();
            character = null;
            charsToRemove = new List<Character>();
            charsToAdd = new List<Character>();
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
        /// A list of all characters will be given.
        /// </param>
        public void ComputeCombat(Action<List<Character>> beforeRound)
        {
            while (true)
            {
                //Stops combat if there's one or no characters left.
                if (characters.Count <= 1)
                {
                    break;
                }

                int teamId = characters.Where(o => o != null).First().TeamID;
                if (characters.TrueForAll(o => o.TeamID == teamId))
                {
                    //Reassigns all characters to different teams.
                    if (FreeForAllEnabled)
                    {
                        for (int i = 0; i < characters.Count; i++)
                        {
                            characters[i].TeamID = i;
                        }
                    }

                    //Exits combat.
                    else
                    {
                        break;
                    }
                }

                beforeRound?.Invoke(new List<Character>(characters));
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
                    .ForEach((object o) => o?.Invoke(characters));
            }));

            //Executes the current action of each character in order.
            for (int i = 0; i < charsInOrder.Count; i++)
            {
                character = charsInOrder[i];

                //Skips invalid characters.
                if (charactersToRemove.Contains(character) ||
                    !characters.Contains(character))
                {
                    continue;
                }

                //Removes characters that aren't combat-enabled.
                var charsLeft = charsInOrder.Where(o => o.DoRemoveFromCombat.Data == true).ToList();
                charsInOrder.Except(charsLeft).ToList().ForEach((chr) => RemoveFromCombat(chr));

                //Executes the current action.
                if (character.CombatActionsEnabled?.Data == true)
                {
                    character.Actions[Constants.CharacterEvents.TurnStart]
                        .ForEach(o => o?.Invoke(characters));
                    character.Actions[Constants.CharacterEvents.CombatTarget]
                        .ForEach(o => o?.Invoke(characters));
                    character.Actions[Constants.CharacterEvents.CombatMovement]
                        .ForEach(o => o?.Invoke(characters));
                    character.Actions[Constants.CharacterEvents.CombatAction]
                        .ForEach(o => o?.Invoke(characters));
                }

                //Stops combat if set from within an invoked action.
                if (doStopCombat)
                {
                    break;
                }
            }

            isInCombat = false;
            character = null;

            //Fires round-ending hooks for each character.
            charsInOrder.ForEach((Action<Character>)((c) =>
            {
                c.Actions[Constants.CharacterEvents.RoundEnding]
                    .ForEach((object o) => o?.Invoke(characters));
            }));

            //Handles queued addition and removal from combat.
            for (int i = 0; i < charactersToAdd.Count; i++)
            {
                AddToCombat(charactersToAdd[i]);
            }
            for (int i = 0; i < charactersToRemove.Count; i++)
            {
                RemoveFromCombat(charactersToRemove[i]);
            }
            charactersToAdd.Clear();
            charactersToRemove.Clear();
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
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();

                //Performs attacks on each targeted character.
                if (character.TargetChars?.Data.Count > 0)
                {
                    for (int i = 0; i < character.TargetChars.Data.Count; i++)
                    {
                        Attack(session, character, character.TargetChars.Data[i], false, 0);
                    }
                }

                //Performs splash attacks based on a radius.
                if (character.TargetAreas?.Data.Count > 0)
                {
                    for (int i = 0; i < character.TargetAreas.Data.Count; i++)
                    {
                        for (int j = 0; j < characters.Count; j++)
                        {
                            if (characters[j] == character)
                            {
                                continue;
                            }

                            int x1 = character.TargetAreas?.Data[i]?.Item1 ?? 0;
                            int y1 = character.TargetAreas?.Data[i]?.Item2 ?? 0;
                            float rad = character.TargetAreas?.Data[i]?.Item3 ?? 0;
                            float x2 = characters[j].Location?.Data?.Item1 ?? 0;
                            float y2 = characters[j].Location?.Data?.Item2 ?? 0;
                            double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                            if (dist <= rad && rad > 0)
                            {
                                if (character.SplashBehavior?.Data ==
                                    Constants.CombatSplashBehaviors.All)
                                {
                                    Attack(session, character, characters[j], true, (dist / rad) * 100);
                                }
                                else if (character.SplashBehavior?.Data ==
                                    Constants.CombatSplashBehaviors.Allies &&
                                    characters[j].TeamID == character.TeamID)
                                {
                                    Attack(session, character, characters[j], true, (dist / rad) * 100);
                                }
                                else if (character.SplashBehavior?.Data ==
                                    Constants.CombatSplashBehaviors.Enemies &&
                                    characters[j].TeamID != character.TeamID)
                                {
                                    Attack(session, character, characters[j], true, (dist / rad) * 100);
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
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();
                float x1 = character.Location.Data.Item1;
                float y1 = character.Location.Data.Item1;

                //Moves towards the locked-on target as needed.
                if (character.TargetChars.Data.Count > 0 &&
                    characters.Contains(character.TargetChars.Data[0]) &&
                    character.TargetChars.Data[0].CombatEnabled.Data &&
                    character.TargetPersistent.Data)
                {
                    float x2 = character.TargetChars.Data[0].Location.Data.Item1;
                    float y2 = character.TargetChars.Data[0].Location.Data.Item1;
                    double distance = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
                    double angle = Math.Atan2(y2 - y1, x2 - x1);

                    //Moves towards the character.
                    double moveDist = distance - character.MaxRangeAllowed.Data;
                    if (Math.Abs(moveDist) > character.MovementRate.Data)
                    {
                        moveDist = character.MovementRate.Data * Math.Sign(moveDist);
                    }

                    character.Location.Data = new Tuple<float, float>(
                        x1 + (float)(Math.Cos(angle) * moveDist),
                        y1 + (float)(Math.Sin(angle) * moveDist));
                }

                //Moves towards the nearest character; sets to target when in range.
                else
                {
                    double angle = 0;
                    double distance = double.MaxValue;
                    int charIndex = -1;

                    //Records the nearest character with magnitude and angle.
                    for (int i = 0; i < characters.Count; i++)
                    {
                        if (characters[i] == character)
                        {
                            continue;
                        }

                        float x2 = characters[i].Location.Data.Item1;
                        float y2 = characters[i].Location.Data.Item2;
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
                        if (distance >= character.MinRangeRequired.Data &&
                            distance <= character.MaxRangeAllowed.Data)
                        {
                            if (!character.TargetChars.Data.Contains(characters[charIndex]))
                            {
                                character.TargetChars.Data.Add(characters[charIndex]);
                            }
                        }
                        else if (character.TargetChars.Data.Contains(characters[charIndex]))
                        {
                            character.TargetChars.Data.Remove(characters[charIndex]);
                        }

                        //Moves towards the character.
                        double moveDist = distance - character.MaxRangeAllowed.Data;
                        if (Math.Abs(moveDist) > character.MovementRate.Data)
                        {
                            moveDist = character.MovementRate.Data * Math.Sign(moveDist);
                        }

                        character.Location.Data = new Tuple<float, float>(
                            x1 + (float)(Math.Cos(angle) * moveDist),
                            y1 + (float)(Math.Sin(angle) * moveDist));
                    }
                }
            });
        }

        /// <summary>
        /// Moves a character towards the first target in their targets list.
        /// </summary>
        public static Action<List<Character>> MoveToTarget(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();
                var firstTarget = character.TargetChars.Data.FirstOrDefault();

                if (character.MovementRate.Data > 0 && firstTarget != null)
                {
                    float x1 = character.Location?.Data?.Item1 ?? 0;
                    float y1 = character.Location?.Data?.Item2 ?? 0;
                    float x2 = firstTarget.Location?.Data?.Item1 ?? 0;
                    float y2 = firstTarget.Location?.Data?.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveDist = character.MovementRate.Data;
                    if (moveDist > dist)
                    {
                        moveDist = dist;
                    }
                    double moveX = Math.Cos(moveAngle) * moveDist;
                    double moveY = Math.Sin(moveAngle) * moveDist;

                    character.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Moves a character until their first target is just within attack
        /// range.
        /// </summary>
        public static Action<List<Character>> MoveToTargetMaxRange(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();
                var firstTarget = character.TargetChars.Data.FirstOrDefault();

                if (character.MovementRate.Data > 0 && firstTarget != null)
                {
                    float x1 = character.Location?.Data?.Item1 ?? 0;
                    float y1 = character.Location?.Data?.Item2 ?? 0;
                    float x2 = firstTarget.Location?.Data?.Item1 ?? 0;
                    float y2 = firstTarget.Location?.Data?.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveDist = character.MovementRate.Data;
                    if (moveDist > dist - character.MaxRangeAllowed.Data)
                    {
                        moveDist = dist - character.MaxRangeAllowed.Data;
                    }

                    double moveX = Math.Cos(moveAngle) * moveDist;
                    double moveY = Math.Sin(moveAngle) * moveDist;

                    character.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Moves a character towards the average location computed from the
        /// position of all their targets.
        /// </summary>
        public static Action<List<Character>> MoveToTargets(Session session)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();

                if (character.MovementRate.Data > 0 &&
                    character.TargetChars.Data.Count > 0)
                {
                    float x1 = character.Location?.Data?.Item1 ?? 0;
                    float y1 = character.Location?.Data?.Item2 ?? 0;
                    float x2 = character.TargetChars.Data
                        .Average(o => o?.Location?.Data?.Item1 ?? 0);
                    float y2 = character.TargetChars.Data
                        .Average(o => o?.Location?.Data?.Item2 ?? 0);
                    double moveAngle = Math.Atan2(y2 - y1, x2 - x1);

                    double moveX = Math.Cos(moveAngle) * character.MovementRate.Data;
                    double moveY = Math.Sin(moveAngle) * character.MovementRate.Data;

                    character.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Moves the character within range of the nearest other if it has
        /// no targets.
        /// </summary>
        /// <param name="filter">
        /// Filters the candidates for the nearest character by team
        /// allegiance.
        /// </param>
        public static Action<List<Character>> MoveToNearestUntargeted(
            Session session,
            Constants.CombatTargetsAllowed filter)
        {
            return new Action<List<Character>>((chrs) =>
            {
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();

                //Exits if there are targets.
                if (character.TargetChars.Data.Count != 0)
                {
                    return;
                }

                //Filters possible characters by team allegiance.
                if (filter == Constants.CombatTargetsAllowed.OnlyAllies)
                {
                    characters = characters.Where(o => character.TeamID == o.TeamID && character != o).ToList();
                }
                else if (filter == Constants.CombatTargetsAllowed.OnlyEnemies)
                {
                    characters = characters.Where(o => character.TeamID != o.TeamID).ToList();
                }
                else
                {
                    characters = characters.Where(o => character != o).ToList();
                }

                //Organize characters by distance to the character.
                float x1 = character.Location?.Data?.Item1 ?? float.MinValue;
                float y1 = character.Location?.Data?.Item2 ?? float.MinValue;
                double minimumDist = -1;
                Tuple<float, float> minimumLoc = null;

                //Records the minimum location to the nearest other character.
                for (int i = 0; i < characters.Count; i++)
                {
                    float x2 = characters[i].Location?.Data.Item1 ?? 0;
                    float y2 = characters[i].Location?.Data.Item2 ?? 0;
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
                        minimumLoc.Item2 - character.Location.Data.Item2,
                        minimumLoc.Item1 - character.Location.Data.Item1);

                    double moveDist = character.MovementRate.Data;
                    if (moveDist > minimumDist - character.MaxRangeAllowed.Data)
                    {
                        moveDist = minimumDist - character.MaxRangeAllowed.Data;
                    }

                    double moveX = Math.Cos(moveAngle) * moveDist;
                    double moveY = Math.Sin(moveAngle) * moveDist;

                    character.Location.Data = new Tuple<float, float>(
                        x1 + (float)moveX, y1 + (float)moveY);
                }
            });
        }

        /// <summary>
        /// Targets the nearest X characters based on the chosen filter
        /// that fill within the character's attack range. Does not clear
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
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();

                //Filters possible characters by team allegiance.
                if (filter == Constants.CombatTargetsAllowed.OnlyAllies)
                {
                    characters = characters.Where(o => character.TeamID == o.TeamID && character != o).ToList();
                }
                else if (filter == Constants.CombatTargetsAllowed.OnlyEnemies)
                {
                    characters = characters.Where(o => character.TeamID != o.TeamID).ToList();
                }

                //Organize characters by distance to the character.
                float x1 = character.Location?.Data?.Item1 ?? float.MinValue;
                float y1 = character.Location?.Data?.Item2 ?? float.MinValue;
                var charDists = new List<Tuple<Character, double>>();

                for (int i = 0; i < characters.Count; i++)
                {
                    float x2 = characters[i].Location?.Data.Item1 ?? 0;
                    float y2 = characters[i].Location?.Data.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                    if (character.MinRangeRequired.Data <= dist &&
                        character.MaxRangeAllowed.Data >= dist)
                    {
                        charDists.Add(new Tuple<Character, double>(characters[i], dist));
                    }
                }

                charDists = charDists.OrderBy(o => o.Item2).ToList();

                //Sets the targets, skipping redundant targets.
                for (int i = 0; i < charDists.Count && i < numberOfTargets; i++)
                {
                    if (!character.TargetChars.Data.Contains(charDists[i].Item1))
                    {
                        character.TargetChars.Data.Add(charDists[i].Item1);
                    }
                }
            });
        }

        /// <summary>
        /// Targets the farthest X characters based on the chosen filter
        /// that fall within the character's attack range. Does not clear
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
                Character character = session.GetCharacter();
                List<Character> characters = session.Getcharacters();

                //Filters possible characters by team allegiance.
                if (filter == Constants.CombatTargetsAllowed.OnlyAllies)
                {
                    characters = characters.Where(o => character.TeamID == o.TeamID && character != o).ToList();
                }
                else if (filter == Constants.CombatTargetsAllowed.OnlyEnemies)
                {
                    characters = characters.Where(o => character.TeamID != o.TeamID).ToList();
                }

                //Organize characters by distance to the character.
                float x1 = character.Location?.Data?.Item1 ?? float.MinValue;
                float y1 = character.Location?.Data?.Item2 ?? float.MinValue;
                var charDists = new List<Tuple<Character, double>>();

                for (int i = 0; i < characters.Count; i++)
                {
                    float x2 = characters[i].Location?.Data.Item1 ?? 0;
                    float y2 = characters[i].Location?.Data.Item2 ?? 0;
                    double dist = Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));

                    if (character.MinRangeRequired.Data <= dist &&
                        character.MaxRangeAllowed.Data >= dist)
                    {
                        charDists.Add(new Tuple<Character, double>(characters[i], dist));
                    }
                }

                charDists = charDists.OrderByDescending(o => o.Item2).ToList();

                //Sets the targets, skipping redundant targets.
                for (int i = 0; i < charDists.Count && i < numberOfTargets; i++)
                {
                    if (!character.TargetChars.Data.Contains(charDists[i].Item1))
                    {
                        character.TargetChars.Data.Add(charDists[i].Item1);
                    }
                }
            });
        }

        /// <summary>
        /// Performs an attack against the given opponent with the character.
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
        /// characters within a radius of its epicenter given in x,y coords.
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
            Character character = session.GetCharacter();
            List<Character> characters = session.Getcharacters();

            //Exits if any character is invalid.
            if (!characters.Contains(attacker) ||
                !characters.Contains(opponent))
            {
                attacker?.Actions[Constants.CharacterEvents.TargetInvalid]
                    ?.ForEach(o => o?.Invoke(characters));
                return;
            }

            //Event hooks for initializing an attack.
            attacker?.Actions[Constants.CharacterEvents.TargetsSelected]
                ?.ForEach(o => o?.Invoke(characters));

            if (isSplashAttack)
            {
                attacker?.Actions[Constants.CharacterEvents.AttackingByArea]
                    ?.ForEach(o => o?.Invoke(characters));
            }
            else
            {
                attacker?.Actions[Constants.CharacterEvents.AttackingTarget]
                    ?.ForEach(o => o?.Invoke(characters));
            }

            opponent?.Actions[Constants.CharacterEvents.Targeted]
                ?.ForEach(o => o?.Invoke(characters));

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
                        ?.ForEach(o => o?.Invoke(characters));
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
                    ?.ForEach(o => o?.Invoke(characters));

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
                            ?.ForEach(o => o?.Invoke(characters));

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
                                ?.ForEach(o => o?.Invoke(characters));

                            attacker.Health.Data -=
                                (opponent.ConstantKnockback.Data +
                                (int)(opponent.PercentKnockback.Data * dmg / 100));
                        }

                        if (attHealth > 0 && attacker.Health.Data <= 0)
                        {
                            if (attacker.CombatHeathStatus.Data == Constants.HealthStatuses.RemoveAtZero)
                            {
                                attacker?.Actions[Constants.CharacterEvents.NoHealth]
                                    ?.ForEach(o => o?.Invoke(characters));
                                session.RemoveFromCombat(attacker);
                            }
                        }
                        if (oppHealth > 0 && opponent.Health.Data <= 0)
                        {
                            if (opponent.CombatHeathStatus.Data == Constants.HealthStatuses.RemoveAtZero)
                            {
                                opponent?.Actions[Constants.CharacterEvents.NoHealth]
                                    ?.ForEach(o => o?.Invoke(characters));
                                session.RemoveFromCombat(opponent);
                            }
                        }
                    }
                }
            }
            else
            {
                attacker?.Actions[Constants.CharacterEvents.AttackMissed]
                    ?.ForEach(o => o?.Invoke(characters));
            }
        }
        #endregion
        */
    }
}
