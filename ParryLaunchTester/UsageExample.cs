using Parry;
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
            Random rng = new Random();
            int choice;

            Out("You're walking through a dank dungeon when you encounter a bandit!", true);
            Character player = GenerateCharacter("player", 0);
            player.MoveSelectBehavior.Moves.Add(new Move());
            player.MoveSelectBehavior.GetMoves = (combatHistory, motives, moves) => { return moves; };
            player.DefaultMovementBeforeBehavior.Targets = new List<Character>();
            player.DefaultMovementAfterBehavior.Targets = new List<Character>();
            player.CharStats.MaxHealth.Data = 40;
            player.CharStats.Health.Data = 40;
            player.CombatStats.MovementRate.Data = 3;
            player.CombatStats.MinDamage.Data[0] = 2;
            player.CombatStats.MaxDamage.Data[0] = 10;
            player.CombatStats.MaxRangeAllowed.Data = 6;
            player.CombatStats.PercentToHit.Data = 90;
            player.CombatStats.PercentToCritHit.Data[0] = 10;
            player.CombatStats.MoveSpeed.Data = 3;
            player.CharStats.Location.Data = new Tuple<float, float>(50, 40);

            Character enemy1 = GenerateCharacter("Bunny", 1);
            enemy1.MoveSelectBehavior.Moves.Add(new Move());
            enemy1.CharStats.MaxHealth.Data = 20;
            enemy1.CharStats.Health.Data = 20;
            enemy1.CombatStats.MovementRate.Data = 20;
            enemy1.CombatStats.MinDamage.Data[0] = 1;
            enemy1.CombatStats.MaxDamage.Data[0] = 5;
            enemy1.CombatStats.MaxRangeAllowed.Data = 2;
            enemy1.CombatStats.MoveSpeed.Data = 10;
            enemy1.CharStats.Location.Data = new Tuple<float, float>(30, -10);

            Character enemy2 = GenerateCharacter("Bandit", 1);
            enemy2.MoveSelectBehavior.Moves.Add(new Move());
            enemy2.CharStats.MaxHealth.Data = 20;
            enemy2.CharStats.Health.Data = 20;
            enemy2.CombatStats.MovementRate.Data = 20;
            enemy2.CombatStats.MinDamage.Data[0] = 1;
            enemy2.CombatStats.MaxDamage.Data[0] = 10;
            enemy2.CombatStats.MaxRangeAllowed.Data = 2;
            enemy2.CombatStats.MoveSpeed.Data = 10;
            enemy2.CharStats.Location.Data = new Tuple<float, float>(0, 45);

            session.AddCharacter(player);
            session.AddCharacter(enemy1);
            session.AddCharacter(enemy2);
            session.StartSession();

            while (true)
            {
                #region Choose movement
                choice = OutMenu("Move to...", "A target", "A location", "Skip");
                List<string> targetNames = new List<string>();

                if (choice == 1)
                {
                    var targets = session.GetChars()[0].Where(o => o != player).ToList();
                    for (int i = 0; i < targets.Count; i++)
                    {
                        targetNames.Add(targets[i].CombatStats.CustomStats["name"] as string);
                    }
                    choice = OutMenu("Select target to move towards", targetNames.ToArray());
                    player.DefaultMovementBeforeBehavior.TargetLocations.Clear();
                    player.DefaultMovementBeforeBehavior.TargetLocations.Add(targets.FirstOrDefault(o =>
                        o.CombatStats.CustomStats["name"] as string == targetNames[choice - 1]).CharStats.Location.Data);
                }
                else if (choice == 2)
                {
                    while (true)
                    {
                        Out("Enter x,y: ");
                        string input = Console.ReadLine();
                        string[] inputSegments = input.Split(',');
                        if (inputSegments.Length == 2 &&
                            int.TryParse(inputSegments[0].Trim(), out int xPos) &&
                            int.TryParse(inputSegments[1].Trim(), out int yPos))
                        {
                            player.DefaultMovementBeforeBehavior.TargetLocations.Clear();
                            player.DefaultMovementBeforeBehavior.TargetLocations.Add(new Tuple<float, float>(xPos, yPos));
                            break;
                        }
                    }
                }
                else if (choice == 3)
                {
                    player.CombatMovementAfterEnabled.Data = false;
                    player.CombatMovementBeforeEnabled.Data = false;
                }
                #endregion

                #region Choose targets
                var enemies = session.GetChars()[0].Where(o => o != player).ToList();
                targetNames = new List<string>();
                for (int i = 0; i < enemies.Count; i++)
                {
                    targetNames.Add(enemies[i].CombatStats.CustomStats["name"] as string);
                }

                choice = OutMenu("Select targets", targetNames.ToArray());
                player.DefaultTargetBehavior.OverrideTargets = new List<Character>();
                player.DefaultTargetBehavior.OverrideTargets.Add(enemies.FirstOrDefault(
                    o => o.CombatStats.CustomStats["name"] as string == targetNames[choice - 1]));
                #endregion

                session.ExecuteRound();
                if (!session.NextRound())
                {
                    if (player.CharStats.Health.Data <= 0)
                    {
                        OutRed("You've died.", true);
                    }
                    else
                    {
                        OutGreen("You've won!", true);
                    }

                    Console.ReadKey();
                    break;
                }
            }
        }

        public static Character GenerateCharacter(string name, int teamId)
        {
            Character chr = new Character();
            chr.CombatStats = new CombatStats();
            chr.CombatStats.CustomStats.Add("name", name);
            chr.TeamID = teamId;

            chr.TurnStart += () => {
                OutCyan($"{chr.CombatStats.CustomStats["name"]}'s turn. Location: ({chr.CharStats.Location.Data.Item1},{chr.CharStats.Location.Data.Item2}).");
            };

            chr.AttackMissed += (a) => {
                if (a != null)
                {
                    Out("", true);
                    Out($"Missed {a.CombatStats.CustomStats["name"]}. ");
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
                Out($"{chr.CombatStats.CustomStats["name"]} takes a swing. ");
                Out("", true);
                Out($"{a.CombatStats.CustomStats["name"]} takes {b.Sum()} damage and has {a.CharStats.Health.Data - b.Sum()} health. ");
            };

            chr.AttackRecoil += (a, b, c) => {
                if (b > 0)
                {
                    Out("", true);
                    OutGreen($"Knocked {a.CombatStats.CustomStats["name"]} to ");
                    OutCyan($"({c.Item1}, {c.Item2}). ");
                }
            };

            chr.AttackKnockback += (a, b, c) => {
                if (c > 0)
                {
                    Out("", true);
                    OutGreen($"{a.CombatStats.CustomStats["name"]} took {c} knockback damage and has {a.CharStats.Health.Data - c} health. ");
                }
            };

            chr.TurnEnd += () => {
                Out("", true);
                Out("", true);
            };

            chr.MovementBeforeSelected += (a) =>
            {
                if (chr.CharStats.Location.Data.Item1 != a.Item1 ||
                    chr.CharStats.Location.Data.Item2 != a.Item2)
                {
                    Out("", true);
                    OutCyan($"Moving from ({a.Item1}, {a.Item2}) to ({chr.CharStats.Location.Data.Item1},{chr.CharStats.Location.Data.Item2}).");
                }
            };

            chr.MovementAfterSelected += (a) =>
            {
                if (chr.CharStats.Location.Data.Item1 != a.Item1 ||
                    chr.CharStats.Location.Data.Item2 != a.Item2)
                {
                    Out("", true);
                    OutCyan($"Moving from ({a.Item1}, {a.Item2}) to ({chr.CharStats.Location.Data.Item1},{chr.CharStats.Location.Data.Item2}).");
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
