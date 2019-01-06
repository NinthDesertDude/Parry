using System;
using System.Collections.Generic;

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
        public static readonly TargetBehavior Normal;

        /// <summary>
        /// Prioritizes strong and nearby enemies.
        /// </summary>
        public static readonly TargetBehavior Champion;

        /// <summary>
        /// Prioritizes strong enemies that the team can take down, and
        /// enemies already targeted by teammates.
        /// </summary>
        public static readonly TargetBehavior Hivemind;

        /// <summary>
        /// Prioritizes attackers first, followed by the nearest target.
        /// </summary>
        public static readonly TargetBehavior Aggressive;

        /// <summary>
        /// Stands idle until provoked, then prioritizes attackers.
        /// </summary>
        public static readonly TargetBehavior Sentry;

        /// <summary>
        /// Prioritizes weak nearby enemies with their backs turned.
        /// </summary>
        public static readonly TargetBehavior Assassin;

        /// <summary>
        /// Prioritizes targets closest to the archer or most likely to reach
        /// the archer first based on speed.
        /// </summary>
        public static readonly TargetBehavior Archer;
        #endregion

        #region Variables - Factors and Bonuses
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
        /// Average ally damage ÷ their health, clamped to 1.
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
        /// Number of combatants that targeted the target in consideration.
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
        /// Adds X for targets in attack range after moving.
        /// </summary>
        public float InMobileRangeBonus
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
        /// Adds X for targets facing a direction away from you (by 90 degrees
        /// or more). Based on direction to targets, or movement direction.
        /// </summary>
        public float TurnedAwayBonus
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
        public bool IsBetrayer
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
        /// The AI considers targets in range until there are none.
        /// False by default.
        /// </summary>
        public bool InRangeOnly
        {
            get;
            set;
        }

        /// <summary>
        /// The AI does not consider targets until targeted or damaged once.
        /// False by default.
        /// </summary>
        public bool IsDormant
        {
            get;
            set;
        }

        /// <summary>
        /// The weighted scores involved in selecting targets must be >= this
        /// value to cause the AI to switch targets. Default 0. Negative or
        /// sufficiently high values cause the AI to never switch valid
        /// targets.
        /// </summary>
        public int ScoreDifferenceToChangeTargets
        {
            get;
            set;
        }
        #endregion

        #region Variables
        /// <summary>
        /// The AI will take the first N targets based on their criteria.
        /// If negative, the AI will take more than 1 only for area targeting.
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
        /// targeting is limited to the radius and an attack is made for each
        /// entry. 
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
        public List<Combatant> Targets
        {
            get;
            set;
        }

        /// <summary>
        /// When set, the combat system will copy this list to the Targets list
        /// rather than recomputing the targets.
        /// </summary>
        public List<Combatant> OverrideTargets
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
        /// Sets up all statically-available default behaviors.
        /// </summary>
        static TargetBehavior()
        {
            Aggressive = new TargetBehavior()
            {
                IsVindictive = true,
                DistanceFactor = 1
            };

            Champion = new TargetBehavior()
            {
                ThreatOpportunityFactor = 3,
                ResistOpportunityFactor = 3,
                YourThreatFactor = -1,
                YourResistFactor = -1,
                YourHealthDamageFactor = -1,
                GroupAttackFactor = -1,
                RetaliationBonus = 10,
                InEasyRangeBonus = 20,
                InMobileRangeBonus = 10,
                ScoreDifferenceToChangeTargets = 5
            };

            Hivemind = new TargetBehavior()
            {
                TeamThreatFactor = 5,
                TeamHealthDamageFactor = 5,
                TeamResistFactor = 5,
                GroupAttackFactor = 2,
                TeamworkBonus = 2,
                InEasyRangeBonus = 5,
                InMobileRangeBonus = 5,
                ScoreDifferenceToChangeTargets = 5
            };

            Normal = new TargetBehavior()
            {
                YourThreatFactor = 2,
                YourResistFactor = 1,
                YourHealthDamageFactor = 10,
                GroupAttackFactor = 1,
                RetaliationBonus = 20,
                InEasyRangeBonus = 20,
                InMobileRangeBonus = 10,
                EasyDefeatBonus = 20,
                NoRiskBonus = 10,
                ScoreDifferenceToChangeTargets = 10
            };

            Assassin = new TargetBehavior()
            {
                InEasyRangeBonus = 10,
                InMobileRangeBonus = 10,
                TurnedAwayBonus = 20,
                NoRiskBonus = 10,
                EasyDefeatBonus = 10,
                YourThreatFactor = 1,
                ScoreDifferenceToChangeTargets = 10
            };

            Sentry = new TargetBehavior()
            {
                IsDormant = true,
                IsVindictive = true,
                ScoreDifferenceToChangeTargets = 1
            };

            Archer = new TargetBehavior()
            {
                InMobileRangeBonus = 500,
                DistanceFactor = -1,
                EasyDefeatBonus = 10,
                YourThreatFactor = 2,
                YourResistFactor = 1,
                YourHealthDamageFactor = 10,
                ScoreDifferenceToChangeTargets = 50
            };
        }

        /// <summary>
        /// Sets defaults for tendencies and all factors and bonuses to 0.
        /// </summary>
        public TargetBehavior()
        {
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
            TurnedAwayBonus = 0;
            NoRiskBonus = 0;
            InEasyRangeBonus = 0;
            InMobileRangeBonus = 0;
            RetaliationBonus = 0;
            TeamworkBonus = 0;
            EasyDefeatBonus = 0;
            ScoreDifferenceToChangeTargets = 0;
            IsBetrayer = false;
            IsLoyal = false;
            IsNeutral = false;
            IsVindictive = false;
            IsSelfless = false;
            InRangeOnly = false;
            IsDormant = false;
            MaxNumberTargets = 1;
            AreaTargetPoints = new List<Tuple<int, int, int>>();
            Targets = new List<Combatant>();
            OverrideTargets = new List<Combatant>();
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
            TurnedAwayBonus = other.TurnedAwayBonus;
            NoRiskBonus = other.NoRiskBonus;
            InEasyRangeBonus = other.InEasyRangeBonus;
            InMobileRangeBonus = other.InMobileRangeBonus;
            RetaliationBonus = other.RetaliationBonus;
            TeamworkBonus = other.TeamworkBonus;
            EasyDefeatBonus = other.EasyDefeatBonus;
            ScoreDifferenceToChangeTargets = other.ScoreDifferenceToChangeTargets;
            IsBetrayer = other.IsBetrayer;
            IsLoyal = other.IsLoyal;
            IsNeutral = other.IsNeutral;
            IsVindictive = other.IsVindictive;
            IsSelfless = other.IsSelfless;
            InRangeOnly = other.InRangeOnly;
            IsDormant = other.IsDormant;
            MaxNumberTargets = other.MaxNumberTargets;
            AreaTargetPoints = other.AreaTargetPoints;
            Targets = other.Targets;
            OverrideTargets = other.OverrideTargets;
            AllowSelfTargeting = other.AllowSelfTargeting;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns targets automatically based on the targeting behavior
        /// and a list of combatants. Set OverrideTargets to override this
        /// behavior.
        /// </summary>
        public List<Combatant> Perform(List<Combatant> combatants)
        {
            //TODO: Implement this.
            throw new NotImplementedException();
        }
        #endregion
    }
}
