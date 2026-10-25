# Wireframe: Fast-Capture Bar & Command Palette (`Ctrl + K`)

## Purpose
The **Fast-Capture Bar & Command Palette** enables rapid capture in under 3 seconds. The interface uses a **clean, low-friction text entry model with smart defaults**, connecting directly to the **MVP LLM-assisted intake** ([docs/LLM_ASSISTED_INTAKE.md](../LLM_ASSISTED_INTAKE.md) / Issue #18) for natural English interpretation.

---

## Visual Wireframe: Universal Command Palette (`Ctrl + K` / `Cmd + K`)

```text
+----------------------------------------------------------------------------------------------------+
|                                                                                                    |
|   +--------------------------------------------------------------------------------------------+   |
|   |  Fast Capture & Command Palette                                             [ Esc to close]|   |
|   +--------------------------------------------------------------------------------------------+   |
|   |  > Math homework problem set due tomorrow at 5pm                                           |   |
|   +--------------------------------------------------------------------------------------------+   |
|   |  EXTRACTED DETAILS (Smart Defaults / Future LLM Candidate Preview):                        |   |
|   |  [ Title: Math homework problem set ]  [ 60m (Default) ]  [ Due: Tomorrow 5pm ]            |   |
|   |  [ List: School ]                      [ Importance: Normal ]                              |   |
|   +--------------------------------------------------------------------------------------------+   |
|   |  QUICK ACTIONS / SUGGESTIONS:                                                              |   |
|   |  Enter       Add and Auto-Schedule Task                                                    |   |
|   |  Shift+Enter Add as Fixed Event / Meeting                                                  |   |
|   |  Alt+Enter   Open in Right Inspector for Advanced Settings                                 |   |
|   +--------------------------------------------------------------------------------------------+   |
|   |  RECENT COMMANDS:                                                                          |   |
|   |  * Switch view to: Today Execution View                                                    |   |
|   |  * Switch view to: Week Horizon Grid                                                       |   |
|   |  * Rebalance Schedule                                                                      |   |
|   +--------------------------------------------------------------------------------------------+   |
|                                                                                                    |
+----------------------------------------------------------------------------------------------------+
```

---

## Visual Wireframe: Inline Center Stage Quick-Add Field

```text
+----------------------------------------------------------------------------------------------------+
|  [ + Add task (e.g. "Math homework due tomorrow 5pm"). Press Enter to schedule                   ] |
+----------------------------------------------------------------------------------------------------+
```

---

## Natural Intake & Evolutionary Roadmap

### Phase 1 (UI Overhaul Base):
- **Simple Natural Title + Smart Defaults**:
  - Typing a title and pressing `Enter` creates the task immediately.
  - Applies smart defaults: default duration (`60m` or list default), next working day end for due date, normal importance (`5`), and the active list.
- **One-Tap Micro Chips**:
  - The preview chips below the field (`[ 60m ]`, `[ Due: Tomorrow ]`) are clickable. Tapping any chip opens a 1-tap popover (such as `30m | 1h | 2h` or a calendar picker) within the prompt flow.

### Phase 2 (MVP LLM-Assisted Intake Integration - Issue #18):
- Connects the text input to the server-side LLM intake pipeline ([docs/LLM_ASSISTED_INTAKE.md](../LLM_ASSISTED_INTAKE.md)).
- Accepts natural English phrases:
  - *"Biology lab reading for Thursday afternoon, about 45 minutes"*
  - *"Prep presentation slides due next Monday 9am"*
- The LLM extracts the candidate title, estimated duration, and due date. The preview chips display the extracted candidate fields with confidence indicators for user confirmation before saving to Core.
- The preview chip container built in Phase 1 serves directly as the candidate review interface for Phase 2.
