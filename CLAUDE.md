# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TacticalArena is a Unity 3D game project using Unity 6000.2.0b1 (Beta). This is a tactical arena-style game project with comprehensive input system configuration for multiple platforms including PC, mobile, and console support.

## Unity Project Architecture

### Rendering Pipeline
- **Universal Render Pipeline (URP)**: The project uses URP 17.2.0 with separate render asset configurations:
  - `PC_RPAsset.asset` - High-quality settings for PC platforms
  - `Mobile_RPAsset.asset` - Optimized settings for mobile devices
  - Both assets support HDR, depth texture, and opaque texture requirements

### Input System
- **Unity Input System 1.14.0** is configured with comprehensive player controls in `InputSystem_Actions.inputactions`
- **Player Action Map** includes: Move, Look, Attack, Interact (Hold), Crouch, Jump, Sprint, Previous/Next weapon
- **UI Action Map** includes standard UI navigation controls
- **Multi-platform support**: Keyboard & Mouse, Gamepad, Touch, Joystick, and XR controllers

### Quality Settings
- Two quality levels configured: Mobile (index 0) and PC (index 1)
- Default quality level set to PC (index 1)
- Mobile profile uses reduced shadow settings and disabled anti-aliasing for performance

### Project Structure
- `Assets/Scripts/` - C# scripts (currently contains placeholder `NewMonoBehaviourScript.cs`)
- `Assets/Settings/` - URP render pipeline assets and volume profiles
- `Assets/Scenes/` - Scene files including `SampleScene.unity`
- `Assets/TutorialInfo/` - Unity learning resources and tutorial assets

## Development Commands

### Building and Testing
Since this is a Unity project, building is typically done through the Unity Editor interface. Use these approaches:

**Unity Editor:**
- File → Build Settings → Build (for development builds)
- File → Build Settings → Build and Run (to build and test immediately)

**Command Line Building (if needed):**
```bash
# Open Unity project from command line (macOS)
/Applications/Unity/Hub/Editor/[Version]/Unity.app/Contents/MacOS/Unity -projectPath "/Users/diarmuidcurran/Unity Projects/TacticalArena"
```

### Code Development
- C# scripts should be placed in `Assets/Scripts/` directory
- Follow Unity's MonoBehaviour patterns for game objects
- Use the configured Input System for player controls rather than legacy Input class

## Key Dependencies

**Unity Packages:**
- `com.unity.ai.assistant` (1.0.0-pre.8) - Unity AI assistant tools
- `com.unity.ai.generators` (1.0.0-pre.12) - AI content generation
- `com.unity.ai.navigation` (2.0.7) - NavMesh and pathfinding
- `com.unity.inputsystem` (1.14.0) - New Input System
- `com.unity.render-pipelines.universal` (17.2.0) - URP rendering
- `com.unity.multiplayer.center` (1.0.0) - Multiplayer development tools

## Technical Notes

### Input System Usage
When creating player controllers, reference the `InputSystem_Actions` asset and use the pre-configured action maps:
- Player actions for gameplay controls
- UI actions for menu navigation
- Multiple control schemes already configured for cross-platform compatibility

### Rendering Considerations
- Project uses URP with HDR enabled
- Separate render assets for PC and mobile platforms
- MSAA is enabled on PC profile for better visual quality
- Mobile profile optimized for performance with reduced quality settings

### AI Integration
The project includes Unity's AI tools and generators, suggesting this may be an AI-enhanced development workflow or game with AI features.

## Platform Support
The project is configured for multi-platform deployment with optimized settings for:
- PC/Desktop (high quality rendering)
- Mobile devices (performance-optimized)
- Console controllers
- VR/XR devices
- Touch interfaces


# Claude Code Init - 3D Tactical Arena Project

## **PROJECT OVERVIEW**
**Name**: 3D Tactical Arena (1-week AI workflow validation prototype)  
**Purpose**: Test AI-assisted development methodology for future RPG development  
**Timeline**: 5 days (3 milestones, 6 sub-milestones, ~20 tasks)  
**Unity Version**: 2022.3 LTS, Built-in Render Pipeline

## **CORE GAME CONCEPT**
3D turn-based tactical combat on 4x4 grid with obstacles. Two teams of 2 units each engage in strategic combat using movement, attack, and positioning around terrain features. Minimalist geometric aesthetic using Unity primitives with solid color materials.

**Key Systems**: Grid System (4x4 battlefield), Unit System (health, movement, selection), Combat System (line-of-sight, adjacency), AI Opponent, Turn Management, Menu System

## **DEVELOPMENT METHODOLOGY**

### **Always-Playable Development**
Every task produces a testable/playable version. No abstract systems - always concrete, demonstrable progress.

### **7-Step AI Implementation Process**
1. **CREATE EDITOR SCRIPT** - Automate setup with configurable parameters
2. **PROVIDE EXECUTION INSTRUCTIONS** - Clear usage steps
3. **CREATE GAMEPLAY SCRIPTS** - Runtime systems with suggested architecture
4. **MANUAL SETUP REQUIREMENTS** - Minimal human intervention needed
5. **TESTING & VALIDATION** - Built-in validation with troubleshooting
6. **LIVING DOCUMENTATION UPDATE** - Update Assets/Docs/ProjectOverview.md
7. **TASK LEARNING SUMMARY** - Create Assets/Docs/LearningLog_Task_X_Y_Z.md

### **Editor Tool Philosophy**
Each task gets a Unity Editor Window: `Task_[Milestone]_[SubMilestone]_[TaskNumber]_Setup.cs`
- Configurable parameters for experimentation
- Setup/Reset/Validate functionality 
- One-click automation with built-in testing

## **PROJECT STRUCTURE**

### **File Organization**
```
Assets/
├── Docs/
│   ├── ProjectOverview.md (living document updated each task)
│   └── LearningLog_Task_*.md (learning summaries per task)
├── Editor/
│   └── Task_*_Setup.cs (editor tools for each task)
├── Scripts/
│   ├── Managers/ (GameManager, GridManager, etc.)
│   ├── Units/ (Unit, Health, Selection components)
│   ├── Combat/ (Attack, LineOfSight, etc.)
│   └── UI/ (MenuManager, UIManager, etc.)
├── Scenes/
│   ├── MainMenu.unity
│   └── BattleScene.unity
└── Materials/ (solid color materials for teams/obstacles)
```

### **Naming Conventions**
- **Editor Scripts**: `Task_[M]_[S]_[T]_Setup.cs` (e.g., Task_1_1_1_Setup.cs)
- **Menu Path**: `"Tactical Tools/Task [M].[S].[T] - [Task Name]"`
- **Runtime Scripts**: Descriptive names (GridManager, Unit, etc.)

## **CURRENT MILESTONE STRUCTURE**

### **Milestone 1: Foundation Systems** (2 days)
- **1.1: Grid & Environment Setup** (4 tasks) - 3D battlefield with obstacles
- **1.2: Unit System & Movement** (4 tasks) - Mouse selection and grid movement

### **Milestone 2: Combat & AI** (2 days)  
- **2.1: Combat Mechanics & Line of Sight** (4 tasks) - Attack system with tactical positioning
- **2.2: AI Opponent & Game Logic** (4 tasks) - Turn-based system with AI

### **Milestone 3: Menu & Polish** (1 day)
- **3.1: Menu System & Scene Management** (3 tasks) - Complete game flow
- **3.2: Polish & Final Integration** (3 tasks) - Final quality and validation

## **TECHNICAL ARCHITECTURE PRINCIPLES**

### **Core Systems**
- **GridManager**: 4x4 battlefield, tile positions, obstacle placement
- **Unit**: Health, team assignment, selection states, action execution  
- **GameManager**: Turn management, win conditions, game state
- **UIManager**: Interface updates, button handling, visual feedback
- **AIController**: Simple tactical decision making

### **Design Patterns**
- **Component-based architecture** using Unity MonoBehaviours
- **Event-driven communication** for loose coupling between systems
- **State management** for turn-based gameplay flow
- **Simple inheritance** for unit types and game objects

### **Integration Points**
Grid system → Unit movement → Combat validation → AI decisions → UI feedback

## **QUALITY STANDARDS**
- **Performance**: 60fps stable on development hardware
- **Visual Style**: Clean geometric aesthetic, solid colors, high contrast
- **Code Quality**: Clear naming, component separation, minimal dependencies
- **Testing**: Built-in validation in each editor tool
- **Documentation**: Living overview + learning summaries for skill development

## **AI WORKFLOW VALIDATION GOALS**
- **Editor tool effectiveness** for 3D scene setup and game systems
- **3D Unity prompting** clarity and AI execution quality
- **System integration** approaches and validation methods
- **Task breakdown** methodology for complex game development
- **Always-playable philosophy** maintenance throughout development

## **KEY FILES TO UNDERSTAND**
- `Assets/Docs/ProjectOverview.md` - Current game state and architecture
- `Assets/Docs/LearningLog_Task_*.md` - Educational summaries and insights
- Editor scripts in `Assets/Editor/` - Task automation tools
- Core managers in `Assets/Scripts/Managers/` - System architecture

---

**This project serves as a validation prototype for AI-assisted game development methodology before tackling a larger RPG project. Focus on systematic task execution, comprehensive documentation, and educational value extraction.**


# 3D Tactical Arena

## Lean Game Design Document (1-Week Prototype)

---

## **OVERVIEW**

### **1. Game Overview**

**Core Concept**
A 3D turn-based tactical combat game on a 4x4 grid battlefield with strategic obstacles. Two teams of 2 units each engage in tactical combat using movement, attack, and positioning around terrain features. Built as a 1-week prototype to validate AI-assisted development workflow.

**Target Experience**
Players experience meaningful tactical decision-making through positioning around obstacles and terrain control. Simple rules with environmental complexity create strategic depth. Clean, minimalist 3D aesthetic focuses attention on tactical positioning and battlefield control.

**Scope & Boundaries**
Includes: 4x4 grid battlefield with 2-3 obstacles, 4 total units (2 per team), 3 core actions (Move, Attack, End Turn), turn-based gameplay, basic AI opponent, simple health system (3 HP per unit), victory/defeat conditions, main menu with New Game/Quit options.

Excludes: Character progression, multiple unit types, complex abilities, narrative elements, multiple battlefields, save system, sound/music, settings menu.

**Success Vision**
"Done" means a complete tactical combat experience that can be played from start to finish in 2-3 minutes per match. Clean 3D presentation with responsive controls. Foundation systems ready for expansion. AI workflow methodology validated for future projects.

### **2. Influences & Aesthetics**

**Visual Style**
Minimalist 3D with clean geometric forms. Solid color materials on simple Unity primitives. Think modern board game aesthetic - clear, readable, functional. Isometric perspective for tactical clarity.

**Audio Direction**
No audio for initial prototype. Focus entirely on visual feedback and core mechanics.

**Gameplay Influences**

- **Chess**: Simple rules, deep tactical decisions, turn-based structure
- **Fire Emblem**: Grid-based positioning, unit health, tactical combat
- **Into the Breach**: Minimalist presentation, clear information, tactical depth
- **XCOM**: Turn-based squad tactics, positioning importance

**Narrative Tone**
No narrative elements. Pure tactical gameplay focused on mechanical execution and strategic decision-making.

---

## **SYSTEMS/MECHANICS/FEATURES**

### **3. Gameplay Loops**

**Core Loop (30 seconds)**
Select unit → choose action (Move/Attack/End) → execute action → switch to opponent turn. Simple cycle with immediate tactical feedback.

**Session Loop (2-3 minutes)**
Setup → tactical phase (alternating turns) → resolution (victory/defeat). Complete match experience with clear beginning, middle, and end.

**Progression Loop**
No progression between matches. Each game is self-contained tactical puzzle. Progression comes from player skill development and tactical understanding.

**Engagement Loop**
Visual feedback from actions → tactical analysis → decision making → action execution → result evaluation. Clear cause-and-effect with immediate feedback.

### **4. Game Mechanics**

**Unit Mechanics**
Each unit has 3 health points and can perform one action per turn. Units are destroyed when health reaches 0. Simple, binary states: alive/dead, selected/unselected.

**Grid Mechanics**
4x4 battlefield with 2-3 strategic obstacles placed to create tactical opportunities. Units occupy single tiles. Movement restricted to adjacent tiles (no diagonal). Obstacles block both movement and attacks, creating line-of-sight mechanics and positioning puzzles.

**Combat Mechanics**
Attack targets adjacent enemy units for 1 damage. Line-of-sight required - obstacles block attacks. No range restrictions beyond adjacency and line-of-sight. No miss chance - attacks always hit when line-of-sight exists.

**Terrain Mechanics**
Obstacles are impassable terrain that block movement and attacks. Create chokepoints, cover opportunities, and flanking possibilities. Strategic positioning around obstacles becomes key to victory.

**Turn Mechanics**
Player controls 2 units, AI controls 2 units. Players alternate turns. Each turn allows one action per unit. Turn ends when all units have acted or player chooses "End Turn."

### **5. Systems & Features**

**Core Systems**

- **Menu System**: Main menu with New Game/Quit functionality and scene management
- **Grid System**: 4x4 battlefield with tile-based positioning and obstacle placement
- **Unit System**: Health tracking, action management, team assignment
- **Turn System**: Turn order, action validation, win condition checking
- **UI System**: Unit selection, action buttons, game state display

**Player Features**
Menu navigation, unit selection via mouse click, movement via tile clicking, attack target selection with line-of-sight validation, turn management, game restart capability.

**Game Features**
Main menu interface, visual feedback for valid moves around obstacles, line-of-sight indicators, health display, turn indicators, victory/defeat screens with return to menu option.

**UI Features**
Clean overlay interface with action buttons, health bars, turn indicator, game state messaging (victory/defeat).

---

## **WORLD/STORY**

### **6. World & Story**

**Setting**
Abstract tactical battlefield represented as clean 4x4 grid. No specific thematic setting - focus on pure tactical mechanics.

**Locations**
Single battlefield scene with 4x4 grid and strategic obstacle placement. Example layout creates chokepoints and flanking opportunities:

```
Battlefield Layout:
[ ][ ][X][ ]   X = Obstacle (blocks movement/attacks)
[ ][ ][ ][ ]   [ ] = Open tile
[ ][ ][ ][ ]
[ ][X][ ][ ]

```

**Characters**
Generic units represented as colored cubes. Blue team (player-controlled) vs Red team (AI-controlled). No individual character identity. Obstacles represented as neutral-colored geometric shapes.

**Narrative Structure**
No narrative. Pure gameplay experience focused on tactical combat mechanics.

---

## **TECHNICAL**

### **7. Technical Architecture**

**System Overview**
Unity 3D with standard render pipeline, C# scripting, component-based architecture. Simple state management without complex event systems.

**Core Classes**

- **MenuManager**: Scene transitions, main menu UI, game state management
- **GameManager**: Turn management, win condition checking, game state
- **GridManager**: Battlefield setup, tile positioning, movement validation, obstacle placement
- **Unit**: Health, team assignment, action execution
- **UIManager**: Interface updates, button handling, visual feedback
- **AIController**: Simple opponent behavior and decision making
- **LineOfSightChecker**: Validates attacks and movement around obstacles

**Data Flow**
Player input → UI validation → GameManager state update → Grid/Unit updates → Visual feedback. Simple linear flow without complex event broadcasting.

**Integration Points**
Grid system provides movement validation, Unit system manages health/actions, UI reflects all game state changes, AI uses same systems as player.

---

## **CONTENT**

### **8. Content Overview**

**Asset Requirements**

- **3D Models**: Unity cube primitives (units), Unity plane primitive (battlefield), Unity cube primitives (obstacles)
- **Materials**: 5 simple materials (blue team, red team, obstacles, grid, background)
- **UI Graphics**: Menu buttons, game UI buttons, and health bar elements
- **Effects**: Simple particle systems for attack feedback

**Content Scope**

- **Units**: 4 total (2 per team) with identical mechanics
- **Battlefield**: Single 4x4 grid environment with 2-3 obstacles
- **Actions**: 3 total (Move, Attack, End Turn)
- **UI Elements**: ~8 interface elements (menu buttons, game buttons, health displays)
- **Materials**: 5 solid color materials maximum
- **Scenes**: 2 scenes (MainMenu, GameBattle)

**Visual Standards**
Consistent geometric aesthetic using Unity primitives. Clean solid colors with high contrast for readability. No textures or complex shaders. Maintain 60fps on development hardware.

**Quality Standards**
Responsive controls with immediate visual feedback. Clear visual communication of game state. Simple, bug-free tactical gameplay experience.

---

**Target Development Timeline**: 1 week (5 working days)

- **Days 1-2**: Grid system, obstacles, and unit movement
- **Days 3-4**: Combat system with line-of-sight mechanics
- **Day 5**: Menu system, AI opponent, and polish
**Target File Size**: Under 100MB
**Target Performance**: 60fps stable on development hardware
**Target Audience**: AI workflow validation and tactical gameplay enthusiasts

## **TACTICAL DEPTH FROM OBSTACLES**

**Strategic Elements Created:**

- **Chokepoint Control**: Narrow passages become valuable tactical positions
- **Line-of-Sight Blocking**: Units can take cover behind obstacles
- **Flanking Opportunities**: Multiple paths around obstacles create positioning choices
- **Area Denial**: Controlling key positions limits opponent movement options

**Example Tactical Scenarios:**

- **Defensive Play**: Use obstacles as cover while forcing enemies into chokepoints
- **Aggressive Play**: Use obstacles to block enemy retreat while advancing
- **Positioning**: Control both sides of chokepoints to limit enemy options

## **AI WORKFLOW TESTING PRIORITIES**

**Primary Testing Goals:**

- Editor tool effectiveness for grid/unit setup
- AI prompt clarity for 3D positioning and camera
- Integration between systems (grid, units, UI, turns)
- Validation methodology for tactical gameplay

**Secondary Benefits:**

- 3D isometric camera foundation for RPG
- Turn-based system architecture
- Simple AI behavior patterns
- UI overlay integration with 3D scene