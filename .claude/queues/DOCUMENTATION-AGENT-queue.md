# PENDING TASK QUEUE FOR DOCUMENTATION-AGENT

## Task Assignment

**From:** ARCHITECTURE-AGENT
**To:** DOCUMENTATION-AGENT
**Priority:** High
**Type:** Agent Workflow Standardization Implementation

---

## Overview

You are assigned to implement the **Agent Workflow Standardization Plan** by updating all 26 agent `.md` files. The plan has been USER APPROVED and is located at:
`C:\FathomOS_CLI\FathomOS\.claude\plans\AGENT-WORKFLOW-STANDARDIZATION-PLAN.md`

You own all `.md` files and are the appropriate agent for this work.

---

## Standardized Template to Apply

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

---

## COORDINATION

### Report To:
- **{SUPERVISOR-AGENT}** for [what]

### Coordinate With:
- **{PEER-AGENT}** for [what]

---

## VERSION
- Created: [date]
- Updated: [date]
- Version: [number]
```

---

## CRITICAL RULES BY AGENT TYPE

### For Tier 0: ORCHESTRATOR-AGENT

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

### For Tier 1: R&D-AGENT (RESEARCH-DEVELOPMENT-AGENT)

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

### For Tier 1: ARCHITECTURE-AGENT

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
6. **ALWAYS check conflict matrix** before spawning any task
7. **ALWAYS use WAIT or QUEUE protocol** when conflicts detected
8. **ALWAYS check agent queues** for pending work before spawning new tasks
```

### For Tier 2: Implementation Agents (SHELL, CORE, UI, CERTIFICATION, LICENSING, DATABASE, TEST, BUILD, DOCUMENTATION, MODULE-*)

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

### For SECURITY-AGENT (Special Case - Advisory)

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

## PHASE 1: Tier 1 Agents (3 agents)

| Agent File | Changes Needed |
|------------|----------------|
| `RESEARCH-DEVELOPMENT-AGENT.md` | Add CRITICAL RULES section at top using R&D template above. Keep existing content, reorganize to match template structure. |
| `ARCHITECTURE-AGENT.md` | Already has CRITICAL RULES - verify formatting matches template, add conflict detection rules |
| `ORCHESTRATOR-AGENT.md` | Already has CRITICAL RULES - verify formatting matches template, add conflict detection rules |

---

## PHASE 2: Infrastructure Agents (5 agents)

| Agent File | Scope | Changes Needed |
|------------|-------|----------------|
| `SHELL-AGENT.md` | `FathomOS.Shell/**` | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `CORE-AGENT.md` | `FathomOS.Core/**` (except Certificates/) | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `UI-AGENT.md` | `FathomOS.UI/**` | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `CERTIFICATION-AGENT.md` | `FathomOS.Core/Certificates/**`, `FathomOS.Core/Data/*Certificate*` | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `LICENSING-AGENT.md` | `LicensingSystem.*/**`, `Shell/Views/ActivationWindow.*`, `Core/LicenseHelper.cs` | Add CRITICAL RULES section, standardize format per Tier 2 template |

---

## PHASE 3: Support Agents (5 agents)

| Agent File | Scope | Changes Needed |
|------------|-------|----------------|
| `DATABASE-AGENT.md` | `FathomOS.Core/Data/**`, schema patterns | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `TEST-AGENT.md` | `FathomOS.Tests/**` | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `BUILD-AGENT.md` | `.github/**`, `build/**`, `installer/**` | Add CRITICAL RULES section, standardize format per Tier 2 template |
| `SECURITY-AGENT.md` | Reviews only, coordinates fixes (advisory role) | Add CRITICAL RULES section per SECURITY template, standardize format |
| `DOCUMENTATION-AGENT.md` | All `*.md` files, `docs/**` | Add CRITICAL RULES section, standardize format per Tier 2 template |

---

## PHASE 4: Module Agents (13 agents)

All MODULE agents follow the same Tier 2 template with these specifics:
- Report to: ARCHITECTURE-AGENT
- Manage: None - they are implementers
- Key restrictions: No cross-module dependencies, must use Core/Shell services via DI

| Agent File | Scope |
|------------|-------|
| `MODULE-SurveyListing.md` | `FathomOS.Modules.SurveyListing/**` |
| `MODULE-SurveyLogbook.md` | `FathomOS.Modules.SurveyLogbook/**` |
| `MODULE-NetworkTimeSync.md` | `FathomOS.Modules.NetworkTimeSync/**` |
| `MODULE-EquipmentInventory.md` | `FathomOS.Modules.EquipmentInventory/**` |
| `MODULE-SoundVelocity.md` | `FathomOS.Modules.SoundVelocity/**` |
| `MODULE-GnssCalibration.md` | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.GnssCalibration/**` |
| `MODULE-MruCalibration.md` | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.MruCalibration/**` |
| `MODULE-UsblVerification.md` | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.UsblVerification/**` |
| `MODULE-TreeInclination.md` | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.TreeInclination/**` |
| `MODULE-RovGyroCalibration.md` | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.RovGyroCalibration/**` |
| `MODULE-VesselGyroCalibration.md` | `FathomOS.ModuleGroups.Calibrations/FathomOS.Modules.VesselGyroCalibration/**` |
| `MODULE-PersonnelManagement.md` | `FathomOS.Modules.PersonnelManagement/**` |
| `MODULE-ProjectManagement.md` | `FathomOS.Modules.ProjectManagement/**` |

---

## Execution Instructions

1. **Read the full plan** at `C:\FathomOS_CLI\FathomOS\.claude\plans\AGENT-WORKFLOW-STANDARDIZATION-PLAN.md`
2. **Process each phase** in order (Phase 1 first for highest priority agents)
3. **For each agent file:**
   - Read the current content
   - Preserve all existing valuable content (responsibilities, coordination sections, implementation standards)
   - Add or move CRITICAL RULES section to the TOP (right after Identity section)
   - Ensure hierarchy position is clearly documented
   - Ensure file scope is clearly documented
   - Update version number and date
4. **Update the Implementation Checklist** in the plan file as you complete each agent
5. **Report completion** of each phase to ARCHITECTURE-AGENT

---

## Success Criteria

After updates:
1. Every agent file has CRITICAL RULES section at the top
2. Every agent knows who they report to and who they manage
3. Every agent has clear file scope boundaries
4. Template structure is consistent across all files

---

## Queue Metadata

- **Queued By:** ARCHITECTURE-AGENT
- **Priority:** High
- **Queued At:** 2026-01-16
- **Blocked By:** None - this is the primary task

---

**Begin execution when spawned.**
