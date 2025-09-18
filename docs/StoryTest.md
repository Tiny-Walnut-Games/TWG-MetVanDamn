# Story Test (Narrative Integrity Pass)

The Story Test enforces that every symbol in the codebase plays an active, understandable role in the overall "play" of the game systems.

## Metaphor Mapping

| Narrative | Code Concept | Notes |
|-----------|--------------|-------|
| Cast | Types (MonoBehaviours, Systems, ScriptableObjects) | Must have purpose; ignored only with justification. |
| Props | Serialized fields | Must be *read* somewhere; write-only props are flagged. |
| Lines | Executable behaviors / log output | Future rules will trace invocation & chronology. |
| Scenes | Feature modules / namespaces | Grouping concept for future coverage. |
| Acts | Generation phases (Init, Build, Runtime) | Helps ensure correct ordering. |
| Phantom Prop | Field written but never read | Indicates unused or dead state. |

## Current Rule Set (v0.6.0)
1. **Phantom Props**: Serialized instance fields (public or `[SerializeField] private`) that are written (assigned anywhere in IL) but never read.
2. **Cold Lines (Cold Public Methods)**: Public instance methods (excluding Unity lifecycle / magic methods) declared but never invoked within any scanned type's IL (`call` / `callvirt`). Indicates dead API surface or external/event-only usage (justify with `StoryIgnore`).
3. **Hollow Enums**: Enum types declared under the project namespace but never referenced in any field, property, method return, or parameter signature. Suggests either obsolete design or missing integration.
4. **Premature Celebration**: A celebratory or success-style log (e.g., contains `complete`, `success`, `ready`, `generated`, `done`, `initialized`, ✅) emitted inside a method **before** any readiness flag (`*_Ready`, `*Ready`, `*Initialized`, `*Completed`, etc.) boolean field in that type is written. Indicates narrative inversion: announcing success before state is truly committed.
5. **Narrative Diffs** (meta feature): After each run, a `story-report-diff.json` highlights newly introduced violations and resolved ones compared to the previous baseline (`story-report-prev.json`).
6. **Severity Classification (NEW)**: Each rule maps to a severity contributing to an aggregate `highestSeverity`:
   - Cold Public Methods → Info
   - Phantom Props → Warning
   - Hollow Enums → Warning
   - Premature Celebrations → Error
7. **Suppression Transparency (NEW)**: Items "explained away" by either coverage overlay or reflection heuristic appear in `suppressed*` arrays (not silently ignored).

## Heuristics & Overlays (v0.6.0 Additions)
### Global Call Graph
All assemblies under the project root namespace are scanned; invocation tokens are aggregated globally to reduce false positives for Cold Public Methods (a method used only from a different type/assembly no longer shows as cold).

### Reflection Heuristic
If a field's name appears in a captured string literal alongside reflection verbs (`GetField`, `GetProperty`, `Find`, `nameof`) the "write-only" Phantom Prop is suppressed and recorded under `suppressedPhantomProps` with suffix `(heuristic-reflection)`.

### Coverage Overlay (Optional)
If a `story-coverage.json` file exists at project root it is loaded to suppress items that were actually exercised at runtime or in tests. Format:
```json
{
  "fieldsRead": ["Namespace.Type.field"],
  "methodsInvoked": ["Namespace.Type.Method()"],
  "enumsReferenced": ["Namespace.EnumType"]
}
```
Any violation suppressed by coverage is placed in its corresponding `suppressed*` list with suffix `(coverage)`.

### Severity Threshold Gating
CI can now fail based on a severity threshold via `STORYTEST_FAIL_ON_SEVERITY` environment variable (`INFO`, `WARNING`, or `ERROR`).
Examples:
```
$Env:STORYTEST_FAIL_ON_SEVERITY = 'WARNING'   # Fail if any Warning or Error
$Env:STORYTEST_FAIL_ON_SEVERITY = 'ERROR'     # Only fail on Error (Premature Celebration)
```

## Running the Test
Menu: `Tools > Story Test > Run Integrity Pass`

Outputs:
- Console summary, e.g.:
  ```
  [StoryTest] Phantom Props: 2
  [StoryTest][PhantomProp] TinyWalnutGames.MetVD.Core.WorldFoo.bar
  [StoryTest] Cold Public Methods: 1
  [StoryTest][ColdMethod] TinyWalnutGames.MetVD.Core.WorldFoo.CalculateStuff() -> Public method never invoked within assembly (may need event, call site, or StoryIgnore).
  [StoryTest] Hollow Enums: 1
  [StoryTest][HollowEnum] TinyWalnutGames.MetVD.Core.WorldPhase -> Enum type declared but never referenced...
  [StoryTest] Premature Celebrations: 1
  [StoryTest][PrematureCelebration] TinyWalnutGames.MetVD.Authoring.MetVanDAMNMapGenerator.GenerateWorldMap -> '✅ MetVanDAMN world map generation complete!' before readiness flag
  ```
- JSON file: `story-report.json` in project root:
  ```json
  {
    "phantomProps": ["Namespace.Type.field"],
    "coldPublicMethods": ["Namespace.Type.Method()"],
    "hollowEnums": ["Namespace.EnumType"],
    "prematureCelebrations": ["Namespace.Type.Method -> 'Message' before readiness flag"],
    "suppressedPhantomProps": ["Namespace.Type.coveredField (coverage)", "Namespace.Type.reflectField (heuristic-reflection)"],
    "suppressedColdPublicMethods": ["Namespace.Type.UsedOnlyInRuntime() (coverage)"],
    "suppressedHollowEnums": ["Namespace.SpareEnum (coverage)"] ,
    "suppressedPrematureCelebrations": [],
    "highestSeverity": "Warning",
    "generatedUtc": "2025-09-17T12:34:56.789Z",
    "version": "0.6.0"
  }
  ```
  Additionally on subsequent runs:
  - `story-report-prev.json` (prior baseline)
  - `story-report-diff.json` e.g.:
    ```json
    {
      "newPhantomProps": ["Namespace.Type.newDeadField"],
      "resolvedPhantomProps": ["Namespace.Type.removedField"],
      "newColdPublicMethods": [],
      "resolvedColdPublicMethods": ["Namespace.Type.PreviouslyCold()"],
      "newHollowEnums": [],
      "resolvedHollowEnums": ["Namespace.ReusedEnum"],
      "newPrematureCelebrations": ["Namespace.Type.Method -> 'Done!' before readiness flag"],
      "resolvedPrematureCelebrations": [],
      "previousVersion": "0.6.0",
      "currentVersion": "0.6.0",
      "generatedUtc": "2025-09-17T12:35:10.123Z"
    }
    ```

## CI / Pipeline Usage

Batch-mode entry point:
```
-executeMethod TinyWalnutGames.StoryTest.StoryIntegrityValidator.RunFromCI
```

Optional failure gates (non-zero exit):
1. Any violation present: set `STORYTEST_FAIL_ON_VIOLATION=1` (or `true`).
2. Severity threshold: set `STORYTEST_FAIL_ON_SEVERITY=WARNING` (or `INFO` / `ERROR`).

Example (GitHub Actions Unity step excerpt):
```yaml
    - name: Story Test Narrative Integrity
      run: |
        $Env:STORYTEST_FAIL_ON_VIOLATION = '1'
        & "${{ env.UNITY_EDITOR_PATH }}" -batchmode -quit -projectPath . \
          -executeMethod TinyWalnutGames.StoryTest.StoryIntegrityValidator.RunFromCI
```

Artifacts to collect for inspection:
- `story-report.json`
- `story-report-diff.json`

First run establishes baseline (`story-report-prev.json`). Later runs compute diff and update baseline automatically.

## Ignoring Intentional Exceptions
Use the `StoryIgnoreAttribute` with a reason:
```csharp
[StoryIgnore("Serialized for inspector live tuning; read via reflection in debug tooling.")]
[SerializeField] private float experimentalBalanceScalar;
```
Ignored items must always document rationale. Treat this like director margin notes.

## Philosophy
> Unknowns corrode comprehension. If a field exists, the audience should eventually see it matter.

This tool prioritizes *signal now* with minimal false positives and will grow incrementally.

## Roadmap (Forward Looking)

| Milestone | Feature | Description |
|-----------|---------|-------------|
| v0.7 | Deeper External Assembly Mapping | Extend invocation detection beyond project root namespace (3rd-party event hooks). |
| v0.8 | Advanced Reflection Pattern Learning | Detect chained reflection + dynamic invoke patterns; optional symbol sampling. |
| v0.9 | Act / Scene Coverage | Higher-level grouping metrics (per namespace or feature module). |
| v1.0 | Configurable Rule Weights | Allow custom severity remapping & project-specific rule toggles. |

## Contributing
- Add new rule in `StoryIntegrityValidator`.
- Extend `StoryReport` with new collection.
- Update this doc & JSON serialization wrapper.

## FAQ
**Q:** Why not rely purely on the C# compiler + analyzers?
**A:** This validator encodes *domain narrative semantics* (e.g., readiness chronology, placeholder eradication) that generic analyzers don't understand.

**Q:** Does this enforce build failure?
**A:** Not yet. Integrate into CI after false-positive trimming (target: v0.3+).

**Q:** What about reflection-based reads?
**A:** Use `[StoryIgnore("Read via reflection in X")]` until reflection trace support is added.

---
*Version: 0.6.0*
