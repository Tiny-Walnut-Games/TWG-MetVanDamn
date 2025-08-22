# 🚀 GitHub Actions Workflows

Welcome to the automation sanctuary of the Living Dev Agent! This directory houses the GitHub Actions workflows that power our continuous integration and deployment pipelines.

## 🧙‍♂️ Workflow Lore

This directory serves as the **automation hub** where our DevOps spells are crafted and maintained. Each workflow is a carefully choreographed sequence of actions that helps maintain code quality, run tests, and deploy our Living Dev Agent template with precision.

## 🎯 Workflow Structure

### Required Directory Structure
- **CRITICAL**: This directory must exist for template initialization scripts to function properly
- Created automatically by `mkdir -p .github/workflows` during bootstrap
- Expected by `scripts/init_agent_context.sh` during template setup (~180ms execution)

### Workflow Categories

#### 🛡️ Quality Assurance Workflows
- **Validation Pipeline**: Runs TLDL validation, debug overlay checks, and symbolic linting
- **Test Automation**: Executes template creation and validation scenarios
- **Performance Monitoring**: Tracks execution timing and system health

#### 🚀 Deployment Workflows
- **Template Publishing**: Automates template updates and releases
- **Documentation Deployment**: Publishes TLDL entries and documentation
- **Artifact Management**: Handles build outputs and distribution

## 🧰 Workflow Intelligence

### Expected Timing Benchmarks
- **Validation workflows**: Complete in under 200ms for each tool
- **Template creation workflows**: Execute in ~53ms for clone-and-clean operations
- **Full CI/CD pipeline**: Target completion under 5 minutes for standard operations

### 🍑 Cheek Preservation Features
- **Pre-commit validation**: Catches issues before they reach main branch
- **Rollback mechanisms**: Automatic revert capabilities for failed deployments
- **Health monitoring**: Proactive alerts for workflow failures or performance degradation

## 🧬 Manifesto Alignment

These workflows embody our core principle: **"Make development feel like a collaborative adventure while maintaining technical excellence."**

Each workflow is designed to:
- **Reduce developer anxiety** through predictable, reliable automation
- **Preserve institutional knowledge** by documenting expected behaviors
- **Enable fearless development** with comprehensive safety nets
- **Celebrate achievements** by treating successful builds as "quest completions"

## 🎮 Usage Patterns

### For Template Users
- Workflows run automatically on push/PR events
- Monitor Actions tab for "boss encounter" status (validation failures)
- Celebrate "achievement unlocks" (successful builds) in your development saga

### For Template Maintainers
- Add new workflows following the established patterns
- Maintain timing benchmarks and update documentation accordingly
- Preserve the adventure narrative in workflow names and descriptions

## 🧾 Sacred Documentation

When modifying workflows, remember:
- Each change deserves a TLDL entry documenting the quest
- Performance impacts should be measured and documented
- Failure scenarios should be treated as learning opportunities, not disasters
- Success should be celebrated as contributions to the project's legend

---

*"In the realm of automation, every workflow is a spell cast for the benefit of future adventurers."* 🧙‍♂️
