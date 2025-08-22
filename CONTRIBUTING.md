# Contributing to Living Dev Agent Template

Thank you for your interest in contributing to the Living Dev Agent Template! This project aims to provide a comprehensive template for AI-powered development workflows with integrated TLDL (Living Dev Log) and DevTimeTravel capabilities.

## 🎯 Project Overview

The Living Dev Agent Template is designed to:
- Package proven development workflows into a reusable template
- Integrate AI-powered development tools (GitHub Copilot, etc.)
- Provide structured documentation through TLDL entries
- Enable development context capture via DevTimeTravel
- Include comprehensive linting and validation tools
- Support seamless CI/CD integration

## 🚀 Getting Started

### Prerequisites

- **Python 3.11+** for linting and validation scripts
- **Git** for version control
- **GitHub Copilot** (recommended) for AI assistance
- **Shell/Bash** for script execution

### Initial Setup

1. **Clone the template** (or use it as a GitHub template):
   ```bash
   git clone https://github.com/your-username/living-dev-agent-template.git
   cd living-dev-agent-template
   ```

2. **Initialize the development environment**:
   ```bash
   chmod +x scripts/init_agent_context.sh
   scripts/init_agent_context.sh --verbose
   ```

3. **Install Python dependencies**:
   ```bash
   pip install -r scripts/requirements.txt
   ```

4. **Run validation checks**:
   ```bash
   python src/SymbolicLinter/validate_docs.py --tldl-path docs/
   python src/SymbolicLinter/symbolic_linter.py --path src/
   ```

## 📋 Development Workflow

## 📚 Documentation Architecture

### Content Placement Guidelines

Following the docs architecture cutover (Issue #87), all content must be placed in the correct location:

#### 📜 TLDL Entries → `TLDL/entries/`
**Use for**: Time-bound development narratives, discoveries, and adventures
- **Format**: `TLDL-YYYY-MM-DD-Title.md`
- **Examples**: Bug fixes, feature implementations, investigations, learnings
- **Creation**: `scripts/init_agent_context.sh --create-tldl "Title"`

#### 📖 Evergreen Documentation → `docs/`
**Use for**: Timeless guides, references, and operational procedures
- **Examples**: Setup guides, API references, playbooks, architectural decisions
- **Navigation**: Must be included in `docs/SUMMARY.md` for GitBook
- **Format**: Descriptive names without timestamps

#### 🗂️ Templates → `templates/`
**Use for**: Template files, examples, and boilerplate content
- **Examples**: TLDL templates, project templates, configuration examples

### Referencing Between Sections

- **From docs/ → TLDL/**: Use relative paths `../TLDL/entries/TLDL-YYYY-MM-DD-Title.md`
- **From TLDL/ → docs/**: Use relative paths `../docs/guide-name.md`
- **Monthly archives**: Link to `docs/TLDL-Monthly/YYYY-MM.md` for consolidated views

### CI Enforcement

The repository includes automated checks to prevent misplacement:
```bash
# Run locally to check compliance
python3 scripts/directory_lint.py
```

## 📋 Development Workflow

### TLDL (Living Dev Log) Process

All significant development work should be documented using TLDL entries:

1. **Create a TLDL entry** for your work:
   ```bash
   scripts/init_agent_context.sh --create-tldl "YourFeatureName"
   ```

2. **Update the TLDL entry** as you work:
   - Document discoveries and learnings
   - Record actions taken and rationale
   - Note any issues or blockers
   - Update next steps

3. **Complete the TLDL entry** when work is finished:
   - Summarize final outcomes
   - Document any remaining work
   - Link to relevant PRs, issues, or documentation
3. **Complete the TLDL entry** when work is finished:
   - Summarize final outcomes
   - Document any remaining work
   - Link to relevant PRs, issues, or documentation

### **🔄 Interactive AI Collaboration Workflow**

**New Methodology**: Use **ping-and-fix patterns** for optimal AI collaboration efficiency.

#### **Real-Time Issue Resolution**
Instead of traditional review cycles, use immediate feedback:

```bash
# When you encounter issues during development:
@copilot this test is failing - need to fix the validation logic
@copilot CI error on import - missing dependency installation  
@copilot neutrality check has false positives - exclude documentation files
```

#### **Benefits of Interactive Guidance**
- **2x faster resolution** compared to formal review cycles
- **Prevents error compounding** through immediate corrections
- **Maintains context** while issues are fresh
- **Enables dynamic adaptation** during task execution

#### **Implementation Pattern**
1. **Start with clear direction**: Provide initial task context
2. **Monitor progress**: Watch for issues or unexpected results  
3. **Provide real-time guidance**: Use ping comments for immediate fixes
4. **Validate incrementally**: Confirm fixes before continuing
5. **Complete with confidence**: Final approval after all issues resolved

#### **Anti-Patterns to Avoid**
- ❌ Waiting for full completion before providing feedback
- ❌ Batch reviewing entire results at the end
- ❌ Providing vague or non-specific guidance
- ❌ Creating formal change requests for simple fixes

> **Workflow Achievement Unlock**: The "slot instructions in the middle of a task" philosophy creates tighter feedback loops and dramatically improves collaboration efficiency.

### Code Changes

1. **Create a feature branch**:
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **Make incremental changes** with frequent commits:
   ```bash
   git add .
   git commit -m "feat: add new validation tool

   - Implemented debug overlay validator
   - Added comprehensive test coverage
   - Updated documentation

   TLDL: TLDL-2024-XX-XX-DebugValidation"
   ```

3. **Run linters and validation** frequently:
   ```bash
   # Run all linters
   python src/SymbolicLinter/ecs_system_linter.py --path src/
   python src/SymbolicLinter/symbolic_linter.py --path src/
   python src/SymbolicLinter/validate_docs.py --tldl-path docs/
   
   # Run debug overlay validation
   python src/DebugOverlayValidation/debug_overlay_validator.py --path src/DebugOverlayValidation/
   ```

4. **Update documentation** as needed:
   - Update `docs/Copilot-Setup.md` for setup changes
   - Modify configuration files if behavior changes
   - Add examples for new features

### Pull Request Process

1. **Ensure all validation passes**:
   ```bash
   # Full validation suite
   scripts/init_agent_context.sh --dry-run
   ```

2. **Create pull request** with:
   - Clear description of changes
   - Link to related TLDL entry
   - Reference any issues addressed
   - Include testing information

3. **Address review feedback** and update TLDL entries accordingly

## 🛠️ Component Architecture

### Directory Structure

```
living-dev-agent-template/
├── .github/                    # GitHub workflows and templates
│   ├── workflows/
│   └── ISSUE_TEMPLATE/
├── docs/                       # Documentation and TLDL entries
│   ├── Copilot-Setup.md       # Setup instructions
│   ├── devtimetravel_snapshot.yaml
│   └── tldl_template.yaml
├── scripts/                    # Utility scripts
│   ├── clone-and-clean.sh     # Template initialization
│   └── init_agent_context.sh  # Context setup
├── src/                        # Source code and tools
│   ├── DebugOverlayValidation/ # Debug system validation
│   └── SymbolicLinter/         # Linting and validation tools
├── .editorconfig              # Editor configuration
├── TWG-Copilot-Agent.yaml     # Copilot integration config
├── mcp-config.json            # MCP server configuration
└── CONTRIBUTING.md            # This file
```

### Key Components

#### Linting and Validation Tools

- **ECS System Linter** (`src/SymbolicLinter/ecs_system_linter.py`): Validates ECS system architecture
- **Symbolic Linter** (`src/SymbolicLinter/symbolic_linter.py`): Checks symbol resolution
- **Documentation Validator** (`src/SymbolicLinter/validate_docs.py`): Validates TLDL entries
- **Debug Overlay Validator** (`src/DebugOverlayValidation/debug_overlay_validator.py`): Validates debug systems

#### Configuration Files

- **EditorConfig** (`.editorconfig`): Consistent coding styles
- **Copilot Configuration** (`TWG-Copilot-Agent.yaml`): AI assistant behavior
- **MCP Configuration** (`mcp-config.json`): Model Context Protocol settings

#### Scripts

- **Clone and Clean** (`scripts/clone-and-clean.sh`): Template setup script
- **Agent Context Initialization** (`scripts/init_agent_context.sh`): Development environment setup

## 🧪 Testing Guidelines

### Validation Testing

All validation tools should be tested with:

1. **Valid input cases**: Ensure tools pass correct code/documentation
2. **Invalid input cases**: Verify tools catch errors appropriately
3. **Edge cases**: Test boundary conditions and unusual inputs
4. **Performance**: Ensure tools run efficiently on large codebases

### Integration Testing

Test the complete workflow:

1. **Template initialization**: Using `clone-and-clean.sh`
2. **Context setup**: Using `init_agent_context.sh`
3. **TLDL creation**: Create and validate TLDL entries
4. **DevTimeTravel**: Snapshot creation and validation
5. **CI/CD integration**: Verify workflows run correctly

### Manual Testing

Before submitting changes:

1. **Create a test project** using the template
2. **Follow the complete setup process**
3. **Verify all scripts work correctly**
4. **Test with various project types**

## 📝 Documentation Standards

### TLDL Entry Requirements

All TLDL entries must include:

- **Entry ID**: Format `TLDL-YYYY-MM-DD-DescriptiveTitle`
- **Author**: GitHub username or `@copilot`
- **Context**: Related issue, feature, or investigation
- **Summary**: One-line description of work
- **Discoveries**: Key learnings and insights
- **Actions Taken**: What was done and why
- **Next Steps**: Follow-up work needed

### 📜 Chronicle Keeper Integration

The Chronicle Keeper is an automated system that preserves lore-worthy content. As a contributor, you can invoke the Chronicle Keeper using these patterns:

#### **Automatic Triggers**
- **🧠 in issue titles**: Marks issues for automatic TLDL generation
- **Merged PRs**: Automatically creates basic documentation entries
- **Failed workflows**: Captures lessons from build/test failures

#### **Manual Triggers in Comments**
Use these patterns in GitHub issue/PR comments:

```markdown
TLDL: The validation system expects YAML front-matter but our templates 
use markdown headers. This mismatch causes silent failures that are 
difficult to debug.
```

```markdown
📜 Key architectural decision: Chose event-driven pattern over direct 
method calls for better testability and future extensibility. The 
performance overhead is minimal and maintainability benefits are significant.
```

#### **Best Practices for Chronicle Keeper**
- **Be specific**: Include enough context for future developers
- **Focus on insights**: Document the "why" behind decisions and discoveries
- **Reference sources**: Link to relevant code, issues, or documentation
- **Consider timing**: Use triggers when you have genuine wisdom to preserve

#### **Chronicle Keeper Review Process**
1. Auto-generated TLDL entries are created in `docs/`
2. Review the generated content for accuracy and completeness
3. Add additional context, references, or corrections as needed
4. Ensure the entry provides value for future development work

### Code Documentation

- **Python**: Use docstrings for all functions and classes
- **Shell Scripts**: Include comments for complex logic
- **Configuration**: Comment non-obvious settings
- **README Updates**: Keep setup instructions current

### Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Examples:
- `feat: add new validation tool`
- `fix: resolve symbolic linter path issue`
- `docs: update setup instructions`
- `refactor: improve error handling in validator`

## 🐛 Issue Reporting

### Bug Reports

Use the provided bug report template and include:

- **Environment**: Operating system, Python version, etc.
- **Steps to reproduce**: Clear, minimal reproduction steps
- **Expected behavior**: What should happen
- **Actual behavior**: What actually happens
- **Logs**: Relevant error messages or output

### Feature Requests

Use the feature request template and include:

- **Problem statement**: What issue does this solve?
- **Proposed solution**: How should it work?
- **Alternatives**: Other approaches considered
- **Impact**: Which components would be affected?

## 🚦 Code Review Guidelines

### For Contributors

- **Keep changes focused**: One feature or fix per PR
- **Write clear descriptions**: Explain what and why
- **Include tests**: Add or update validation tests
- **Update documentation**: Keep docs synchronized
- **Follow coding standards**: Use linters and formatters
- **Use interactive collaboration**: Apply ping-and-fix methodology for AI assistance

#### **🚀 Optimized AI Collaboration Patterns**

When working with AI assistants (like @copilot), use these proven patterns:

**✅ Effective Ping-and-Fix Examples:**
```bash
@copilot this linter is failing on line 45 - fix the import statement
@copilot CI test failing on PyYAML - add dependency to requirements  
@copilot neutrality validation flagging docs - exclude .md files
@copilot this function needs error handling for edge case X
```

**❌ Inefficient Traditional Approaches:**
- Waiting for complete task finish before providing feedback
- Creating formal change requests for simple fixes
- Providing vague feedback without specific guidance
- Batch reviewing entire results instead of incremental validation

**Benefits of Interactive Guidance:**
- **2x faster resolution** than traditional review cycles
- **Real-time course correction** prevents compounding errors  
- **Context preservation** while issues are fresh
- **Collaborative debugging** with tight feedback loops

### For Reviewers

- **Focus on correctness**: Does the code work as intended?
- **Check documentation**: Is it complete and accurate?
- **Verify testing**: Are changes adequately tested?
- **Consider maintenance**: Is the code easy to maintain?
- **Validate integration**: Does it work with existing tools?
- **Leverage interactive patterns**: Use ping comments for immediate clarification

## 🏷️ Release Process

### Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR**: Breaking changes to template structure or API
- **MINOR**: New features, backward-compatible
- **PATCH**: Bug fixes, documentation updates

### Release Checklist

1. **Update version numbers** in configuration files
2. **Update documentation** with new features
3. **Run full validation suite**
4. **Test template creation** end-to-end
5. **Create release notes** with change summary
6. **Tag release** in Git
7. **Update GitHub template** if applicable

## 📄 Licensing and Legal

### License Compliance

This project is licensed under the GNU General Public License v3.0. All contributions must:

- **Include appropriate license headers** in new source files
- **Maintain GPL compatibility** for any dependencies added
- **Respect copyleft requirements** for derivative works
- **Follow GPL distribution terms** for any redistributions

### Adding License Headers

New source files must include the standard GPLv3 header:

```python
#!/usr/bin/env python3
"""
Brief description of the file's purpose

Copyright (C) 2025 Bellok

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
"""
```

### Channels

- **GitHub Issues**: Bug reports, feature requests
- **GitHub Discussions**: General questions, ideas
- **Pull Request Comments**: Code-specific discussions
- **TLDL Entries**: Development process documentation

### Response Times

We aim to:
- **Acknowledge issues**: Within 48 hours
- **Review pull requests**: Within 1 week
- **Address bugs**: Based on severity
- **Implement features**: Based on community need and complexity

## 📚 Additional Resources

### Related Documentation

- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [EditorConfig](https://editorconfig.org/)
- [Semantic Versioning](https://semver.org/)
- **[Workflow Evolution Insights](docs/TLDL-2024-12-19-WorkflowEvolutionInsights.md)**: Breakthrough discoveries in AI collaboration methodology

### Workflow Methodology References

- **Ping-and-Fix Patterns**: Interactive AI collaboration methodology
- **Slot Instructions Philosophy**: Real-time guidance during task execution  
- **Interactive vs Batch Processing**: Efficiency comparison and implementation guides
- **TLDL Documentation**: Living Dev Log best practices and templates

### Community

- **Discussions**: Use GitHub Discussions for questions
- **Examples**: Check existing TLDL entries for patterns
- **Best Practices**: Review successful PRs for guidance

---

## ❤️ Thank You

Your contributions help make AI-powered development workflows more accessible and effective for everyone. We appreciate your time and effort in improving this template!

For questions or clarification on any aspect of contributing, please open a GitHub Discussion or reach out through the appropriate channels listed above.