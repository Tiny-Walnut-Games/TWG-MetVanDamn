# Extensibility & Sacred Code

MetVanDAMN separates core systems from intended expansion zones to preserve stability while enabling customization.

## Sacred Code Classification Protocol

- ??? PROTECTED CORE – Core ECS systems, blittable components, coordinate math
- ?? INTENDED EXPANSION – Validation rules, art strategies, BiomeArtProfile configuration
- ?? COORDINATE-AWARE – Systems that use node coordinates for spatial intelligence

Only modify INTENDED EXPANSION ZONES directly. Extend core via configuration, interfaces, or ScriptableObjects.

## Patterns

- Null-Proof by Construction: initialize references in Awake/OnEnable; expose readiness flags instead of null
- Deterministic Defaults: provide fallback values for all fields and parameters
- No Silent Failures: surface errors via logs or exceptions in editor tooling
- Sealed by Default: seal classes unless explicit inheritance is required

## Examples

- Replace prop placement strategy via a ScriptableObject strategy and inject it into the placement system
- Augment biome transition with custom curves via BiomeTransitionProfile

## Testing

- Add unit tests for critical math/scheduling logic
- Add integration tests for authoring/baker pipelines
