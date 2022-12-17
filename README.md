# Parry

Parry is a turn-based combat framework for text-based games and 2D graphical games. You can also use it in an event-driven way, although it has limited handling for things like pathfinding in realtime games.

Features are largely:
- fine control over turn progression
- flexible movement and targeting system
- many built-in common mechanics such as knockback damage
- customization of any of the above through event hooks
- a weight-based system of factors for determining targets, for smarter enemy AI

Important lesser features to point out:
- support for any number of teams and the ability to change teams / change combatants / end combat at any time

### Session
Start by creating character objects, adding them to teams as desired, add them to a Session instance, then call StartSession(). Use the Execute* Has* and HasNext* functions of the session instance to control the progression of turns and rounds.

Here are the special variables you can set on a session:
- Speed carries over: when true, all character speed is cumulative across rounds (doesn't reset to zero)  
- Extra turns enabled: When true, excessive character speed leads to additional turns during a round, similar to Pokemon games  
- Simultaneous turns enabled: When true, characters with the exact same speed will both go at the same time to enable scenarios like both dealing lethal damage to each other  
- Round history limit: How many previous rounds of combat are stored, available to moves so they can take previous combat into account  

### Turn structure
Turn: all the steps a single character takes in a round.

**Turn begins**  
- Motive and move are selected. Moves are written by you and grouped by motive to easier control which moves to allow
- Targets are selected. Multiple targets can be selected at once. They don't have to be in range
- Pre-move movement occurs. The character moves their location on a 2D grid if allowed
- Move executes. The character attempts to execute their move if possible
- Post-move movement occurs. The characters moves their location again after having performed their move, if allowed
- Turn ends

### Moves and multiple moves
Move: an action a character can take during their turn. Moves are not necessarily just attacking; they can be arbitrary callbacks.

Moves can take up part of a turn, in other words, when a character executes moves, they can execute any number of moves that add up to 1.0 (representing 100% of the time available during their turn). Moves have the following related logic:
- turn fraction: how much of a turn a move takes. Normally, moves take a full turn (1.0), but multiple moves may be possible if they take less
- charging: moves can be charged to reduce the turn fraction taken. Performing a charged move reduces the charge
- move speed delay: if a character takes multiple moves in a turn, they're performed in order from smallest to greatest delay
- cooldown: how many turns must pass until the character can use a move again
- uses per turn: how many times the same move can be used in one turn, regardless of its turn fraction
- uses remaining turn: using this move prevents any other moves by the character this turn

### Move targets
A big part of moves is picking the targets. Since moves can be arbitrary actions, targets are not just limited to characters on opposing teams, or characters in range. Anybody can be targeted including the character executing the move (for e.g. healing moves).

Here are the targeting options:
- max number targets: limits the number of targets. Most moves in a typical game target only 1
- Area target points: ignores potential targets outside the radii of these circle(s)
- Allow self targeting: self explanatory. This is useful for e.g. moves that help yourself
- Override targets: when provided, skips targeting for the move and just uses this list of characters as targets

Targeting is an automatic decision (unless overridden) based on a large list of possible factors. Factors are only considered if given a weight > 0, and factors are considered for each potential target. The targets, once weighted, are sorted from highest to lowest weight and the first X targets allowed (or all of them, if there is no max number of targets) become targets for the move.

Here are the factors (X is whatever you assign as the weight of the factor):
- Random: a random number from 0 to 1, great to add to help targets change
- Your threat factor: Your damage / their resistances
- Team threat factor: Average ally damage ÷ their resistances
- Your resist factor: 1 ÷ (their damage ÷ your resistances)
- Team resist factor: 1 ÷ (their damage ÷ average ally resistances)
- Threat opportunity factor: yourThreatFactor ÷ teamThreatFactor
- Resist opportunity factor: yourResistFactor ÷ teamResistFactor
- Your health damage factor: Your damage ÷ their health
- Team health damage factor: Average ally damage ÷ target's health, clamped to 1
- Distance factor: Average enemy distance from you ÷ their >= 1 distance from you
- Movement factor: Their movement rate ÷ average enemy movement rate
- Group attack factor: Number of characters that targeted the target in consideration
- In easy range bonus: Adds X for targets in attack range before moving
- Relatiation bonus: Adds X for targets that targeted you
- Teamwork bonus: Adds X for targets that targeted allies
- Easy defeat bonus: Adds X for targets that can be dispatched in one hit by you
- No risk bonus: Adds X for targets in attack range that can't attack you after moving
- Previous target bonus: Adds X for targets that were targeted last round

Here are special targeting effects:
- Swap Allies Enemies: The AI considers allies as enemies and vice versa everywhere
- Is Loyal: The AI will not retaliate against allies that attack them
- Is Neutral: Adds allies to the list of possible targets
- Is Vindictive: The AI will first consider those that attacked them (no effect with selfless)
- Is Selfless: The AI will first consider those that attacked allies (no effect with vindictive)
- Min Score Threshold: If nonzero, the weighted scores involved in selecting targets must be >= this value to select a target

## Movement behavior
Actually moving the character may occur in a few different ways, because the character can only be in one place before and after their move, but they can have multiple targets. So this covers how they move with respect to their targets, and is part of the MovementBehavior class accessible from TargetBehavior.

The destination to move to may be, if not overridden:
- Average: the averaged location of all targets
- First: location of the first target
- Furthest: location of the furthest target
- Nearest: location of the nearest target, regardless of whether they're first or not
- NearestToCenter: location of the target nearest to the averaged location of all targets, regardless if they're first or not

The actual movement itself doesn't have to be to walk towards the destination. It can be:
- Away: Move in the exact opposite direction
- AwayUpToDistance: Move away up to a distance (and don't move forward if past that distance)
- ToDistance: Move towards or away from the target as needed until at a specified distance
- Towards: Move to the destination (default)
- TowardsUpToDistance: Move to the destination, but stop a distance away (and don't move backwards if closer than that distance)
- WithinDistanceRange: Move until between a minimum and maximum distance of the point

## Damage Stats  
Lastly, here's a list of the damage-related stats available by default for combat-oriented moves:  
- character health, max health, speed delay and accumulated speed over rounds (if in use)
- minimum and maximum damage
- damage reduction (a constant value) and resistance (a percentage)
- chance to hit and chance to dodge
- critical hit chance, critical hit damage multiplier, and critical hit immunity status
- minimum and max range requirements to hit, and damage multipliers for how close/far within range
- knockback (damage dealt back, both constant or percentage)
- recoil (physical location pushback on successful strike)
- special statuses like immunity to critical hits, always hitting, living even at < 0 health, always going first/last, etc.
