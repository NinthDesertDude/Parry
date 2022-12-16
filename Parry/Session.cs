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
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a combat module to handle turn-based combat logic.
        /// </summary>
        public Session()
        {
            ResetSession();
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
        /// Starts combat. Call this function after adding all characters that
        /// will participate in the first round, and before any functions that
        /// deal with turns and rounds. Unlike resetting the session, this does
        /// not clear the characters to add (which is how to add characters at
        /// the start of the combat).
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

            character.MoveSelectBehavior.Moves.ForEach(o => o.RefreshForNextTurn());
            List <Move> moves = PerformMoveSelect();
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
            }

            // Action
            BeforeMove?.Invoke();
            character.RaiseBeforeMove();

            for (int i = 0; i < moves.Count; i++)
            {
                PerformMove(targetLists[i], moves[i]);
            }

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
        }
        #endregion
    }
}
