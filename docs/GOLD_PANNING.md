# RFC: "Gold Panning" Scheduling Algorithm

## 1. Core Concept (The Sluice Box)
We treat the schedule as a river of time flowing through a **Sluice Box** (The Scheduling Engine).
We dump all the material (Tasks) into the flow.
*   **Gold Nuggets (Urgent/Important Tasks)**: These are heavy. They drop out of the flow and lodge into the "Riffles" (Time Slots) immediately near the top. They are hard to displace.
*   **Gravel (Standard Tasks)**: These are lighter. If the top riffles are full of Gold, the gravel washes over them and settles in the middle riffles.
*   **Sand/Silt (Backlog/Low Priority)**: These are very light. They flow over everything else and settle in the calm pools at the very end of the box (Future Dates).

## 2. The Physics of the Flow (The Equation)
For a specific `Task (T)` being considered for a specific `Day (D)`, the Resistance to Movement (Weight) is:

$$ Weight(T) = UrgencyMass(T) + ImportanceMass(T) + AgingMass(T) $$

### A. Gold (Urgency) - "Density"
Tasks with near deadlines are ultra-dense.
*   **Formula**: `1 / (DaysUntilDeadline + 1)^2`
*   **Effect**: If the deadline is Today, Mass is HUGE. It sinks instantly into Day 1.
*   If the deadline is 30 days out, Mass is low. It floats easily.

### B. Size (Importance) - "The Catch"
Large/Important items are more likely to get caught in the riffles.
*   High Importance Task = Large Rock. Even if not urgent, it's heavy enough to settle early if there's room.

### C. Flow (Congestion) - "The Water Pressure"
*   **The Stream Power**: Purely the **Time Capacity** of the day.
*   **Selection**: The Pressure pushes the "Lightest" items (Lowest Importance/Urgency) downstream first, preserving the "Gold" (High Priority) in the current day.
*   **Constructive Fill**: Unlike a real river, we don't just let things wash away randomly. We actively pack the day. If a day has a 30-minute gap, and the next "Gold Nugget" is 2 hours long, we **Hammer** (Split) that nugget. We put 30 minutes of it in the gap, and the remaining 1.5 hours washes to the next day.

## 3. The Execution Flow (The Masonry)

### Step 1: The Sort (The Sift)
We sort all active tasks by their "Weight" (Urgency + Importance). The heaviest items are at the top of the stack.

### Step 2: The Fill (Bin Packing)
We treat each Day as a Bucket with fixed capacity (e.g., 8 hours).
*   **Iterate**: We take the top task from the sorted stack.
*   **Fit Check**: Does it fit in the current day's remaining space?
    *   **Yes**: Place it in.
    *   **No**:
        *   **Gap Analysis**: Is the remaining space usable (e.g., > 15 mins)?
        *   **The Hammer**: If yes, we **Split** the task. Part A fills the gap perfectly. Part B returns to the top of the stack for the next day.
        *   **Skip**: If the gap is too small, we might skip it or look for a tiny "pebble" (short task) later in the list to fill it.

### Step 3: The Mosaic (Energy Management)
*   **Goal**: Optimize for Cognitive Load *within* the day.
*   Once the "Bucket" for Day 1 is full, we arrange the specific start times.
*   **Strategy**: "Front-Loading".
    *   Human energy/focus is typically highest at the start of the day.
    *   We Sort the daily list by `Complexity DESC`.
    *   **Schedule**: `High Complexity (Morning) -> Medium Complexity (Mid-Day) -> Low Complexity (Afternoon)`.
    *   *Result*: "Eat the frog" first, then settle into easier administrative work as fatigue sets in.

## 4. Why this works for your Questions
*   **"Recalculate?"**: Yes. Every time you run the scheduler, we re-weigh the gold and re-pack the buckets.
*   **"What dictates the day?"**: The specific gravity of the task vs. the capacity of the current bucket.
*   **"Splitting?"**: We prefer filling a day 100% over keeping a task whole. This ensures maximum throughput (no "Swiss Cheese" schedules).
*   **"No Due Date"**: This is fine Silt. It is light, so it naturally ends up in the last buckets (future dates) after all the heavy Gold is packed.

## 5. Strategy Characteristics & Behavior

The Gold Panning strategy prioritizes **Throughput** (filling the day) and **Energy Management** (ordering the day).

### A. "Eat the Frog" overrides Importance (Day Sequencing)
*   **Behavior**: Within a single day, we use **Deadline Anchoring**.
    *   **Tie-Breaker 1: Deadline Safety**: Tasks due *Today* (or Overdue) are pinned to the start of the day. This prevents last-minute stress.
    *   **Tie-Breaker 2: Energy Management**: For all other tasks (flexible for the day), we sort by `Complexity DESC`.
*   **Rationale**: High-complexity work requires peak mental energy (morning), but *Deadlines* require safety. We clear the MUST-DOs first, then optimize the rest of the day.
*   **Consequence**: A low-complexity task due today will be scheduled *before* a high-complexity task due next week. This is an intentional deviation from pure "Eat the Frog" to ensure reliability.

### B. The "Hammer" (Splitting)
*   **Behavior**: We ruthlessly split tasks to eliminate wasted time gaps.
*   **Rationale**: Time is a non-renewable resource. Leaving 45 minutes empty because "the next task takes an hour" is waste.
*   **Consequence**: Users may see a large task broken across 2 or 3 days.
*   **Mitigation**: The UI explicitly links these chunks (e.g., "Part 1", "Part 2") so the user understands strictly that it is one task.

### C. Urgency Bias (Prioritization)
*   **Behavior**: The scoring formula `(Urgency + Importance) * Density` heavily favors imminent deadlines.
*   **Rationale**: Clearing immediate blockers prevents debt accumulation.
*   **Consequence**: Long-term strategic work (High Importance, Low Urgency) risks being perpetually washed downstream by a stream of incoming fire-fighting tasks.
*   **Mitigation**: The system relies on "Aging" (tasks getting more urgent as they approach deadline) to eventually force them through.

## 5. Summary
*   **Gold Panning Algorithm**:
    *   **Gravity**: Pulls everything to Day 1.
    *   **Water Pressure**: Pushes excess volume to Day N+1.
    *   **Weight**: Urgency + Importance prevents washing away.
