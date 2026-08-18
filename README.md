# Animal Stack

**Animal Stack** is a physics-based mobile AR game with one simple goal:  
stack as many animals as possible on the starting platform without letting any of them fall off the edge.

A playful safari/zoo theme meets skill-based balance gameplay where every drop matters.

---

## Core Game Idea

- **Name:** Animal Stack  
- **Genre:** Physics-based AR stacker / skill puzzle  
- **Goal:** Stack as many animals as possible on the starting platform  
- **Lose Condition:** The game ends immediately when one or more animals fall outside the platform  
- **Scoring:** Score = number of successfully stacked animals

### What makes it interesting?

- Every animal has a different shape and center of mass  
  (for example, a tall giraffe vs. a low/round turtle)
- The player chooses **both position and rotation** before dropping
- Physics decides the result: good judgment is rewarded, bad judgment collapses the stack

---

## Gameplay Loop

1. The player detects a surface (table/floor)  
2. The starting platform is placed in AR  
3. The next animal appears as a preview above the stack  
4. The player drags the animal to the desired position  
5. The player rotates the animal to the desired angle  
6. The animal is dropped  
7. Physics resolves the balance  
8. If everything stays on the platform → **+1 point** and a new animal  
9. If an animal falls outside the platform → **Game Over**

---

## AR and Mobile Experience

### AR approach
- **AR Type:** Surface detection  
- **Positioning:** No GPS required  
- **Why AR matters:** The player can move around the table and evaluate the stack from different angles before dropping

### Controls
- **Drag:** move the animal above the stack  
- **Two-finger rotate:** rotate the animal  
- **Release / tap:** drop the animal  
- Optional rotation buttons are available for one-handed play

### UI ideas
- Large score counter at the top  
- Small “next animal” preview  
- Short tutorial hint in the first run:
  - *“Drag to place, rotate with two fingers, release to drop.”*

---

## ✅ Features

### Required (MVP)
- [x] Surface detection + platform placement
- [x] Animal drag and rotation controls
- [x] Physics-based stacking
- [x] Lose-condition detection (falling outside platform)
- [x] Score counter

### Extra features (future)
- [ ] Multiple animal models with different shapes/sizes
- [ ] Sound effects
- [ ] Swaying and reactive animations
- [ ] Increasing difficulty / timer
- [ ] Leaderboard
- [ ] Bonus points for especially stable stacking

---

## Safety and Testability

- Played while stationary, ideally seated at a table  
- Works in roughly **50 × 50 cm** of table space  
- Easy to test in classrooms or at home  
- No need to walk while staring at the screen → safer AR experience

---

## Technology

- **Engine:** Unity  
- **Unity Version:** `6000.5.8f1`  
- **AR:** AR Foundation + ARCore + ARKit  
- **Rendering:** URP

---

## Core Vision

**Easy to start, hard to master.**  
One animal at a time, one mistake at a time — how high can your stack survive?
