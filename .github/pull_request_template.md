## 🎭 Warbler Impact Classification

<!-- Please check one box to indicate the impact of your changes -->

- [ ] **Engine Changes** - Modifications to warbler-core runtime (types, algorithms, APIs)
- [ ] **Content-Only Changes** - Template additions/modifications in content packs
- [ ] **Tooling Changes** - Updates to validation or simulation scripts
- [ ] **Documentation** - README, comments, or guides updates only
- [ ] **CI/CD Changes** - Workflow, automation, or build configuration
- [ ] **Other** - Please describe: _______________

## 📋 Warbler Validation Checklist

<!-- Check all items that apply to ensure Dependabot + RitualBot flows remain consistent -->

### For Engine Changes (warbler-core)
- [ ] All existing unit tests pass
- [ ] New functionality includes appropriate tests
- [ ] API changes maintain backward compatibility (or are marked as breaking)
- [ ] TypeScript types are properly exported and documented
- [ ] README reflects any API changes

### For Content Pack Changes
- [ ] Templates validate successfully with `npm run pack:validate`
- [ ] All required slots are properly documented
- [ ] Template IDs are unique within the pack
- [ ] Content length limits are respected (≤400 chars recommended)
- [ ] Semantic versioning follows content-specific conventions:
  - Major: Breaking template contract changes
  - Minor: New templates (additive)
  - Patch: Content improvements, typo fixes

### For All Changes
- [ ] Warbler simulation runs successfully (`npm run warbler:simulate`)
- [ ] Build process completes without errors (`npm run build`)
- [ ] No TypeScript compilation errors
- [ ] Changes follow existing code style and conventions

## 🔄 Dependency Impact

<!-- Check if this PR affects dependency management or auto-merge flows -->

- [ ] Changes affect package.json dependencies
- [ ] Updates require Dependabot configuration changes
- [ ] Changes may impact auto-merge criteria for RitualBot Phase 0
- [ ] None of the above

## 📝 Description

<!-- Provide a clear description of your changes -->

### What Changed
<!-- Describe what you modified, added, or removed -->

### Why
<!-- Explain the motivation for this change -->

### How to Test
<!-- Describe how to verify these changes work correctly -->

### Breaking Changes
<!-- List any breaking changes and migration steps -->

## 🎯 Related Issues

<!-- Link any related issues or discussions -->

Closes #
Related to #

## 🧪 Testing Done

<!-- Describe the testing you performed -->

- [ ] Manual testing of conversation flows
- [ ] Template validation scripts
- [ ] Unit test coverage
- [ ] Integration testing with sample contexts
- [ ] Performance/memory impact testing

## 📸 Screenshots/Examples

<!-- For UI changes or new templates, include examples -->

```
Example conversation output:
User: "Hello there!"
NPC: "Hello there, Traveler! Welcome to the bustling marketplace..."
```

## 🌟 Additional Notes

<!-- Any additional context, concerns, or considerations -->

---

<!-- 
This template helps maintain consistency in Warbler development and ensures 
Dependabot + RitualBot automation flows work correctly. Thank you for contributing!
-->

**Reviewer Notes:**
- Content-only changes can typically be auto-merged after validation
- Engine changes require careful review for backward compatibility
- All changes should pass the warbler-validate workflow before merge