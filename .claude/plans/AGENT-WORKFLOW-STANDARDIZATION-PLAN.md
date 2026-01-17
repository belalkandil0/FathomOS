# AGENT WORKFLOW STANDARDIZATION PLAN

**Status:** PENDING USER APPROVAL
**Created:** 2026-01-16
**Author:** R&D-AGENT
**Requested By:** ORCHESTRATOR-AGENT
**Priority:** High

---

## 1. EXECUTIVE SUMMARY

This plan addresses inconsistent agent workflow execution where agents sometimes perform work directly instead of delegating to agents within their scope. The goal is to standardize all 26 agent `.md` files with clear scope boundaries, delegation rules, and hierarchy enforcement.

### Current State Analysis

After auditing all agent files, I found:

**Well-Structured Agents (Good Templates):**
- ORCHESTRATOR-AGENT.md - Has CRITICAL RULES section, clear hierarchy
- ARCHITECTURE-AGENT.md - Has CRITICAL RULES section, delegation examples
- UI-AGENT.md - Clear scope, good coordination section
- CORE-AGENT.md - Clear boundaries and restrictions

**Needs Improvement:**
- R&D-AGENT.md (RESEARCH-DEVELOPMENT-AGENT.md) - Missing CRITICAL RULES section at top
- SHELL-AGENT.md - Missing CRITICAL RULES section
- All MODULE-*.md files - Missing CRITICAL RULES, inconsistent format
- Support agents - Missing CRITICAL RULES section

**Missing Agents:**
- None - All 26 agents have files

---

## 2. STANDARDIZED AGENT TEMPLATE

Every agent `.md` file MUST follow this structure:

```markdown
# {AGENT-NAME}

## Identity
[1-2 sentences describing who this agent is and primary role]

---

## CRITICAL RULES - READ FIRST

### NEVER DO THESE:
1. **NEVER write code yourself** - Always delegate to appropriate agents
2. **NEVER modify files outside your scope** - [List specific boundaries]
3. **NEVER bypass the hierarchy** - All requests go through proper channels
4. **NEVER [agent-specific prohibition]**

### ALWAYS DO THESE:
1. **ALWAYS read your agent file first** when spawned
2. **ALWAYS delegate to agents within your scope** - [List who you manage]
3. **ALWAYS report back to your supervisor** - [Name supervisor]
4. **ALWAYS verify work stays within agent boundaries**

### COMMON MISTAKES TO AVOID:
```
 WRONG: [Example of incorrect behavior]
 RIGHT: [Example of correct behavior]

 WRONG: [Example 2]
 RIGHT: [Example 2]
```

---

## HIERARCHY POSITION

```
[ASCII diagram showing position in hierarchy]
```

**You report to:** {SUPERVISOR-AGENT}
**You manage:** {LIST OF SUBORDINATE AGENTS or "None - you are an implementer"}

---

## FILES UNDER YOUR RESPONSIBILITY

```
{Project}/
+-- folder/
|   +-- file.cs
|   +-- ...
```

**Allowed to Modify:**
- [Specific files/folders]

**NOT Allowed to Modify:**
- [Specific files/folders - delegate to X-AGENT]

---

## RESPONSIBILITIES

### What You ARE Responsible For:
1. [Responsibility 1]
2. [Responsibility 2]
...

### What You MUST Do:
- [Requirement 1]
- [Requirement 2]
...

---

## RESTRICTIONS

### What You are NOT Allowed To Do:

#### Code Boundaries
- **DO NOT** [boundary 1]
- **DO NOT** [boundary 2]

#### Architecture Violations
- **DO NOT** [violation 1]
- **DO NOT** [violation 2]

#### [Category-Specific Violations]
- **DO NOT** [violation]

---

## COORDINATION

### Report To:
- **{SUPERVISOR-AGENT}** for [what]

### Coordinate With:
- **{PEER-AGENT}** for [what]

### Manage (if applicable):
- **{SUBORDINATE-AGENT}** - [their responsibility]

### Request Approval From:
- **{AGENT}** before [action]

---

## DELEGATION PROTOCOL (for coordinating agents only)

### How to Properly Delegate:
[Include Task spawn examples if this agent manages others]

---

## IMPLEMENTATION STANDARDS
[Code patterns and examples relevant to this agent]

---

## VERSION
- Created: [date]
- Updated: [date]
- Version: [number]
```

---

## 3. AGENT HIERARCHY DIAGRAM

```
                                    USER
                        (Product Owner / Architect)
                                     |
                                     | Approvals & Decisions
                                     v
    +================================================================+
    |                      TIER 0: USER INTERFACE                     |
    |                                                                 |
    |                      ORCHESTRATOR-AGENT                         |
    |              (User Interface / Progress Tracking)               |
    |                                                                 |
    |    NOT an implementer - routes requests & reports progress      |
    +================================================================+
                                     |
              +----------------------+----------------------+
              |                                             |
              v                                             v
    +=====================+                   +==========================+
    |      TIER 1:        |                   |         TIER 1:          |
    |   PLANNING          |                   |     IMPLEMENTATION       |
    |                     |                   |                          |
    |  R&D-AGENT          |                   |  ARCHITECTURE-AGENT      |
    |  (Research &        |                   |  (Implementation         |
    |   Planning)         |                   |   Coordinator)           |
    |                     |                   |                          |
    | NOT an implementer  |                   |  NOT an implementer      |
    | Creates PLANS only  |                   |  DELEGATES to agents     |
    +=====================+                   +==========================+
                                                          |
              +-------------------------------------------+
              |                    |                      |
              v                    v                      v
    +==================+  +==================+  +==================+
    |    TIER 2:       |  |    TIER 2:       |  |    TIER 2:       |
    |  INFRASTRUCTURE  |  |    SUPPORT       |  |    MODULES       |
    |                  |  |                  |  |                  |
    | SHELL-AGENT      |  | DATABASE-AGENT   |  | MODULE-Survey    |
    | CORE-AGENT       |  | TEST-AGENT       |  |   Listing        |
    | UI-AGENT         |  | BUILD-AGENT      |  | MODULE-Survey    |
    | CERTIFICATION    |  | SECURITY-AGENT   |  |   Logbook        |
    |   -AGENT         |  | DOCUMENTATION    |  | MODULE-Network   |
    | LICENSING-AGENT  |  |   -AGENT         |  |   TimeSync       |
    |                  |  |                  |  | MODULE-Equipment |
    | IMPLEMENTERS     |  | IMPLEMENTERS     |  |   Inventory      |
    | (write code)     |  | (write code/     |  | MODULE-Sound     |
    |                  |  |  tests/docs)     |  |   Velocity       |
    +==================+  +==================+  | MODULE-Gnss      |
                                               |   Calibration    |
                                               | MODULE-Mru       |
                                               |   Calibration    |
                                               | MODULE-Usbl      |
                                               |   Verification   |
                                               | MODULE-Tree      |
                                               |   Inclination    |
                                               | MODULE-RovGyro   |
                                               |   Calibration    |
                                               | MODULE-Vessel    |
                                               |   GyroCalibration|
                                               | MODULE-Personnel |
                                               |   Management     |
                                               | MODULE-Project   |
                                               |   Management     |
                                               |                  |
                                               | IMPLEMENTERS     |
                                               | (write code)     |
                                               +==================+
```

---

## 4. WORKFLOW RULES

### Rule 1: Only Tier 2 Agents Write Code

```
TIER 0 (ORCHESTRATOR):     Routes requests, tracks progress      NO CODE
TIER 1 (R&D):              Creates plans, researches             NO CODE
TIER 1 (ARCHITECTURE):     Coordinates, delegates                NO CODE
TIER 2 (ALL):              Implements within their scope         WRITES CODE
```

### Rule 2: Delegation Chain

```
USER
  |
  +--> ORCHESTRATOR-AGENT
          |
          +--> [NEW work] --> R&D-AGENT --> Creates plan --> Back to ORCHESTRATOR
          |                                                  --> USER APPROVES
          |
          +--> [APPROVED plan] --> ARCHITECTURE-AGENT --> Delegates to:
                                                              |
                                                              +--> SHELL-AGENT
                                                              +--> CORE-AGENT
                                                              +--> UI-AGENT
                                                              +--> MODULE-* agents
                                                              +--> Support agents
```

### Rule 3: Agent Spawn Protocol

When ARCHITECTURE-AGENT needs to delegate, it MUST:

**Step 1: Conflict Check (REQUIRED)**
```
Before spawning, check:
1. Is [AGENT-NAME] currently running a task?
2. Does this task's file scope overlap with any running task? (See Section 4.5.2)
3. Does this task depend on output from a running task?

If YES to any: Use WAIT or QUEUE protocol (Section 4.5.3)
If NO to all: Proceed with spawn
```

**Step 2: Spawn Task**
```
Task(
  description="[AGENT-NAME] - [brief task]",
  prompt="""
    You are [AGENT-NAME].
    First, read your agent file: C:\FathomOS_CLI\FathomOS\.claude\agents\[AGENT-NAME].md

    Your task: [describe the task]

    You can ONLY modify files in: [list scope]
    Follow all restrictions in your agent file.

    BEFORE COMPLETING: Check your queue at .claude/queues/[AGENT-NAME]-queue.md
    for any pending tasks that were blocked on this work.

    Report back when complete.
  """,
  subagent_type="general-purpose"
)
```

### Rule 4: Parallel Delegation

ARCHITECTURE-AGENT can spawn multiple agents in ONE message:

```
// GOOD: Multiple Task calls in single message
Task(...SHELL-AGENT...)
Task(...MODULE-PersonnelManagement...)
Task(...MODULE-ProjectManagement...)

// BAD: Sequential spawning when parallel is possible
```

### Rule 5: No Hierarchy Bypass

```
 WRONG: ORCHESTRATOR directly spawning SHELL-AGENT
 RIGHT: ORCHESTRATOR --> ARCHITECTURE-AGENT --> SHELL-AGENT

 WRONG: R&D-AGENT implementing code changes
 RIGHT: R&D-AGENT creates plan, ARCHITECTURE-AGENT delegates implementation

 WRONG: MODULE-A directly calling MODULE-B
 RIGHT: All inter-module communication through Shell services
```

### Rule 6: Task Conflict Detection (Required Before Spawning)

Before spawning any Task, the coordinating agent MUST:

```
1. Check for RUNNING tasks that might conflict
2. Identify FILE SCOPE conflicts (two agents modifying same files)
3. Identify DEPENDENCY conflicts (Agent B needs Agent A's output)
4. Either WAIT for conflict resolution OR QUEUE the task
```

---

## 4.5 TASK CONFLICT DETECTION AND QUEUING

### 4.5.1 Conflict Detection Rules

Before submitting ANY task, the coordinating agent (ORCHESTRATOR or ARCHITECTURE) MUST check:

| Check Type | What to Look For | Action |
|------------|------------------|--------|
| **File Scope Conflict** | Two agents would modify the same files | Wait or Queue |
| **Dependency Conflict** | Agent B requires output from Agent A's running task | Wait or Queue |
| **Resource Conflict** | Same database tables, same test fixtures | Wait or Queue |
| **Build Conflict** | Multiple agents triggering builds simultaneously | Wait or Queue |

### 4.5.2 Scope Conflict Matrix

This matrix shows which agent pairs CAN conflict based on overlapping file scopes:

```
                    SHELL  CORE   UI   CERT   LIC   DB   TEST  BUILD  DOC  MOD-*
SHELL-AGENT           -     X*    X*    -      X*    -     -      -    -     -
CORE-AGENT           X*     -     -     X      -     X     -      -    -     -
UI-AGENT             X*     -     -     -      -     -     -      -    -     -
CERTIFICATION-AGENT   -     X     -     -      -     -     -      -    -     -
LICENSING-AGENT      X*     -     -     -      -     -     -      -    -     -
DATABASE-AGENT        -     X     -     -      -     -     -      -    -     -
TEST-AGENT            -     -     -     -      -     -     -      -    -     -
BUILD-AGENT           -     -     -     -      -     -     -      -    -     -
DOCUMENTATION-AGENT   -     -     -     -      -     -     -      -    -     -
MODULE-* (same)       -     -     -     -      -     -     -      -    -     X
MODULE-* (different)  -     -     -     -      -     -     -      -    -     -

Legend:
  -  = No conflict possible (different scopes)
  X  = Potential conflict (overlapping scope)
  X* = Conflict via shared dependencies (Shell/UI DI registration, etc.)
```

**Key Conflict Scenarios:**

| Scenario | Agents | Conflict Area |
|----------|--------|---------------|
| Shell DI Changes | SHELL + any MODULE | `App.xaml.cs`, service registration |
| Core Interface Changes | CORE + CERTIFICATION | `FathomOS.Core/Interfaces/` |
| Database Schema | CORE + DATABASE | `FathomOS.Core/Data/` |
| UI Theme Changes | SHELL + UI | Shared theme resources |
| Same Module | MODULE-X + MODULE-X | Entire module folder |

### 4.5.3 Conflict Resolution Protocol

When a conflict is detected, follow this decision tree:

```
CONFLICT DETECTED
       |
       v
+------------------+
| Is the blocking  |
| task almost done?|
| (< 5 min est.)   |
+--------+---------+
         |
    +----+----+
    |         |
   YES        NO
    |         |
    v         v
  WAIT      QUEUE
    |         |
    v         v
+----------+ +------------------+
| Wait for | | Add to agent's   |
| task to  | | pending queue    |
| complete | | and continue     |
+----------+ | with other work  |
             +------------------+
```

**WAIT Protocol:**
```markdown
The coordinating agent:
1. Notes: "Waiting for [AGENT-NAME] to complete [task description]"
2. Polls or waits for completion
3. Once complete, proceeds with the dependent task
4. Use this when: Task is quick, agent has nothing else to do
```

**QUEUE Protocol:**
```markdown
The coordinating agent:
1. Creates a queue entry for the target agent
2. Notes: "Queued [task] for [AGENT-NAME] - pending completion of [blocking task]"
3. Moves on to other non-conflicting work
4. The blocked agent picks up queued task after current work
5. Use this when: Task will take time, agent can do other work meanwhile
```

### 4.5.4 Task Queue Format

Each agent can have a pending task queue. Format:

```markdown
## PENDING TASK QUEUE FOR [AGENT-NAME]

### Queued Task 1
- **Queued By:** ARCHITECTURE-AGENT
- **Blocked By:** [Current running task]
- **Priority:** High | Medium | Low
- **Description:** [Brief task description]
- **Full Prompt:**
  ```
  [The full Task prompt to execute when unblocked]
  ```
- **Queued At:** [timestamp]

### Queued Task 2
...
```

**Queue Storage Location:** `.claude/queues/[AGENT-NAME]-queue.md`

**Queue Lifecycle:**
1. **Created** when conflict detected and QUEUE chosen
2. **Picked up** by agent after completing current task
3. **Deleted** after task completion
4. **Expired** if not picked up within 24 hours (requires re-evaluation)

### 4.5.5 Conflict Handling Examples

#### Example 1: File Scope Conflict - WAIT

```
ARCHITECTURE-AGENT wants to:
  Task 1 (RUNNING): SHELL-AGENT updating App.xaml.cs service registration
  Task 2 (PENDING): MODULE-PersonnelManagement adding new service registration

CONFLICT CHECK:
  - Both tasks touch App.xaml.cs (Shell owns, but modules need registration)
  - This is a FILE SCOPE CONFLICT

RESOLUTION:
  - Task 1 is almost done (simple DI change)
  - DECISION: WAIT

ACTION:
  "Waiting for SHELL-AGENT to complete service registration before
   spawning MODULE-PersonnelManagement (both require App.xaml.cs changes)."

  [After SHELL-AGENT completes]

  Task(description="MODULE-PersonnelManagement - add service registration", ...)
```

#### Example 2: Dependency Conflict - QUEUE

```
ARCHITECTURE-AGENT wants to:
  Task 1 (RUNNING): CORE-AGENT creating new IReportService interface
  Task 2 (PENDING): MODULE-SurveyListing implementing IReportService

CONFLICT CHECK:
  - Task 2 depends on Task 1's output (interface definition)
  - This is a DEPENDENCY CONFLICT

RESOLUTION:
  - CORE-AGENT has substantial work remaining
  - ARCHITECTURE-AGENT has other non-conflicting tasks
  - DECISION: QUEUE

ACTION:
  "Creating queue entry for MODULE-SurveyListing - blocked until
   CORE-AGENT completes IReportService interface.

   Moving on to spawn TEST-AGENT for unrelated test work."

  [Creates: .claude/queues/MODULE-SurveyListing-queue.md]
  [Spawns TEST-AGENT for non-conflicting work]
```

#### Example 3: No Conflict - PARALLEL

```
ARCHITECTURE-AGENT wants to:
  Task 1: MODULE-PersonnelManagement add new view
  Task 2: MODULE-ProjectManagement add new view
  Task 3: TEST-AGENT create test fixtures

CONFLICT CHECK:
  - Task 1 scope: FathomOS.Modules.PersonnelManagement/**
  - Task 2 scope: FathomOS.Modules.ProjectManagement/**
  - Task 3 scope: FathomOS.Tests/**
  - NO OVERLAP - different file scopes
  - NO DEPENDENCIES - tasks are independent

RESOLUTION:
  - DECISION: PARALLEL EXECUTION

ACTION:
  // All three in ONE message:
  Task(description="MODULE-PersonnelManagement - add view", ...)
  Task(description="MODULE-ProjectManagement - add view", ...)
  Task(description="TEST-AGENT - create fixtures", ...)
```

#### Example 4: Multiple Conflicts - Mixed Strategy

```
ARCHITECTURE-AGENT has 5 tasks to delegate:
  Task A: SHELL-AGENT - update DI container
  Task B: MODULE-PersonnelManagement - use new DI service (depends on A)
  Task C: MODULE-ProjectManagement - use new DI service (depends on A)
  Task D: CORE-AGENT - add new interface
  Task E: DATABASE-AGENT - update schema (depends on D)

CONFLICT ANALYSIS:
  A -> B (dependency)
  A -> C (dependency)
  D -> E (dependency)
  No conflict: A and D can run parallel

EXECUTION PLAN:
  Round 1 (Parallel - no conflicts):
    - Spawn SHELL-AGENT for Task A
    - Spawn CORE-AGENT for Task D

  Round 2 (After A completes - queued):
    - Queue Task B for MODULE-PersonnelManagement
    - Queue Task C for MODULE-ProjectManagement

  Round 3 (After D completes - queued):
    - Queue Task E for DATABASE-AGENT

ACTION:
  "Spawning SHELL-AGENT and CORE-AGENT in parallel (no conflicts).
   Queuing Tasks B, C, E pending completion of their dependencies."
```

---

## 5. SCOPE MATRIX

### File Ownership Table

| Agent | Owns (Can Modify) | Cannot Modify (Delegate To) |
|-------|-------------------|----------------------------|
| **ORCHESTRATOR-AGENT** | `.claude/agents/ORCHESTRATOR-AGENT.md`, `.claude/` status files | All code files (delegate to ARCHITECTURE) |
| **R&D-AGENT** | `.claude/agents/RESEARCH-DEVELOPMENT-AGENT.md`, `.claude/plans/*.md` | All code files (plans only, no implementation) |
| **ARCHITECTURE-AGENT** | `.claude/agents/ARCHITECTURE-AGENT.md`, architecture docs | All code files (delegates to Tier 2 agents) |
| **SHELL-AGENT** | `FathomOS.Shell/**` | Core, UI, Modules, Licensing |
| **CORE-AGENT** | `FathomOS.Core/**` (except Certificates/) | Shell, Modules, UI |
| **UI-AGENT** | `FathomOS.UI/**` | Shell, Core, Modules |
| **CERTIFICATION-AGENT** | `FathomOS.Core/Certificates/**`, `FathomOS.Core/Data/*Certificate*` | Shell, UI, Modules |
| **LICENSING-AGENT** | `LicensingSystem.*/**`, `Shell/Views/ActivationWindow.*`, `Core/LicenseHelper.cs` | Everything else |
| **DATABASE-AGENT** | `FathomOS.Core/Data/**`, schema patterns | Module-specific data code |
| **TEST-AGENT** | `FathomOS.Tests/**` | Production code |
| **BUILD-AGENT** | `.github/**`, `build/**`, `installer/**` | All application code |
| **SECURITY-AGENT** | Reviews only, coordinates fixes | Does not directly modify (advisory role) |
| **DOCUMENTATION-AGENT** | All `*.md` files, `docs/**` | All code files |
| **MODULE-SurveyListing** | `FathomOS.Modules.SurveyListing/**` | Core, Shell, UI, other modules |
| **MODULE-SurveyLogbook** | `FathomOS.Modules.SurveyLogbook/**` | Core, Shell, UI, other modules |
| **MODULE-NetworkTimeSync** | `FathomOS.Modules.NetworkTimeSync/**` | Core, Shell, UI, other modules |
| **MODULE-EquipmentInventory** | `FathomOS.Modules.EquipmentInventory/**` | Core, Shell, UI, other modules |
| **MODULE-SoundVelocity** | `FathomOS.Modules.SoundVelocity/**` | Core, Shell, UI, other modules |
| **MODULE-GnssCalibration** | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.GnssCalibration/**` | Core, Shell, UI, other modules |
| **MODULE-MruCalibration** | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.MruCalibration/**` | Core, Shell, UI, other modules |
| **MODULE-UsblVerification** | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.UsblVerification/**` | Core, Shell, UI, other modules |
| **MODULE-TreeInclination** | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.TreeInclination/**` | Core, Shell, UI, other modules |
| **MODULE-RovGyroCalibration** | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.RovGyroCalibration/**` | Core, Shell, UI, other modules |
| **MODULE-VesselGyroCalibration** | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.VesselGyroCalibration/**` | Core, Shell, UI, other modules |
| **MODULE-PersonnelManagement** | `FathomOS.Modules.PersonnelManagement/**` | Core, Shell, UI, other modules |
| **MODULE-ProjectManagement** | `FathomOS.Modules.ProjectManagement/**` | Core, Shell, UI, other modules |

---

## 6. CRITICAL RULES BY AGENT TYPE

### Tier 0: ORCHESTRATOR-AGENT

```markdown
## CRITICAL RULES - READ FIRST

### NEVER DO THESE:
1. **NEVER write code directly** - Not even "simple fixes"
2. **NEVER spawn Tier 2 agents directly** - Go through ARCHITECTURE-AGENT
3. **NEVER approve plans yourself** - USER must approve all plans
4. **NEVER use subagent_type="general-purpose" for implementation**
5. **NEVER send conflicting tasks to ARCHITECTURE-AGENT simultaneously**

### ALWAYS DO THESE:
1. **NEW features** --> Send to R&D-AGENT for planning
2. **APPROVED plans** --> Send to ARCHITECTURE-AGENT for implementation
3. **Track and report progress** to USER
4. **Present plans to USER** for approval
5. **CHECK for running R&D or ARCHITECTURE tasks** before sending new work
6. **QUEUE or WAIT** if R&D or ARCHITECTURE is busy with conflicting work
```

### Tier 1: R&D-AGENT

```markdown
## CRITICAL RULES - READ FIRST

### NEVER DO THESE:
1. **NEVER implement changes directly** - You create PLANS only
2. **NEVER modify any code files** - .cs, .xaml, .csproj, etc.
3. **NEVER bypass ARCHITECTURE-AGENT** - All plans go through proper channels
4. **NEVER instruct Tier 2 agents to implement** - That's ARCHITECTURE-AGENT's job
5. **NEVER approve your own proposals** - Plans require USER approval

### ALWAYS DO THESE:
1. **ALWAYS research and analyze** before proposing
2. **ALWAYS create detailed plans** with clear agent assignments
3. **ALWAYS return plans to ORCHESTRATOR** for USER approval
4. **ALWAYS document all proposals** in `.claude/plans/`
```

### Tier 1: ARCHITECTURE-AGENT

```markdown
## CRITICAL RULES - READ FIRST

### NEVER DO THESE:
1. **NEVER write code yourself** - ALWAYS spawn Task agents
2. **NEVER use Edit/Write tools on code files** - Delegate to responsible agent
3. **NEVER implement features directly** - You are a COORDINATOR
4. **NEVER receive requests directly from USER** - Go through ORCHESTRATOR
5. **NEVER spawn agents with file scope conflicts** - Check conflict matrix first
6. **NEVER spawn dependent tasks before dependencies complete**

### ALWAYS DO THESE:
1. **ALWAYS spawn Task agents** for each piece of work
2. **ALWAYS include agent identity** in Task prompt (e.g., "You are SHELL-AGENT")
3. **ALWAYS tell agents to read their .md file** for scope and rules
4. **ALWAYS verify agents work within their scope**
5. **ALWAYS report progress to ORCHESTRATOR**
6. **ALWAYS check conflict matrix** before spawning any task (see Section 4.5.2)
7. **ALWAYS use WAIT or QUEUE protocol** when conflicts detected (see Section 4.5.3)
8. **ALWAYS check agent queues** for pending work before spawning new tasks
```

### Tier 2: Implementation Agents (SHELL, CORE, UI, CERTIFICATION, LICENSING, DATABASE, TEST, BUILD, DOCUMENTATION, MODULE-*)

```markdown
## CRITICAL RULES - READ FIRST

### NEVER DO THESE:
1. **NEVER modify files outside your designated scope** - [List specific scope]
2. **NEVER bypass the hierarchy** - Report to ARCHITECTURE-AGENT
3. **NEVER create dependencies on other modules** (for MODULE agents)
4. **NEVER duplicate services that exist in Core/Shell** (for MODULE agents)

### ALWAYS DO THESE:
1. **ALWAYS read your agent file first** when spawned
2. **ALWAYS work within your file scope** - [List specific files]
3. **ALWAYS report completion** to ARCHITECTURE-AGENT
4. **ALWAYS follow implementation standards** in your agent file
```

### Tier 2: SECURITY-AGENT (Special Case - Advisory)

```markdown
## CRITICAL RULES - READ FIRST

### NEVER DO THESE:
1. **NEVER implement fixes directly** - Review and advise only
2. **NEVER modify code** without coordinating with responsible agent
3. **NEVER approve your own security changes**
4. **NEVER weaken security mechanisms**

### ALWAYS DO THESE:
1. **ALWAYS review security-sensitive changes** before release
2. **ALWAYS report vulnerabilities** to ARCHITECTURE-AGENT
3. **ALWAYS coordinate fixes** through responsible agents
4. **ALWAYS enforce security checklist** before releases
```

---

## 7. IMPLEMENTATION CHECKLIST

### Phase 1: Update Tier 1 Agents (Priority: High)

| Agent | File | Changes Needed | Status |
|-------|------|----------------|--------|
| RESEARCH-DEVELOPMENT-AGENT | `RESEARCH-DEVELOPMENT-AGENT.md` | Add CRITICAL RULES section at top, add delegation protocol | Pending |
| ARCHITECTURE-AGENT | `ARCHITECTURE-AGENT.md` | Already good - minor formatting updates | Pending |
| ORCHESTRATOR-AGENT | `ORCHESTRATOR-AGENT.md` | Already good - minor formatting updates | Pending |

### Phase 2: Update Infrastructure Agents (Priority: High)

| Agent | File | Changes Needed | Status |
|-------|------|----------------|--------|
| SHELL-AGENT | `SHELL-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| CORE-AGENT | `CORE-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| UI-AGENT | `UI-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| CERTIFICATION-AGENT | `CERTIFICATION-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| LICENSING-AGENT | `LICENSING-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |

### Phase 3: Update Support Agents (Priority: Medium)

| Agent | File | Changes Needed | Status |
|-------|------|----------------|--------|
| DATABASE-AGENT | `DATABASE-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| TEST-AGENT | `TEST-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| BUILD-AGENT | `BUILD-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |
| SECURITY-AGENT | `SECURITY-AGENT.md` | Add CRITICAL RULES section, clarify advisory role | Pending |
| DOCUMENTATION-AGENT | `DOCUMENTATION-AGENT.md` | Add CRITICAL RULES section, standardize format | Pending |

### Phase 4: Update Module Agents (Priority: Medium)

| Agent | File | Changes Needed | Status |
|-------|------|----------------|--------|
| MODULE-SurveyListing | `MODULE-SurveyListing.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-SurveyLogbook | `MODULE-SurveyLogbook.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-NetworkTimeSync | `MODULE-NetworkTimeSync.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-EquipmentInventory | `MODULE-EquipmentInventory.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-SoundVelocity | `MODULE-SoundVelocity.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-GnssCalibration | `MODULE-GnssCalibration.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-MruCalibration | `MODULE-MruCalibration.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-UsblVerification | `MODULE-UsblVerification.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-TreeInclination | `MODULE-TreeInclination.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-RovGyroCalibration | `MODULE-RovGyroCalibration.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-VesselGyroCalibration | `MODULE-VesselGyroCalibration.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-PersonnelManagement | `MODULE-PersonnelManagement.md` | Add CRITICAL RULES section, standardize | Pending |
| MODULE-ProjectManagement | `MODULE-ProjectManagement.md` | Add CRITICAL RULES section, standardize | Pending |

---

## 8. EXAMPLES OF PROPER VS IMPROPER BEHAVIOR

### Example 1: New Feature Request

```
 WRONG:
USER: "Add a new report type to SurveyListing"
ORCHESTRATOR: *spawns SHELL-AGENT to add the feature*

 RIGHT:
USER: "Add a new report type to SurveyListing"
ORCHESTRATOR: *sends to R&D-AGENT for planning*
R&D-AGENT: *creates detailed plan*
ORCHESTRATOR: *presents plan to USER for approval*
USER: "Approved"
ORCHESTRATOR: *sends approved plan to ARCHITECTURE-AGENT*
ARCHITECTURE-AGENT: *spawns MODULE-SurveyListing to implement*
```

### Example 2: Bug Fix

```
 WRONG:
ARCHITECTURE-AGENT: *directly edits FathomOS.Shell/Services/ThemeService.cs*

 RIGHT:
ARCHITECTURE-AGENT:
  Task(
    description="SHELL-AGENT fix ThemeService bug",
    prompt="You are SHELL-AGENT. Read your agent file first.
           Fix the bug in ThemeService.cs as described..."
  )
```

### Example 3: Multi-Module Work

```
 WRONG:
ARCHITECTURE-AGENT: *spawns one general-purpose agent to update all modules*

 RIGHT:
ARCHITECTURE-AGENT:
  // In ONE message, parallel tasks:
  Task(description="MODULE-PersonnelManagement - add DI", prompt="...")
  Task(description="MODULE-ProjectManagement - add DI", prompt="...")
  Task(description="SHELL-AGENT - register new services", prompt="...")
```

### Example 4: R&D Trying to Implement

```
 WRONG:
R&D-AGENT: "Here's the plan. I'll also create the initial interface..."
*uses Edit tool on FathomOS.Core/Interfaces/INewService.cs*

 RIGHT:
R&D-AGENT: "Here's the plan. It should be implemented by:
- CORE-AGENT creates INewService.cs
- SHELL-AGENT creates NewService.cs implementation
- MODULE-X consumes the service via DI

Returning plan to ORCHESTRATOR for USER approval."
```

### Example 5: Conflict Detection - Proper Handling

```
 WRONG:
ARCHITECTURE-AGENT receives multi-part task:
  1. CORE-AGENT: Create new ICertificateValidator interface
  2. CERTIFICATION-AGENT: Implement ICertificateValidator

*Spawns both simultaneously without checking dependencies*

RESULT: CERTIFICATION-AGENT fails because interface doesn't exist yet

 RIGHT:
ARCHITECTURE-AGENT receives multi-part task:
  1. CORE-AGENT: Create new ICertificateValidator interface
  2. CERTIFICATION-AGENT: Implement ICertificateValidator

CONFLICT CHECK:
  - Task 2 depends on Task 1 output (interface must exist first)
  - DEPENDENCY CONFLICT detected

ACTION:
  "Spawning CORE-AGENT for interface creation.
   Queuing CERTIFICATION-AGENT implementation - blocked until interface complete."

  Task(description="CORE-AGENT - create ICertificateValidator", ...)
  [Creates queue entry for CERTIFICATION-AGENT]
```

### Example 6: Parallel vs Sequential - Proper Decision

```
 WRONG:
ARCHITECTURE-AGENT has tasks for 3 different modules:
*Spawns them one at a time, waiting for each to complete*

RESULT: Unnecessary slowdown - these could run in parallel

 RIGHT:
ARCHITECTURE-AGENT has tasks for 3 different modules:

CONFLICT CHECK:
  - MODULE-PersonnelManagement: FathomOS.Modules.PersonnelManagement/**
  - MODULE-ProjectManagement: FathomOS.Modules.ProjectManagement/**
  - MODULE-SurveyListing: FathomOS.Modules.SurveyListing/**
  - NO FILE OVERLAP
  - NO DEPENDENCIES

ACTION:
  "No conflicts detected. Spawning all three in parallel."

  Task(description="MODULE-PersonnelManagement - ...", ...)
  Task(description="MODULE-ProjectManagement - ...", ...)
  Task(description="MODULE-SurveyListing - ...", ...)
```

### Example 7: Queue Pickup Protocol

```
CORE-AGENT completes creating INewService interface.

BEFORE REPORTING COMPLETION:
  "Checking queue at .claude/queues/CORE-AGENT-queue.md..."
  [Queue is empty]

  "Checking if any queued tasks were blocked on my work..."
  [Finds: MODULE-SurveyListing-queue.md has task waiting for INewService]

ACTION:
  "Completed INewService interface.
   Note to ARCHITECTURE-AGENT: MODULE-SurveyListing has queued task
   that was blocked on this work and can now proceed."
```

---

## 9. ESTIMATED EFFORT

| Phase | Agents | Est. Time | Priority |
|-------|--------|-----------|----------|
| Phase 1: Tier 1 Agents | 3 agents | 1 hour | High |
| Phase 2: Infrastructure Agents | 5 agents | 2 hours | High |
| Phase 3: Support Agents | 5 agents | 2 hours | Medium |
| Phase 4: Module Agents | 13 agents | 3 hours | Medium |

**Total: ~8 hours of updates**

---

## 10. SUCCESS CRITERIA

After implementation:

1. **Every agent file has CRITICAL RULES section** at the top
2. **Every agent knows who they report to** and who they manage
3. **Every agent has clear file scope boundaries**
4. **No agent attempts to modify files outside their scope**
5. **ORCHESTRATOR, R&D, and ARCHITECTURE never write code directly**
6. **All implementation is done by Tier 2 agents**
7. **All new features go through planning -> approval -> implementation flow**
8. **Coordinating agents check for conflicts before spawning tasks**
9. **Conflicting tasks are properly queued or waited for**
10. **Agents check their queues before completing tasks**

---

## 11. NEXT STEPS

Upon USER approval of this plan:

1. ORCHESTRATOR sends approved plan to ARCHITECTURE-AGENT
2. ARCHITECTURE-AGENT delegates to DOCUMENTATION-AGENT to update all agent files
3. Updates are made in phases (Tier 1 first, then Infrastructure, Support, Modules)
4. Each updated file is reviewed for consistency
5. Final verification that all 26 agents follow the standardized template

---

## VERSION
- Created: 2026-01-16
- Updated: 2026-01-16
- Author: R&D-AGENT (RESEARCH-DEVELOPMENT-AGENT)
- Version: 1.1
- Status: Pending USER Approval

### Changelog
- **v1.1** (2026-01-16): Added Section 4.5 - Task Conflict Detection and Queuing
  - Added conflict detection rules
  - Added scope conflict matrix
  - Added WAIT vs QUEUE decision tree
  - Added task queue format specification
  - Added conflict handling examples
  - Updated CRITICAL RULES for ORCHESTRATOR and ARCHITECTURE agents
  - Updated Agent Spawn Protocol with conflict check step
  - Added success criteria for conflict management

---

## APPROVAL REQUIRED

This plan requires USER approval before implementation.

**Recommended Response:**
- "Approved" - Proceed with implementation
- "Approved with changes: [specify]" - Implement with modifications
- "Needs revision: [specify]" - Return to R&D for updates
