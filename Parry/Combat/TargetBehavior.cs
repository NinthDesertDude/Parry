using System;
using System.Collections.Generic;
using System.Linq;

namespace Parry.Combat
{
    /// <summary>
    /// Describes the AI targeting behaviors with tendencies and
    /// a weighting system to determine the best target.
    /// </summary>
    public class TargetBehavior
    {
        #region Variables - Default Behaviors
        /// <summary>
        /// Prioritizes attackers or weak and nearby enemies.
        /// </summary>
        public static TargetBehavior Normal
        {
            get
            {
                return new TargetBehavior()
                {
                    YourThreatFactor = 2,
                    YourResistFactor = 1,
                    YourHealthDamageFactor = 10,
                    GroupAttackFactor = 1,
                    RetaliationBonus = 20,
                    InEasyRangeBonus = 20,
                    EasyDefeatBonus = 20,
                    NoRiskBonus = 10,
                    PreviousTargetBonus = 10
                };
            }
        }

        /// <summary>
        /// Prioritizes strong and nearby enemies.
        /// </summary>
        public static TargetBehavior Champion
        {
            get
            {
                return new TargetBehavior()
                {
                    ThreatOpportunityFactor = 3,
                    ResistOpportunityFactor = 3,
                    YourThreatFactor = -1,
                    YourResistFactor = -1,
                    YourHealthDamageFactor = -1,
                    GroupAttackFactor = -1,
                    RetaliationBonus = 10,
                    InEasyRangeBonus = 20,
                    PreviousTargetBonus = 5
                };
            }
        }

        /// <summary>
        /// Prioritizes strong enemies that the team can take down, and
        /// enemies already targeted by teammates.
        /// </summary>
        public static TargetBehavior Hivemind
        {
            get
            {
                return new TargetBehavior()
                {
                    TeamThreatFactor = 5,
                    TeamHealthDamageFactor = 5,
                    TeamResistFactor = 5,
                    GroupAttackFactor = 2,
                    TeamworkBonus = 2,
                    InEasyRangeBonus = 5,
                    PreviousTargetBonus = 5
                };
            }
        }

        /// <summary>
        /// Prioritizes attackers first, followed by the nearest target.
        /// </summary>
        public static TargetBehavior Aggressive
        {
            get
            {
                return new TargetBehavior()
                {
                    IsVindictive = true,
                    DistanceFactor = 1
                };
            }
        }

        /// <summary>
        /// Prioritizes weak nearby enemies.
        /// </summary>
        public static TargetBehavior Ruthless
        {
            get
            {
                return new TargetBehavior()
                {
                    InEasyRangeBonus = 10,
                    NoRiskBonus = 10,
                    EasyDefeatBonus = 10,
                    YourThreatFactor = 1,
                    PreviousTargetBonus = 10
                };
            }
        }

        /// <summary>
        /// Prioritizes targets closest to the archer or most likely to reach
        /// the archer first based on speed.
        /// </summary>
        public static TargetBehavior Archer
        {
            get
            {
                return new TargetBehavior()
                {
                    InEasyRangeBonus = -500,
                    DistanceFactor = -1,
                    EasyDefeatBonus = 10,
                    YourThreatFactor = 2,
                    YourResistFactor = 1,
                    YourHealthDamageFactor = 10,
                    PreviousTargetBonus = 50
                };
            }
        }
        #endregion

        #region Variables - Factors and Bonuses
        /// <summary>
        /// Functions that run when performing targeting, adding the resulting
        /// number to the character's score. This is a way to extend factors
        /// and bonuses. Custom logic that involves distances should use
        /// CustomDistanceFactors instead.
        /// First argument: The combat history.
        /// Second argument: The possible target.
        /// Third argument: The character in consideration.
        /// Returns: The score to add to the character.
        /// </summary>
        public List<Func<List<List<Character>>, Character, Character, float>> CustomFactors;

        /// <summary>
        /// Similar to custom factors, but these execute when recomputing
        /// distance. Since targeting occurs before first movement, targeting
        /// is updated after movement for all distance-based calculations.
        /// First argument: The combat history.
        /// Second argument: The possible target.
        /// Third argument: The character in consideration.
        /// Returns: The score to add to the character.
        /// </summary>
        public List<Func<List<List<Character>>, Character, Character, float>> CustomDistanceFactors;

        /// <summary>
        /// Generates a random value from 0 to 1 for each possible target.
        /// </summary>
        public float RandomFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Your damage ÷ their resistances.
        /// If their resistance is 0, treats it as 1.
        /// </summary>
        public float YourThreatFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Average ally damage ÷ their resistances.
        /// If their resistance is 0, treats it as 1.
        /// </summary>
        public float TeamThreatFactor
        {
            get;
            set;
        }

        /// <summary>
        /// 1 ÷ (their damage ÷ your resistances).
        /// If your resistance is 0, treats it as 1.
        /// </summary>
        public float YourResistFactor
        {
            get;
            set;
        }

        /// <summary>
        /// 1 ÷ (their damage ÷ average ally resistances).
        /// If ally resistance is 0, treats it as 1.
        /// </summary>
        public float TeamResistFactor
        {
            get;
            set;
        }

        /// <summary>
        /// yourThreatFactor ÷ teamThreatFactor.
        /// If teamThreatFactor is 0, treats it as 1.
        /// </summary>
        public float ThreatOpportunityFactor
        {
            get;
            set;
        }

        /// <summary>
        /// yourResistFactor ÷ teamResistFactor.
        /// If teamResistFactor is 0, treats it as 1.
        /// </summary>
        public float ResistOpportunityFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Your damage ÷ their health.
        /// If their health is 0, treats it as 1.
        /// If your damage >= their health, caps at 1.
        /// </summary>
        public float YourHealthDamageFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Average ally damage ÷ target's health, clamped to 1.
        /// If their health is 0, treats it as 1.
        /// If ally damage >= their health, caps at 1.
        /// </summary>
        public float TeamHealthDamageFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Average enemy distance from you ÷ their >= 1 distance from you.
        /// If their distance is 0, treats it as 1.
        /// </summary>
        public float DistanceFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Their movement rate ÷ average enemy movement rate.
        /// If average movement rate is 0, treats it as 1.
        /// </summary>
        public float MovementFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Number of characters that targeted the target in consideration.
        /// </summary>
        public float GroupAttackFactor
        {
            get;
            set;
        }

        /// <summary>
        /// Adds X for targets in attack range before moving.
        /// </summary>
        public float InEasyRangeBonus
        {
            get;
            set;
        }

        /// <summary>
        /// Adds X for targets that targeted you.
        /// </summary>
        public float RetaliationBonus
        {
            get;
            set;
        }

        /// <summary>
        /// Adds X for targets that targeted allies.
        /// </summary>
        public float TeamworkBonus
        {
            get;
            set;
        }

        /// <summary>
        /// Adds X for targets that can be dispatched in one hit by you.
        /// </summary>
        public float EasyDefeatBonus
        {
            get;
            set;
        }

        /// <summary>
        /// Adds X for targets in attack range that can't attack you after
        /// moving.
        /// </summary>
        public float NoRiskBonus
        {
            get;
            set;
        }

        /// <summary>
        /// Adds X for targets that were targeted last round. Checks in
        /// Targets.
        /// Default 0.
        /// </summary>
        public float PreviousTargetBonus
        {
            get;
            set;
        }
        #endregion

        #region Variables - Tendencies
        /// <summary>
        /// The AI considers allies as enemies and vice versa everywhere.
        /// False by default.
        /// </summary>
        public bool SwapAlliesEnemies
        {
            get;
            set;
        }

        /// <summary>
        /// The AI will not retaliate against allies that attack them.
        /// False by default.
        /// </summary>
        public bool IsLoyal
        {
            get;
            set;
        }

        /// <summary>
        /// Adds allies to the list of possible targets.
        /// False by default.
        /// </summary>
        public bool IsNeutral
        {
            get;
            set;
        }

        /// <summary>
        /// The AI will first consider those that attacked them
        /// (no effect with selfless).
        /// False by default.
        /// </summary>
        public bool IsVindictive
        {
            get;
            set;
        }

        /// <summary>
        /// The AI will first consider those that attacked allies
        /// (no effect with vindictive).
        /// False by default.
        /// </summary>
        public bool IsSelfless
        {
            get;
            set;
        }

        /// <summary>
        /// If nonzero, the weighted scores involved in selecting targets must
        /// be >= this value to select a target.
        /// Default 0.
        /// </summary>
        public int MinScoreThreshold
        {
            get;
            set;
        }
        #endregion

        #region Variables
        private static Random rng = new Random();

        /// <summary>
        /// Limits to this many targets.
        /// Default value is 1.
        /// </summary>
        public int MaxNumberTargets
        {
            get;
            set;
        }

        /// <summary>
        /// Takes a list of (x,y,radius) tuples, where x and y describe the
        /// center point of the area attack. If any tuples are provided,
        /// targeting is limited to the radii. Doesn't select a target more
        /// than once if radii overlap.
        /// </summary>
        public List<Tuple<int, int, int>> AreaTargetPoints
        {
            get;
            set;
        }

        /// <summary>
        /// When the combat system computes targets from this criteria, this
        /// list is populated with those results. Use OverrideTargets instead
        /// if you want to skip targeting logic and provide your own targets
        /// manually.
        /// </summary>
        public List<Character> Targets
        {
            get
            {
                return WeightedTargets.Select(o => o.Char).ToList();
            }
            set
            {
                WeightedTargets.Clear();
                for (int i = 0; i < value.Count; i++)
                {
                    WeightedTargets.Add(new WeightedTarget(value[i], 0, 0));
                }
            }
        }

        /// <summary>
        /// When the combat system computes targets from this criteria, this
        /// list is populated with both characters and their scores.
        /// </summary>
        private List<WeightedTarget> WeightedTargets = new List<WeightedTarget>();

        /// <summary>
        /// When set, the combat system will copy this list to the Targets list
        /// rather than recomputing the targets. This ignores any max number of
        /// targets limit.
        /// </summary>
        public List<Character> OverrideTargets
        {
            get;
            set;
        }

        /// <summary>
        /// Allows the AI to move based on their targeting behavior.
        /// </summary>
        public MovementBehavior MovementBehavior
        {
            get;
            set;
        }

        /// <summary>
        /// When targeting allies, you can be a target when enabled.
        /// </summary>
        public bool AllowSelfTargeting
        {
            get;
            set;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Sets defaults for tendencies and all factors and bonuses to 0.
        /// </summary>
        public TargetBehavior()
        {
            CustomFactors = new List<Func<List<List<Character>>, Character, Character, float>>();
            CustomDistanceFactors = new List<Func<List<List<Character>>, Character, Character, float>>();
            RandomFactor = 0;
            YourThreatFactor = 0;
            TeamThreatFactor = 0;
            YourResistFactor = 0;
            TeamResistFactor = 0;
            ThreatOpportunityFactor = 0;
            ResistOpportunityFactor = 0;
            GroupAttackFactor = 0;
            YourHealthDamageFactor = 0;
            TeamHealthDamageFactor = 0;
            DistanceFactor = 0;
            MovementFactor = 0;
            NoRiskBonus = 0;
            InEasyRangeBonus = 0;
            RetaliationBonus = 0;
            TeamworkBonus = 0;
            EasyDefeatBonus = 0;
            PreviousTargetBonus = 0;
            MinScoreThreshold = 0;
            SwapAlliesEnemies = false;
            IsLoyal = false;
            IsNeutral = false;
            IsVindictive = false;
            IsSelfless = false;
            MaxNumberTargets = 1;
            AreaTargetPoints = new List<Tuple<int, int, int>>();
            Targets = new List<Character>();
            OverrideTargets = null;
            AllowSelfTargeting = false;
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="other">
        /// The instance to copy all values from.
        /// </param>
        public TargetBehavior(TargetBehavior other)
        {
            CustomFactors = other.CustomFactors;
            CustomDistanceFactors = other.CustomDistanceFactors;
            RandomFactor = other.RandomFactor;
            YourThreatFactor = other.YourThreatFactor;
            TeamThreatFactor = other.TeamThreatFactor;
            YourResistFactor = other.YourResistFactor;
            TeamResistFactor = other.TeamResistFactor;
            ThreatOpportunityFactor = other.ThreatOpportunityFactor;
            ResistOpportunityFactor = other.ResistOpportunityFactor;
            GroupAttackFactor = other.GroupAttackFactor;
            YourHealthDamageFactor = other.YourHealthDamageFactor;
            TeamHealthDamageFactor = other.TeamHealthDamageFactor;
            DistanceFactor = other.DistanceFactor;
            MovementFactor = other.MovementFactor;
            NoRiskBonus = other.NoRiskBonus;
            InEasyRangeBonus = other.InEasyRangeBonus;
            RetaliationBonus = other.RetaliationBonus;
            TeamworkBonus = other.TeamworkBonus;
            EasyDefeatBonus = other.EasyDefeatBonus;
            PreviousTargetBonus = other.PreviousTargetBonus;
            MinScoreThreshold = other.MinScoreThreshold;
            SwapAlliesEnemies = other.SwapAlliesEnemies;
            IsLoyal = other.IsLoyal;
            IsNeutral = other.IsNeutral;
            IsVindictive = other.IsVindictive;
            IsSelfless = other.IsSelfless;
            MaxNumberTargets = other.MaxNumberTargets;
            AreaTargetPoints = other.AreaTargetPoints;
            Targets = other.Targets;
            OverrideTargets = other.OverrideTargets;
            AllowSelfTargeting = other.AllowSelfTargeting;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Computes scores for each character based on factors and bonuses,
        /// removing targets out of mobile range, then prioritizes by high
        /// score. Returns character.
        /// </summary>
        public List<Character> Perform(List<List<Character>> combatHistory, Character self)
        {
            if (OverrideTargets != null)
            {
                return OverrideTargets;
            }

            List<Character> chars = new List<Character>(combatHistory[0]);

            // Area-based targeting.
            if (AreaTargetPoints.Count > 0)
            {
                chars = new List<Character>();

                for (int i = 0; i < AreaTargetPoints.Count; i++)
                {
                    chars.AddRange(
                        combatHistory[0].Where(o =>
                        {
                            double distance = Math.Sqrt(
                            Math.Pow(o.CharStats.Location.Data.Item1
                                - AreaTargetPoints[i].Item1, 2) +
                            Math.Pow(o.CharStats.Location.Data.Item2
                                - AreaTargetPoints[i].Item2, 2));

                            return distance <= AreaTargetPoints[i].Item3;
                        }));
                }

                chars = chars.Distinct().ToList();
            }

            var weights = new List<WeightedTarget>();
            List<Character> allies = chars.Where(o => o != self && o.TeamID == self.TeamID).ToList();
            List<Character> enemies = chars.Where(o => o.TeamID != self.TeamID).ToList();

            if (AllowSelfTargeting)
            {
                allies.Add(self);
            }

            if (IsNeutral)
            {
                enemies = new List<Character>(chars);
                allies.Clear();

                if (!AllowSelfTargeting)
                {
                    enemies.Remove(self);
                }
            }

            if (!IsLoyal && combatHistory.Count > 1)
            {
                Character selfLastRound = combatHistory[1]
                    .FirstOrDefault(o => o.Id == self.Id);

                for (int i = 0; i < allies.Count; i++)
                {
                    Character allyLastRound = combatHistory[1]
                        .FirstOrDefault(o => o.Id == allies[i].Id);

                    if ((allyLastRound.MoveSelectBehavior.Motive == Constants.Motives.DamageHealth ||
                        allyLastRound.MoveSelectBehavior.Motive == Constants.Motives.LowerStats) &&
                        allyLastRound.GetTargets().Contains(selfLastRound))
                    {
                        allies.RemoveAt(i);
                        enemies.Add(allies[i]);
                    }
                }
            }

            if (SwapAlliesEnemies)
            {
                List<Character> temp = allies;
                allies = enemies;
                enemies = temp;
            }

            // Computes stats for the character.
            var resistance = (YourResistFactor != 0 || ResistOpportunityFactor != 0)
                ? self.CombatStats.DamageResistance.Data.Sum()
                : 0;

            int minDamage = 0;
            int maxDamage = 0;
            float avgDamage = 0;

            if (YourThreatFactor != 0 || YourHealthDamageFactor != 0 || ThreatOpportunityFactor != 0 || EasyDefeatBonus != 0)
            {
                minDamage = self.CombatStats.MinDamage.Data.Sum();
                maxDamage = self.CombatStats.MaxDamage.Data.Sum();
                avgDamage = minDamage + (maxDamage - minDamage) / 2;
            }

            // Computes all factors.
            for (int i = 0; i < enemies.Count; i++)
            {
                float regScore = (RandomFactor == 0) ? 0 : (float)rng.NextDouble() * RandomFactor;
                float distScore = 0;
                int theirMinDamage = 0;
                int theirMaxDamage = 0;
                float theirAvgDamage = 0;
                float theirResistance = 0;
                float teamAvgDamage = 0;
                double distance = 0;

                if (YourThreatFactor != 0 || TeamThreatFactor != 0 || ThreatOpportunityFactor != 0)
                {
                    theirResistance = enemies[i].CombatStats.DamageResistance.Data.Sum();
                }

                if (YourResistFactor != 0 || TeamResistFactor != 0 || ResistOpportunityFactor != 0)
                {
                    theirMinDamage = enemies[i].CombatStats.MinDamage.Data.Sum();
                    theirMaxDamage = enemies[i].CombatStats.MaxDamage.Data.Sum();
                    theirAvgDamage = theirMinDamage + (theirMaxDamage - theirMinDamage) / 2;
                }
                
                if (TeamThreatFactor != 0 || TeamHealthDamageFactor != 0 || ThreatOpportunityFactor != 0)
                {
                    for (int j = 0; j < allies.Count; j++)
                    {
                        for (int k = 0; k < CombatStats.NUM_TYPES_DAMAGE; k++)
                        {
                            teamAvgDamage += allies[j].CombatStats.MinDamage.Data[k] +
                                (allies[j].CombatStats.MaxDamage.Data[k] - allies[j].CombatStats.MinDamage.Data[k]) / 2;
                        }
                    }
                    teamAvgDamage = (allies.Count > 0) ? teamAvgDamage / allies.Count : 0;
                }

                if (DistanceFactor != 0
                    || NoRiskBonus != 0
                    || InEasyRangeBonus != 0
                    || self.CombatStats.MinRangeRequired.Data != 0
                    || self.CombatStats.MaxRangeAllowed.Data >= 0)
                {
                    distance = Math.Sqrt(
                    Math.Pow(enemies[i].CharStats.Location.Data.Item1
                        - self.CharStats.Location.Data.Item1, 2) +
                    Math.Pow(enemies[i].CharStats.Location.Data.Item2
                        - self.CharStats.Location.Data.Item2, 2));

                    // Allows targets in radius + potential movement radius.
                    if (distance + self.CombatStats.MovementRate.Data
                            < self.CombatStats.MinRangeRequired.Data ||
                        distance - self.CombatStats.MovementRate.Data
                            > self.CombatStats.MaxRangeAllowed.Data)
                    {
                        continue;
                    }
                }

                CustomFactors?.ForEach(o => regScore += o(combatHistory, enemies[i], self));
                CustomDistanceFactors?.ForEach(o => distScore += o(combatHistory, enemies[i], self));

                if (YourThreatFactor != 0)
                {
                    regScore += (theirResistance == 0)
                        ? YourThreatFactor * avgDamage
                        : YourThreatFactor * (avgDamage / theirResistance);
                }

                if (TeamThreatFactor != 0)
                {
                    regScore += (theirResistance == 0)
                        ? TeamThreatFactor * teamAvgDamage
                        : TeamThreatFactor * teamAvgDamage / theirResistance;
                }

                if (YourResistFactor != 0 && theirAvgDamage != 0)
                {
                    regScore += (resistance == 0)
                        ? YourResistFactor * (1 / theirAvgDamage)
                        : YourResistFactor * (1 / (theirAvgDamage / resistance));
                }

                if (TeamResistFactor != 0 && theirAvgDamage != 0)
                {
                    float teamResist = allies.Average(o => o.CombatStats.DamageResistance.Data.Sum());
                    regScore += (teamResist == 0)
                        ? TeamResistFactor * (1 / theirAvgDamage)
                        : TeamResistFactor * (1 / (theirAvgDamage / teamResist));
                }

                if (GroupAttackFactor != 0)
                {
                    int numTargeting = allies.Count(o => o.GetTargets().Contains(enemies[i]));
                    regScore += GroupAttackFactor * numTargeting;
                }

                if (YourHealthDamageFactor != 0)
                {
                    float factor = (enemies[i].CharStats.Health.Data <= 0)
                        ? avgDamage
                        : avgDamage / enemies[i].CharStats.Health.Data;

                    regScore += (factor > 1)
                        ? YourHealthDamageFactor
                        : YourHealthDamageFactor * factor;
                }

                if (TeamHealthDamageFactor != 0)
                {
                    float factor = (enemies[i].CharStats.Health.Data <= 0)
                        ? teamAvgDamage
                        : teamAvgDamage / enemies[i].CharStats.Health.Data;

                    regScore += (factor > 1)
                        ? TeamHealthDamageFactor
                        : TeamHealthDamageFactor * factor;
                }

                if (DistanceFactor != 0)
                {
                    double avgEnemyDistance = enemies.Average(o =>
                    {
                        return Math.Sqrt(
                            Math.Pow(o.CharStats.Location.Data.Item1 - self.CharStats.Location.Data.Item1, 2) +
                            Math.Pow(o.CharStats.Location.Data.Item2 - self.CharStats.Location.Data.Item2, 2));
                    });

                    distScore += (distance < 1)
                        ? (float)(DistanceFactor * avgEnemyDistance)
                        : (float)(DistanceFactor * avgEnemyDistance / distance);
                }

                if (MovementFactor != 0)
                {
                    float avgMovement = enemies.Average(o => o.CombatStats.MovementRate.Data);
                    float movement = enemies[i].CombatStats.MovementRate.Data;

                    regScore += (avgMovement == 0)
                        ? MovementFactor * movement
                        : MovementFactor * movement / avgMovement;
                }

                if (ThreatOpportunityFactor != 0)
                {
                    float threatFactor = (theirResistance == 0)
                        ? avgDamage : (avgDamage / theirResistance);

                    float teamThreatFactor = (theirResistance == 0)
                        ? teamAvgDamage : teamAvgDamage / theirResistance;

                    regScore += (teamThreatFactor == 0)
                        ? ThreatOpportunityFactor * threatFactor
                        : ThreatOpportunityFactor * threatFactor / teamThreatFactor;
                }

                if (ResistOpportunityFactor != 0)
                {
                    float resistFactor = (resistance == 0)
                        ? YourResistFactor * (1 / theirAvgDamage)
                        : YourResistFactor * (1 / (theirAvgDamage / resistance));

                    float teamResist = allies.Average(o => o.CombatStats.DamageResistance.Data.Sum());
                    float teamResistFactor = (teamResist == 0)
                        ? 1 / theirAvgDamage
                        : 1 / (theirAvgDamage / teamResist);

                    regScore += (teamResistFactor == 0)
                        ? ResistOpportunityFactor * resistFactor
                        : ResistOpportunityFactor * resistFactor / TeamResistFactor;
                }

                // Computes all bonuses.
                if (NoRiskBonus != 0)
                {
                    float dist = (float)distance;
                    float movement = (self.CombatMovementBeforeEnabled.Data)
                        ? self.CombatStats.MovementRate.Data
                        : 0;

                    float range = self.CombatStats.MaxRangeAllowed.Data;
                    float goal = range - dist;
                    float intendedMove = Math.Min(Math.Abs(goal), movement);
                    dist += intendedMove * Math.Sign(goal);

                    if (dist <= range)
                    {
                        dist += movement;
                        if (enemies[i].CombatMovementBeforeEnabled.Data &&
                              enemies[i].CombatStats.MovementRate.Data +
                              enemies[i].CombatStats.MaxRangeAllowed.Data < dist)
                        {
                            distScore += NoRiskBonus;
                        }
                    }
                }

                if (InEasyRangeBonus != 0 && distance <= self.CombatStats.MaxRangeAllowed.Data)
                {
                    regScore += InEasyRangeBonus;
                }

                if ((IsVindictive || RetaliationBonus != 0) && combatHistory.Count > 1)
                {
                    Character selfLastRound = combatHistory[1]
                        .FirstOrDefault(o => o.Id == self.Id);
                    Character charLastRound = combatHistory[1]
                        .FirstOrDefault(o => o.Id == enemies[i].Id);

                    if (charLastRound.GetTargets().Contains(selfLastRound))
                    {
                        if (RetaliationBonus != 0)
                        {
                            regScore += RetaliationBonus;
                        }
                    }
                    else if (IsVindictive)
                    {
                        regScore -= float.MaxValue;
                    }
                }

                if ((IsSelfless || TeamworkBonus != 0) && combatHistory.Count > 1)
                {
                    List<Character> alliesLastRound = combatHistory[1]
                        .Where(o => o.TeamID == self.TeamID)
                        .ToList();

                    bool didTarget = enemies[i].GetTargets().Any(o =>
                        alliesLastRound.Contains(o));

                    if (didTarget)
                    {
                        if (TeamworkBonus != 0)
                        {
                            regScore += TeamworkBonus;
                        }
                    }
                    else if (IsSelfless)
                    {
                        regScore -= float.MaxValue;
                    }
                }

                if (EasyDefeatBonus != 0)
                {
                    if (avgDamage >= enemies[i].CharStats.Health.Data)
                    {
                        regScore += EasyDefeatBonus;
                    }
                }

                if (PreviousTargetBonus != 0 && Targets != null && Targets.Contains(enemies[i]))
                {
                    regScore += PreviousTargetBonus;
                }

                weights.Add(new WeightedTarget(enemies[i], regScore, distScore));
            }

            // Organizing weights and selecting targets.
            weights = weights.OrderBy(o => o.DistanceScore + o.NonDistanceScore).ToList();

            List<WeightedTarget> targets = new List<WeightedTarget>();
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i].DistanceScore + weights[i].NonDistanceScore
                    < MinScoreThreshold && MinScoreThreshold != 0)
                {
                    continue;
                }

                targets.Add(weights[i]);
            }

            WeightedTargets = targets;
            return targets.Select(o => o.Char).ToList();
        }

        /// <summary>
        /// Removes targets out-of-range, then adjusts scores based on new
        /// distances after moving, prioritizes high-scoring targets and trims
        /// to the number of targets allowed. Intended to be called after
        /// first movement. Returns characters.
        /// </summary>
        public List<Character> PostMovePerform(List<List<Character>> combatHistory, Character self)
        {
            if (OverrideTargets != null)
            {
                return OverrideTargets;
            }

            if (DistanceFactor == 0
                    && NoRiskBonus == 0
                    && self.CombatStats.MinRangeRequired.Data == 0
                    && self.CombatStats.MaxRangeAllowed.Data == -1)
            {
                WeightedTargets = WeightedTargets
                    .Take(MaxNumberTargets)
                    .ToList();

                return Targets;
            }

            for (int i = WeightedTargets.Count - 1; i >= 0; i--)
            {
                Character character = WeightedTargets[i].Char;
                float score = 0;

                CustomDistanceFactors?.ForEach(func => score += func(combatHistory, WeightedTargets[i].Char, self));

                double distance = Math.Sqrt(
                    Math.Pow(character.CharStats.Location.Data.Item1 - self.CharStats.Location.Data.Item1, 2) +
                    Math.Pow(character.CharStats.Location.Data.Item2 - self.CharStats.Location.Data.Item2, 2));

                // Can't target if out of range.
                if (distance < self.CombatStats.MinRangeRequired.Data ||
                    (self.CombatStats.MaxRangeAllowed.Data > 0 &&
                    distance > self.CombatStats.MaxRangeAllowed.Data))
                {
                    WeightedTargets.RemoveAt(i);
                    continue;
                }

                if (DistanceFactor != 0)
                {
                    double avgEnemyDistance = WeightedTargets.Average(o =>
                    {
                        return Math.Sqrt(
                            Math.Pow(o.Char.CharStats.Location.Data.Item1 - self.CharStats.Location.Data.Item1, 2) +
                            Math.Pow(o.Char.CharStats.Location.Data.Item2 - self.CharStats.Location.Data.Item2, 2));
                    });

                    score += (distance < 1)
                        ? (float)(DistanceFactor * avgEnemyDistance)
                        : (float)(DistanceFactor * avgEnemyDistance / distance);
                }

                // Computes all bonuses.
                if (NoRiskBonus != 0)
                {
                    float dist = (float)distance;
                    float movement = (self.CombatMovementBeforeEnabled.Data)
                        ? self.CombatStats.MovementRate.Data
                        : 0;

                    float range = self.CombatStats.MaxRangeAllowed.Data;
                    float goal = range - dist;
                    float intendedMove = Math.Min(Math.Abs(goal), movement);
                    dist += intendedMove * Math.Sign(goal);

                    if (dist <= range)
                    {
                        dist += movement;
                        if (character.CombatMovementBeforeEnabled.Data &&
                              character.CombatStats.MovementRate.Data +
                              character.CombatStats.MaxRangeAllowed.Data < dist)
                        {
                            score += NoRiskBonus;
                        }
                    }
                }

                WeightedTargets[i].DistanceScore = score;
            }

            WeightedTargets = WeightedTargets
                .OrderBy(o => o.NonDistanceScore + o.DistanceScore)
                .Take(MaxNumberTargets)
                .ToList();

            return Targets;
        }
        #endregion

        /// <summary>
        /// A character with associated score.
        /// </summary>
        private class WeightedTarget
        {
            /// <summary>
            /// The character associated with these scores.
            /// </summary>
            public Character Char;

            /// <summary>
            /// Score based on non-distance factors. Combine with distance
            /// score to get total score.
            /// </summary>
            public float NonDistanceScore;
            
            /// <summary>
            /// Score based on distance factors. Combine with non-distance
            /// score to get total score.
            /// </summary>
            public float DistanceScore;

            public WeightedTarget(Character character, float nonDistScore, float distScore)
            {
                Char = character;
                NonDistanceScore = nonDistScore;
                DistanceScore = distScore;
            }
        }
    }
}
