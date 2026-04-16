# Core Loop Prototype - Setup

## Question
Does the card durability + devour transformation mechanic create meaningful strategic decisions?

## Setup in Unity (2 minutes)

1. Open the project in Unity
2. Create a new scene: `Assets/Scenes/PrototypeCoreLoop.unity`
   - Right-click in Hierarchy -> UI -> Canvas (this creates Canvas + EventSystem)
   - Delete the Canvas (keep the EventSystem)
   - Create empty GameObject, name it `CoreLoopPrototype`
   - Drag `Assets/Prototypes/CoreLoop/PrototypeCoreLoop.cs` onto it
3. Open the scene and hit Play

## What You'll See

- **Top bar**: Day, Round, Revenue, Target
- **4 seat panels**: Show diners with Gold, BaseFee, Spent, Bill
- **Hand area**: Clickable cards with name, cost, durability
- **Log panel**: Settlement results, devour events
- **End Round button**: Triggers settlement

## How to Play

1. Diners auto-assign to empty seats each round
2. **Click a card** in your hand (highlights in cyan)
3. **Click a diner** to play the card as add-on dish
4. Watch the diner's Bill update (turns red when > Gold = devour!)
5. Click **End Round** to settle all diners
6. Reach daily target to advance to next day
7. Miss target after 4 rounds = Game Over

## Key Mechanics to Evaluate

- **Durability tension**: Do you save high-value cards for later or use them now?
- **Devour decision**: Is it worth pushing a diner's bill over their gold to get a powerful new card?
- **Resource depletion**: As cards exhaust, do you feel pressured? Does it create interesting trade-offs?

## Files

| File | Purpose |
|------|---------|
| `Assets/Prototypes/CoreLoop/PrototypeCoreLoop.cs` | Single MonoBehaviour - all logic + UI |
| `prototypes/core-loop/SETUP.md` | This file |
