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
*   The **Stream Power** is purely the **Time Capacity** of the day.
*   If Day 1 is 8 hours long and we dump 12 hours of tasks into it, the "Water Pressure" becomes irresistible.
*   **Result**: The Pressure *must* displace 4 hours of material to Day 2.
*   **Selection**: The Pressure pushes the "Lightest" items (Lowest Importance/Urgency) downstream first, preserving the "Gold" (High Priority) in the current day.
*   *Note*: The schedule is natively **front-loaded**. We fill Day 1 completely before spilling to Day 2. Free time only exists at the end of the schedule (i.e., after all active tasks are complete).

## 3. The Execution Flow (The Pour and Sift)

### Step 1: The Dump (Initial Layout)
We dump all tasks into their "Ideal Spot" (Usually Day 1 or their Start Date).

### Step 2: The Sift (Time Displacement)
We "shake" the box (Iterate through Overloaded Days).
*   **Condition**: Is `TotalHours > Capacity`?
*   **Action**: Identify the lightest tasks (Lowest Weight).
*   **Result**: Wash them downstream to availability.

### Step 3: The Mosaic (Energy Management)
*   **Goal**: Optimize for Cognitive Load *within* the day.
*   Once the "Gold Pan" determines *which* tasks are on Day 1, we arrange their specific start times.
*   **Strategy**: "Front-Loading".
    *   Human energy/focus is typically highest at the start of the day.
    *   We Sort the daily list by `Complexity DESC`.
    *   **Schedule**: `High Complexity (Morning) -> Medium Complexity (Mid-Day) -> Low Complexity (Afternoon)`.
    *   *Result*: "Eat the frog" first, then settle into easier administrative work as fatigue sets in.

## 4. Why this works for your Questions
*   **"Recalculate?"**: Yes, the "Sand" washes from riffle to riffle until it finds a calm spot.
*   **"What dictates the day?"**: The specific gravity of the task vs. the pressure of the day.
*   **"Important vs Low Priority"**: Important = Heavier. It settles earlier than light tasks.
*   **"No Due Date"**: This is fine Silt. It will wash all the way to the end of the sluice box unless there is a calm spot (empty day) early on where it can settle.
    *   **Anti-Starvation (Relative Density)**:
        *   Assign "No Due Date" tasks a **Static Weight** (Density) that is slightly lighter than a Standard Tasks.
        *   *Example*: Standard Task Weight = 1.0. Backlog Task Weight = 0.8.
        *   *Result on a Busy Day*: The Stream Pressure is high. The 1.0 stones hold their ground. The 0.8 silt washes away.
        *   *Result on a Calm Day*: The Stream Pressure is low. The 0.8 silt is heavy enough to settle and stick.
        *   *User Control*: Users can boost this via the `Importance` attribute (e.g., turning Silt into a Pebble with Weight 1.2), allowing specific backlog items to fight for a slot.

## 5. Summary
*   **Gold Panning Algorithm**:
    *   **Gravity**: Pulls everything to Day 1.
    *   **Water Pressure**: Pushes excess volume to Day N+1.
    *   **Weight**: Urgency + Importance prevents washing away.
