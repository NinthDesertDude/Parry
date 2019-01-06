using System;
using System.Collections.Generic;
using System.Linq;
using Parry;
using Parry.Combat;

namespace ParryLaunchTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Session session = new Session();
            session.FreeForAllEnabled = true;

            var generateCharacter = new Func<string, int, Character>((name, team) =>
            {
                Stats stats = new Stats();
                stats.MovementRate.Data = 1;
                stats.MinRangeRequired.Data = 1;
                stats.MaxRangeAllowed.Data = 3;
                stats.MinDamage.Data[0] = 10;
                stats.MaxDamage.Data[0] = 10;
                stats.CustomStats.Add("name", name);

                Character chr = new Character();
                chr.Stats = stats;
                chr.TeamID = team;

                chr.TurnStart += () => { Console.Write($"{chr.Stats.CustomStats["name"]} takes a swing. "); };
                chr.AttackMissed += (a) => { Console.Write($"Missed {a.WrappedChar.Stats.CustomStats["name"]}. "); };
                chr.AttackCritHit += (a, b) => { Console.Write("Critical hit! "); };
                chr.AttackBeforeDamage += (a, b) => { Console.Write($"{a.WrappedChar.Stats.CustomStats["name"]} takes {b.Sum()} damage and has {a.CurrentHealth} health. "); };
                chr.AttackRecoil += (a, b, c) => { Console.Write($"Recoil {b}."); };

                return chr;
            });

            session.AddCharacter(generateCharacter("Adam", 1));
            session.AddCharacter(generateCharacter("Bob", 2));

            session.NextRound();
            while (session.NextTurn()) {
                session.ExecuteTurn(false);
            }
            Console.ReadKey();
        }
    }
}
