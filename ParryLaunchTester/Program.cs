using System;
using System.Collections.Generic;
using System.Linq;
using Parry;

namespace ParryLaunchTester
{
    class Program
    {
        static void Main(string[] args)
        {
            CombatSession session = new CombatSession();
            session.FreeForAllEnabled = true;

            #region Character Tests
            Character char1 = new Character();
            char1.TeamID = 1;
            char1.MovementRate.Data = 1;
            char1.MinRangeRequired.Data = 1;
            char1.MaxRangeAllowed.Data = 3;
            char1.Location.Data = new Tuple<float, float>(0, 0);
            char1.MinDamage.Data[0] = 1;
            char1.MaxDamage.Data[0] = 1;
            char1.Health.Data = 10;
            char1.Actions[CombatConstants.CombatEvents.CombatAction].Add(Combat.AttackAction(session));
            char1.Actions[CombatConstants.CombatEvents.CombatTarget].Add(Combat.MoveAction(session));
            char1.Actions[CombatConstants.CombatEvents.Attacking].Add(new Action<List<Character>>((a) =>
            {
                Console.WriteLine("Adam swings.");
            }));
            char1.Location.OnAfterSet += (a) =>
            {
                Console.WriteLine("Location: " +
                    String.Format("{0:0.#}", char1.Location.Data.Item1) + ", " +
                    String.Format("{0:0.#}", char1.Location.Data.Item2));
            };
            char1.Health.OnAfterSet += (a) => { Console.WriteLine("new health: " + char1.Health.Data); };

            Character char2 = new Character();
            char2.TeamID = 2;
            char2.MovementRate.Data = 1;
            char2.MinRangeRequired.Data = 1;
            char2.MaxRangeAllowed.Data = 3;
            char2.MinDamage.Data[0] = 1;
            char2.MaxDamage.Data[0] = 1;
            char2.Health.Data = 10;
            char2.Location.Data = new Tuple<float, float>(10, 0);
            char2.Actions[CombatConstants.CombatEvents.CombatAction].Add(Combat.AttackAction(session));
            char2.Actions[CombatConstants.CombatEvents.CombatTarget].Add(Combat.MoveAction(session));
            char2.Actions[CombatConstants.CombatEvents.Attacking].Add(new Action<List<Character>>((a) =>
            {
                Console.WriteLine("Bob swings.");
            }));
            char2.Location.OnAfterSet += (a) =>
            {
                Console.WriteLine("Location: " +
                    String.Format("{0:0.#}", char2.Location.Data.Item1) + ", " +
                    String.Format("{0:0.#}", char2.Location.Data.Item2));
            };
            char2.Health.OnAfterSet += (a) => { Console.WriteLine("new health: " + char2.Health.Data); };

            session.AddToCombat(char1);
            session.AddToCombat(char2);
            #endregion

            #region Randomization
            //Creates 10 characters to fight each other.
            Random rng = new Random();
            for (int i = 0; i < 10; i++)
            {
                Character newChar = new Character();
                string newCharName = "";

                //Name generation for fun.
                int randomName = rng.Next(20);
                switch (randomName)
                {
                    case 0:
                        newCharName = "Bob";
                        break;
                    case 1:
                        newCharName = "Bill";
                        break;
                    case 2:
                        newCharName = "Joe";
                        break;
                    case 3:
                        newCharName = "John";
                        break;
                    case 4:
                        newCharName = "Cathy";
                        break;
                    case 5:
                        newCharName = "Moe";
                        break;
                    case 6:
                        newCharName = "George";
                        break;
                    case 7:
                        newCharName = "Kevin";
                        break;
                    case 8:
                        newCharName = "James";
                        break;
                    case 9:
                        newCharName = "Will";
                        break;
                    case 10:
                        newCharName = "Max";
                        break;
                    case 11:
                        newCharName = "Mike";
                        break;
                    case 12:
                        newCharName = "Jeremy";
                        break;
                    case 13:
                        newCharName = "Clarke";
                        break;
                    case 14:
                        newCharName = "Josh";
                        break;
                    case 15:
                        newCharName = "Andy";
                        break;
                    case 16:
                        newCharName = "David";
                        break;
                    case 17:
                        newCharName = "Robert";
                        break;
                    case 18:
                        newCharName = "Richard";
                        break;
                    case 19:
                        newCharName = "Paul";
                        break;
                }

                //Simple attributes.
                newChar.TeamID = rng.Next(4);
                newChar.AttackSpeed.Data = 1 + rng.Next(10);
                newChar.MinDamage.Data[0] = rng.Next(4);
                newChar.MaxDamage.Data[0] = 1 + newChar.MinDamage.Data[0] + rng.Next(6);
                newChar.CritDamageMultiplier.Data[0] = 2 + rng.Next(3);
                newChar.PercentToDodge.Data = rng.Next(20);
                newChar.PercentToHit.Data = 80 + rng.Next(21);
                newChar.PercentToCritHit.Data[0] = rng.Next(31);
                newChar.Location.Data = new Tuple<float, float>(rng.Next(100), rng.Next(100));
                newChar.MinRangeRequired.Data = rng.Next(4);
                newChar.MaxRangeAllowed.Data = 2 + rng.Next(50);
                newChar.MinRangeMultiplier.Data = 0.5f + (float)rng.NextDouble();
                newChar.MaxRangeMultiplier.Data = newChar.MinRangeMultiplier.Data + (float)rng.NextDouble();
                if (rng.Next(10) == 0) { newChar.ConstantKnockback.Data = 1 + rng.Next(3); }
                if (rng.Next(30) == 0) { newChar.PercentKnockback.Data = rng.Next(20) * 5; }

                //Special support for custom characters.
                if (i == 0)
                {
                    newCharName = "God";
                    newChar.TeamID = 5;
                    newChar.CombatSpeedStatus.Data = CombatConstants.CombatSpeedStatuses.AlwaysFirst;
                    newChar.MinDamage.Data[0] = 10;
                    newChar.MaxDamage.Data[0] = 50;
                    newChar.CritDamageMultiplier.Data[0] = 2;
                    newChar.PercentToCritHit.Data[0] = 50;
                    newChar.PercentToDodge.Data = 20;
                    newChar.PercentToHit.Data = 1000;
                    newChar.MaxRangeAllowed.Data = 100;
                }

                //Name suffix generation for fun.
                if (i != 0)
                {
                    newCharName += " the";
                    if (newChar.AttackSpeed.Data >= 10) { newCharName += " supersonic"; }
                    else if (newChar.AttackSpeed.Data > 5) { newCharName += " speedy"; }
                    else if (newChar.AttackSpeed.Data < 3) { newCharName += " elderly"; }
                    else if (newChar.AttackSpeed.Data < 2) { newCharName += " slow"; }
                    if (newChar.MinDamage.Data[0] >= 3) { newCharName += " beatstick"; }
                    else if (newChar.MinDamage.Data[0] == 1) { newCharName += " wimpy"; }
                    if (newChar.MaxDamage.Data[0] > 7) { newCharName += " mc-beaty"; }
                    else if (newChar.MaxDamage.Data[0] < 2) { newCharName += " superwimp"; }
                    if (newChar.CritDamageMultiplier.Data[0] == 4) { newCharName += " lethal"; }
                    else if (newChar.CritDamageMultiplier.Data[0] == 2) { newCharName += " nonlethal"; }
                    if (newChar.PercentToDodge.Data >= 15) { newCharName += " dodgy"; }
                    else if (newChar.PercentToDodge.Data <= 5) { newCharName += " fat"; }
                    if (newChar.PercentToHit.Data == 100) { newCharName += " deadshot"; }
                    else if (newChar.PercentToHit.Data >= 95) { newCharName += " sureshot"; }
                    if (newChar.PercentToCritHit.Data[0] >= 25) { newCharName += " veteran"; }
                    if (newChar.MinRangeRequired.Data == 0) { newCharName += " nearsighted"; }
                    if (newChar.MaxRangeAllowed.Data <= 10) { newCharName += " t-rex"; }
                    else if (newChar.MaxRangeAllowed.Data >= 50) { newCharName += " sniper"; }
                    if (newChar.PercentKnockback.Data != 0) { newCharName += " spiky"; }
                    if (newChar.ConstantKnockback.Data != 0) { newCharName += " lego"; }
                    if (newCharName.EndsWith(" the")) { newCharName += " boring"; }
                }

                //Text reporting.
                newChar.Actions[CombatConstants.CombatEvents.TurnStart].Add((chrs) => { Console.WriteLine("TURN: " + newCharName); });
                newChar.Actions[CombatConstants.CombatEvents.AttackHit].Add((chrs) => { Console.WriteLine("Smack."); });
                newChar.Actions[CombatConstants.CombatEvents.AttackCritHit].Add((chrs) => { Console.WriteLine("Wham bam!"); });
                newChar.Actions[CombatConstants.CombatEvents.AttackMissed].Add((chrs) => { Console.WriteLine("Woosh."); });
                newChar.Actions[CombatConstants.CombatEvents.AttackKnockback].Add((chrs) => { Console.WriteLine("Knockback damage."); });
                newChar.Actions[CombatConstants.CombatEvents.NoHealth].Add((chrs) => { Console.WriteLine(newCharName + " died."); });
                newChar.Health.OnAfterSet += ((oldVal) =>
                {
                    Console.WriteLine((oldVal - newChar.Health.RawData) + " damage. " +
                        newCharName + " has " + newChar.Health.RawData + " health.");
                });
                newChar.Location.OnAfterSet += ((oldVal) =>
                {
                    Console.WriteLine("Moved (" +
                        (newChar.Location.RawData.Item1 - oldVal.Item1) + ", " +
                        (newChar.Location.RawData.Item2 - oldVal.Item2) + ").");
                });


                newChar.Actions[CombatConstants.CombatEvents.CombatAction].Add(Combat.AttackAction(session));
                newChar.Actions[CombatConstants.CombatEvents.CombatTarget].Add(Combat.MoveAction(session));

                session.AddToCombat(newChar);
            }

            //Lists out all character teams.
            Console.ForegroundColor = ConsoleColor.Cyan;
            int maxTeamID = session.GetCombatants().Max(o => o.TeamID);
            for (int i = 0; i <= maxTeamID; i++)
            {
                Console.WriteLine("team " + i + ": " + session.GetCombatants().Where(o => o.TeamID == i).Count());
            }
            Console.ForegroundColor = ConsoleColor.White;
            #endregion

            session.ComputeCombat(null);
            Console.ReadKey();
        }
    }
}
