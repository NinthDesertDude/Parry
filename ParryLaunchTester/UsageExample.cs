using Parry;
using Parry.Combat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ParryLaunchTester
{
    public static class UsageExample
    {
        public static void Example()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Session session = new Session();
            session.FreeForAllEnabled = true;
            Random rng = new Random();

            Out("You're walking through a dank dungeon when you encounter a bandit!", true);
            Character player = GenerateCharacter("player", 0);
            player.DefaultMovementBeforeBehavior.Targets = new List<Combatant>();
            player.DefaultMovementAfterBehavior.Targets = new List<Combatant>();
            player.Health.Data = 20;
            player.Stats.MovementRate.Data = 3;
            player.Stats.MinDamage.Data[0] = 1;
            player.Stats.MaxDamage.Data[0] = 1;
            player.Stats.MaxRangeAllowed.Data = 1;
            player.Stats.MoveSpeed.Data = 3;
            player.Location.Data = new Tuple<float, float>(50, 40);

            Character enemy1 = GenerateCharacter("Bunny", 1);
            enemy1.Health.Data = 20;
            enemy1.Stats.MovementRate.Data = 20;
            enemy1.Stats.MinDamage.Data[0] = 100;
            enemy1.Stats.MaxDamage.Data[0] = 100;
            enemy1.Stats.MaxRangeAllowed.Data = 2;
            enemy1.Stats.MoveSpeed.Data = 10;
            enemy1.Location.Data = new Tuple<float, float>(30, -10);

            Character enemy2 = GenerateCharacter("Bandit", 1);
            enemy2.Health.Data = 20;
            enemy2.Stats.MovementRate.Data = 20;
            enemy2.Stats.MinDamage.Data[0] = 100;
            enemy2.Stats.MaxDamage.Data[0] = 100;
            enemy2.Stats.MaxRangeAllowed.Data = 2;
            enemy2.Stats.MoveSpeed.Data = 10;
            enemy2.Location.Data = new Tuple<float, float>(0, 45);

            session.AddCharacter(player);
            session.AddCharacter(enemy1);
            session.AddCharacter(enemy2);
            session.StartSession();

            while (true)
            {
                int choice = OutMenu("Combat Menu", "Inventory", "Start Turn", "Skip Turn");

                if (choice == 1)
                {
                    choice = OutMenu("Inventory Menu", "Equip stick", "Equip longsword", "Equip God's Blade");
                    if (choice == 1)
                    {
                        player.Stats.MinDamage.Data[0] = 1;
                        player.Stats.MaxDamage.Data[0] = 1;
                        player.Stats.MaxRangeAllowed.Data = 1;
                        player.Stats.PercentToHit.Data = 100;
                        player.Stats.PercentToCritHit.Data[0] = 0;
                        player.Stats.MoveSpeed.Data = 3;
                        OutGreen("Stick equipped.", true);
                    }
                    else if (choice == 2)
                    {
                        player.Stats.MinDamage.Data[0] = 2;
                        player.Stats.MaxDamage.Data[0] = 10;
                        player.Stats.MaxRangeAllowed.Data = 6;
                        player.Stats.PercentToHit.Data = 90;
                        player.Stats.PercentToCritHit.Data[0] = 10;
                        player.Stats.MoveSpeed.Data = 3;
                        OutGreen("Longsword equipped.", true);
                    }
                    else if (choice == 3)
                    {
                        player.Stats.MinDamage.Data[0] = 5;
                        player.Stats.MaxDamage.Data[0] = 15;
                        player.Stats.MaxRangeAllowed.Data = 8;
                        player.Stats.PercentToHit.Data = 50;
                        player.Stats.PercentToCritHit.Data[0] = 15;
                        player.Stats.MoveSpeed.Data = 1;
                        OutGreen("God's Blade equipped.", true);
                    }
                }
                else if (choice == 2)
                {
                    player.CombatMoveEnabled.Data = true;
                    player.CombatMovementEnabled.Data = true;
                    player.CombatMoveSelectEnabled.Data = true;
                    player.CombatTargetingEnabled.Data = true;

                    // Initial movement.
                    choice = OutMenu("Movement", "Based on target", "By location", "None");

                    if (choice == 1)
                    {
                        var targets = session.GetCombatants()[0].Where(o => o.WrappedChar != player).ToList();
                        List<string> targetNames = new List<string>();
                        for (int i = 0; i < targets.Count; i++)
                        {
                            targetNames.Add(targets[i].WrappedChar.Stats.CustomStats["name"] as string);
                        }
                        choice = OutMenu("Select target to move towards", targetNames.ToArray());
                        player.DefaultMovementBeforeBehavior.TargetLocations.Clear();
                        player.DefaultMovementBeforeBehavior.TargetLocations.Add(targets.FirstOrDefault(o =>
                            o.WrappedChar.Stats.CustomStats["name"] as string == targetNames[choice - 1]).WrappedChar.Location.Data);
                    }
                    else if (choice == 2)
                    {
                        Out("Desired location. Use comma to separate x and y: ");

                        while (true)
                        {
                            string input = Console.ReadLine();
                            string[] inputSegments = input.Split(',');
                            if (inputSegments.Length == 2)
                            {
                                if (int.TryParse(inputSegments[0], out int xPos) &&
                                    int.TryParse(inputSegments[1], out int yPos))
                                {
                                    player.DefaultMovementBeforeBehavior.TargetLocations.Clear();
                                    player.DefaultMovementBeforeBehavior.TargetLocations.Add(new Tuple<float, float>(xPos, yPos));
                                    break;
                                }
                            }
                        }
                    }
                    else if (choice == 3)
                    {
                        player.CombatMovementEnabled.Data = false;
                    }

                    // Targeting.
                    var enemiess = session.GetCombatants()[0].Where(o => o.WrappedChar != player).ToList();
                    List<string> targetNamess = new List<string>();
                    for (int i = 0; i < enemiess.Count; i++)
                    {
                        targetNamess.Add(enemiess[i].WrappedChar.Stats.CustomStats["name"] as string);
                    }
                    choice = OutMenu("Select target", targetNamess.ToArray());
                    player.DefaultTargetBehavior.OverrideTargets = new List<Combatant>();
                    player.DefaultTargetBehavior.OverrideTargets.Add(enemiess.FirstOrDefault(
                        o => o.WrappedChar.Stats.CustomStats["name"] as string == targetNamess[choice - 1]));

                    while (session.HasNextTurn())
                    {
                        session.NextTurn();
                        session.ExecuteTurn();
                    }
                    session.NextRound();
                }
                else if (choice == 3)
                {
                    player.CombatMoveEnabled.Data = false;
                    player.CombatMovementEnabled.Data = false;
                    player.CombatMoveSelectEnabled.Data = false;
                    player.CombatTargetingEnabled.Data = false;

                    while (session.HasNextTurn())
                    {
                        session.NextTurn();
                        session.ExecuteTurn();
                    }
                    session.NextRound();
                }
            }
        }

        public static Character GenerateCharacter(string name, int teamId)
        {
            Character chr = new Character();
            chr.Stats = new Stats();
            chr.Stats.CustomStats.Add("name", name);
            chr.TeamID = teamId;

            chr.TurnStart += () => {
                OutCyan($"{chr.Stats.CustomStats["name"]}'s turn. Location: ({chr.Location.Data.Item1},{chr.Location.Data.Item2}).");
            };

            chr.AttackMissed += (a) => {
                if (a != null)
                {
                    Out("", true);
                    Out($"Missed {a.WrappedChar.Stats.CustomStats["name"]}. ");
                }
                else
                {
                    Out("", true);
                    Out($"Missed. ");
                }
            };

            chr.AttackCritHit += (a, b) => {
                Out("", true);
                OutRed("Critical hit! ");
            };

            chr.AttackBeforeDamage += (a, b) => {
                Out("", true);
                Out($"{chr.Stats.CustomStats["name"]} takes a swing. ");
                Out("", true);
                Out($"{a.WrappedChar.Stats.CustomStats["name"]} takes {b.Sum()} damage and has {a.CurrentHealth.Data - b.Sum()} health. ");
            };

            chr.AttackRecoil += (a, b, c) => {
                if (b > 0)
                {
                    Out("", true);
                    OutGreen($"Knocked {a.WrappedChar.Stats.CustomStats["name"]} to ");
                    OutCyan($"({c.Item1}, {c.Item2}). ");
                }
            };

            chr.AttackKnockback += (a, b, c) => {
                if (c > 0)
                {
                    Out("", true);
                    OutGreen($"{a.WrappedChar.Stats.CustomStats["name"]} took {c} knockback damage and has {a.CurrentHealth.Data - c} health. ");
                }
            };

            chr.TurnEnd += () => {
                Out("", true);
                Out("", true);
            };

            chr.MovementBeforeSelected += (a) =>
            {
                if (chr.Location.Data.Item1 != a.Item1 ||
                    chr.Location.Data.Item2 != a.Item2)
                {
                    Out("", true);
                    OutCyan($"Moving from ({a.Item1}, {a.Item2}) to ({chr.Location.Data.Item1},{chr.Location.Data.Item2}).");
                }
            };

            chr.MovementAfterSelected += (a) =>
            {
                if (chr.Location.Data.Item1 != a.Item1 ||
                    chr.Location.Data.Item2 != a.Item2)
                {
                    Out("", true);
                    OutCyan($"Moving from ({a.Item1}, {a.Item2}) to ({chr.Location.Data.Item1},{chr.Location.Data.Item2}).");
                }
            };

            return chr;
        }

        #region Simple output helpers
        private static void Out(string str, bool newline = false)
        {
            if (newline)
            {
                Console.WriteLine(str);
            }
            else
            {
                Console.Write(str);
            }
        }

        private static void Out(string str, ConsoleColor color, bool newline = false)
        {
            Console.ForegroundColor = color;
            Out(str, newline);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void OutRed(string str, bool newline = false)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Out(str, newline);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void OutGreen(string str, bool newline = false)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Out(str, newline);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void OutYellow(string str, bool newline = false)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Out(str, newline);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void OutPurple(string str, bool newline = false)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Out(str, newline);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void OutCyan(string str, bool newline = false)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Out(str, newline);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static int OutMenu(string title, params string[] options)
        {
            while (true)
            {
                OutYellow(title, true);
                for (int i = 0; i < options.Length; i++)
                {
                    OutYellow($"{i + 1}");
                    Out($": {options[i]}", true);
                }
                string input = Console.ReadLine();

                if (Int32.TryParse(input.Trim(), out int inputInt) &&
                    inputInt > 0 && inputInt <= options.Length)
                {
                    return inputInt;
                }
            }
        }
        #endregion
    }
}
