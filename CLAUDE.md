# Project overview
This project is a Unity-based VR cervical rehabilitation system.
The user controls gameplay through head and neck movement only.
Target movements include yaw, pitch, and roll.
The goal is to build a safe, modular, measurable prototype for cervical rehabilitation.

# Current project focus
- Head-controlled movement and interaction
- XR setup stability
- Safe iteration without breaking scenes, prefabs, or XR settings
- Gradual addition of rehabilitation gameplay mechanics
- Maintain a clean workflow for AI-assisted development

# Core architecture
- Unity is the main application.
- C# scripts should stay modular and single-purpose.
- Avoid unnecessary refactors.
- Preserve prefab references, scene references, Input Actions, and XR settings unless explicitly requested.
- Keep gameplay logic separated from UI logic when possible.

# Working rules
- Explore first, then propose a plan, then implement.
- For tasks touching multiple files, use Plan mode first.
- If a fix fails twice, stop and re-plan.
- Prefer small, reversible edits.
- Do not rename scenes, prefabs, tags, layers, serialized fields, input assets, or XR config unless explicitly approved in the plan.
- Do not make broad “cleanup” refactors unless specifically requested.

# Verification rules
Always verify changes with:
1. Compile/build check when applicable
2. Reference safety check
3. Functional check against the requested behavior
4. Regression check for nearby systems

A successful build is not sufficient proof that the feature works.

# Unity-specific safety
- Do not break serialized references.
- Do not edit multiple prefabs unless necessary.
- Do not change XR plugin/provider settings unless the task explicitly requires it.
- Keep public field renames to a minimum.
- State any Unity Inspector or Editor-side manual steps separately.
- Be careful with scene object references, prefab overrides, and Input System assets.

# Done definition
A task is done only when:
- the requested behavior exists,
- the affected files are clearly identified,
- verification has been performed,
- likely regressions are stated,
- and this file is updated if a new rule was learned.

# Known failure patterns
- Build passes but inspector references break
- Scene object names changed and scripts lose bindings
- XR settings changed indirectly
- Over-broad refactors create side effects
- Mixing UI changes and gameplay logic in one edit creates hard-to-debug regressions
- Input System or XR assets get changed unintentionally

# Preferred workflow for Claude Code
For non-trivial tasks:
1. Explore the relevant files
2. Switch to Plan mode
3. Propose the minimum safe implementation
4. Implement only after plan approval
5. Verify
6. Update CLAUDE.md if a new lesson was learned

# Required final instruction after meaningful fixes
Before finishing, update CLAUDE.md with the new failure pattern, prevention rule, and verification step learned from this fix.
