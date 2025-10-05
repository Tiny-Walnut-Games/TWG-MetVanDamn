# Living Dev Agent Onboarding Story

Welcome to the **Living Dev Agent Onboarding System** - a unified, story-driven approach to setting up your development environment that combines the best of automation, personalization, and good old-fashioned adventure!

## Overview

The onboarding system provides two primary modes designed to accommodate different preferences and time constraints:

### 🎭 Story Mode (`--story`)
An interactive, narrative-driven onboarding experience that guides you through each step with explanations, choices, and personalized configurations. Perfect for first-time users or when you want to fully understand what's being set up.

### ⚡ Quick Mode (`--quick`)
A streamlined, efficient setup process for experienced users or automated environments. Minimal prompts, maximum productivity.

## Available Modules

The onboarding system is built around modular components that can be mixed and matched based on your needs:

| Module | Description | Dependencies |
|--------|-------------|--------------|
| `ergonomics` | Ergonomic development practices and butt-saving protocols | initMyButt.sh |
| `character` | Developer character class configuration and specialization | initMyButt.sh |
| `context` | Living Dev Agent context initialization and validation tools | init_agent_context.sh |
| `tldl` | The Living Dev Log workflow and documentation system | None |
| `xp` | Experience tracking and progression systems | None |
| `unity` | Unity integration and game development tools | None |
| `ci` | Continuous integration and automation workflows | None |
| `comfort` | Comfort reminders and wellness protocols | None |

## Usage Examples

### Story Mode - Full Interactive Experience
```bash
# Launch the full interactive story
scripts/lda_story_init.sh --story

# Story mode with specific modules
scripts/lda_story_init.sh --story --modules ergonomics,character,tldl
```

### Quick Mode - Efficient Setup
```bash
# Quick setup with default modules
scripts/lda_story_init.sh --quick

# Quick setup with specific modules
scripts/lda_story_init.sh --quick --modules context,tldl,comfort

# Fully automated setup for CI/scripts
scripts/lda_story_init.sh --quick --modules context,ci --json-out setup_report.json
```

### Advanced Options
```bash
# Dry run to preview actions
scripts/lda_story_init.sh --story --dry-run --json-out preview.json

# License alignment during setup
scripts/lda_story_init.sh --quick --license-plan mit

# No color output for automation
scripts/lda_story_init.sh --quick --no-color --modules context
```

## JSON Output Format

When using `--json-out FILE`, the system generates a comprehensive report:

```json
{
  "timestamp": "2025-09-02T01:48:27+0000",
  "mode": "story|quick",
  "modules_requested": "ergonomics,character,tldl",
  "modules_completed": "ergonomics,character,tldl",
  "modules_failed": "",
  "license_plan": "mit",
  "dry_run": false,
  "status": "success|partial_success|failed",
  "version": "1.0.0"
}
```

This output can be used for:
- CI/CD pipeline validation
- Setup auditing and compliance
- Integration with other tooling
- Troubleshooting failed setups

## Module Behavior

### Graceful Degradation
The orchestrator gracefully handles missing underlying scripts by:
- Creating stub directory structures when base scripts aren't available
- Providing fallback comfort reminders
- Continuing with available modules rather than failing completely
- Clearly logging what was skipped and why

### Non-Interactive Mode
When running in non-interactive environments (CI, automation), the system:
- Automatically selects sensible defaults
- Skips interactive prompts
- Provides appropriate logging for debugging
- Exits with meaningful status codes

## Recommended Onboarding Journey

For new developers joining a project using the Living Dev Agent template:

### First Time Setup (Story Mode)
1. **Start with Story Mode**: `scripts/lda_story_init.sh --story`
2. **Select Core Modules**: ergonomics, character, context, tldl
3. **Review Generated Configurations**: Understand what was created and why
4. **Create First TLDL Entry**: Document your onboarding experience

### Subsequent Setups (Quick Mode)
1. **Use Quick Mode**: `scripts/lda_story_init.sh --quick --modules context,tldl`
2. **Generate Reports**: Include `--json-out` for tracking
3. **Customize as Needed**: Add specific modules for project requirements

### Automated/CI Environments
1. **Minimal Setup**: `scripts/lda_story_init.sh --quick --modules context,ci --no-color`
2. **Validation**: Use JSON output for pipeline verification
3. **Documentation**: Capture setup details in CI artifacts

## Integration with Legacy Scripts

The onboarding system maintains full backward compatibility with existing scripts:

- `init_agent_context.sh` - Remains unchanged, called by context module
- `init_living_dev_agent.sh` - Remains unchanged for existing workflows
- `initMyButt.sh` - Enhanced with new features but preserves existing functionality

## Future Enhancements

The modular architecture supports future expansion:

### Planned Modules
- `testing` - Test framework setup and configuration
- `security` - Security scanning and compliance tools
- `documentation` - Advanced documentation generation
- `performance` - Performance monitoring and optimization tools

### Enhanced Features
- **Module Dependencies**: Automatic resolution of module prerequisites
- **Custom Module Plugins**: User-defined modules for project-specific needs
- **Setup Profiles**: Predefined module combinations for common scenarios
- **Progressive Enhancement**: Incremental feature additions to existing setups

### Integration Opportunities
- **IDE Integration**: Direct integration with popular development environments
- **Package Manager Integration**: Automatic dependency resolution
- **Cloud Platform Integration**: Setup optimization for specific cloud environments
- **Team Synchronization**: Shared team configuration and setup standards

## Troubleshooting

### Common Issues

**Module Execution Fails**
- Check that underlying scripts are executable (`chmod +x scripts/*.sh`)
- Verify Python 3 is available for validation tools
- Review JSON output for specific error details

**Non-Interactive Mode Hangs**
- Ensure `LDA_NON_INTERACTIVE=true` environment variable is set
- Use `--no-color` flag for CI environments
- Check that required directories exist

**Missing Dependencies**
- Run with `--dry-run` first to preview required dependencies
- Install missing tools before running actual setup
- Use individual modules to isolate dependency issues

### Debug Mode

For troubleshooting, combine flags for maximum visibility:
```bash
scripts/lda_story_init.sh --story --dry-run --json-out debug.json --modules context
```

## Contributing

The onboarding system is designed for community contribution:

1. **New Modules**: Add modules following the established pattern in `execute_MODULE_module()`
2. **Enhanced Graceful Degradation**: Improve fallback behavior for missing dependencies
3. **Better Error Handling**: Add more specific error messages and recovery suggestions
4. **Documentation**: Expand this guide with real-world usage examples

---

*The Living Dev Agent Onboarding System: Making setup adventures legendary since 2025!* 🧙‍♂️✨📜