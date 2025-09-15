# Contributing to Living Dev Agent Template

Welcome to the Living Dev Agent Template project! This guide will help you contribute to Jerry Meyer's debugging and development workflow tools.

## Quick Start for Contributors

### 1. Setup Development Environment

```bash
# Fork and clone the repository
git clone https://github.com/your-username/living-dev-agent-template.git
cd living-dev-agent-template

# Set up your development environment
mkdir -p .github/workflows  # Required directory
pip install -r scripts/requirements.txt  # May timeout but that's acceptable
chmod +x scripts/*.sh

# Initialize the template system
scripts/init_agent_context.sh  # ~180ms - don't cancel

# Run validation tools
python3 src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/
python3 src/SymbolicLinter/symbolic_linter.py --path src/
python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/DebugOverlayValidation/
```

### 2. Create Development Branch

```bash
# Create feature branch
git checkout -b feature/your-contribution
# or
git checkout -b fix/issue-description
# or  
git checkout -b docs/documentation-improvement
```

### 3. Development Workflow

#### For Code Changes:
1. Create TLDL entry: `scripts/init_agent_context.sh --create-tldl "YourFeatureName"`
2. Make your changes with clear commit messages
3. Run validation suite to ensure quality
4. Update TLDL entry with implementation details
5. Test thoroughly with provided scenarios

#### For Documentation Changes:
1. Follow clear documentation standards
2. Include practical examples
3. Validate documentation using TLDL validator
4. Ensure examples are copy-pasteable

## Contribution Types

### Code Contributions

#### Validation Tools Enhancement
- Improve execution time (target: <200ms for all tools)
- Add new validation patterns
- Enhance error reporting
- Cross-platform compatibility improvements

#### Console Commentary & Code Snapshots
- New tagging categories for debugging sessions
- Additional preset configurations
- Integration improvements
- Performance optimizations

#### TaskMaster & Time Tracking
- Enhanced project management features
- Better time tracking analytics
- Improved reporting capabilities
- Workflow optimizations

#### CI/CD Improvements
- Faster pipeline execution
- Better error handling
- Enhanced security scanning
- Improved reporting

### Documentation Contributions

#### TLDL Entries
- Development process documentation
- Troubleshooting guides
- Best practices
- Real-world usage examples

#### Tutorial Content
- Step-by-step setup guides
- Integration examples
- Migration guides
- Video tutorial scripts

#### Architecture Documentation
- System design explanations
- Integration patterns
- Performance considerations
- Security implementation

### Security Contributions

#### Security Hardening
- Vulnerability assessments
- Security pattern improvements
- Access control enhancements
- Audit trail improvements

#### Security Documentation
- Security best practices
- Threat modeling
- Incident response procedures
- Security testing guides

## Development Guidelines

### Code Quality Standards

#### Python Style
```python
#!/usr/bin/env python3
"""
Living Dev Agent Template - Module Name
Description of module functionality

Execution time: ~XXms for typical operations
"""

from pathlib import Path
from typing import List, Dict, Any, Optional
import datetime

class ClassName:
    """Description of class functionality"""
    
    def __init__(self, workspace_path: str = "."):
        self.workspace_path = Path(workspace_path)
    
    def method_name(self, parameter: str) -> bool:
        """Method description with clear documentation"""
        try:
            # Implementation with proper error handling
            return True
        except Exception as e:
            self.log_error(f"Operation failed: {e}")
            return False
```

#### Shell Script Style
```bash
#!/bin/bash
# Living Dev Agent Template - Script Name
# Description of script functionality
# Execution time: ~XXms

set -euo pipefail  # Strict error handling

echo "Starting operation..."

# Always validate inputs
if [[ -z "${1:-}" ]]; then
    echo "ERROR: Parameter required"
    exit 1
fi

# Use proper error checking
if ! command -v python3 &> /dev/null; then
    echo "ERROR: Python 3 not found"
    exit 1
fi

echo "Operation completed successfully"
```

### Documentation Standards

#### TLDL Entry Format
```markdown
# TLDL-YYYY-MM-DD-DescriptiveTitle

**Entry ID:** TLDL-YYYY-MM-DD-DescriptiveTitle  
**Author:** Your Name  
**Context:** Brief context description  
**Summary:** One-line summary of what was accomplished

---

## Objective

What you're trying to accomplish.

## Discovery

What you learned during this work.

## Actions Taken

Specific steps taken, including commands and decisions.

## Key Insights

Important insights and patterns discovered.

## Challenges Encountered

Obstacles faced and how they were overcome.

## Next Steps

What needs to happen next.

---

## TLDL Metadata
**Tags**: #relevant #tags #here  
**Complexity**: Low/Medium/High  
**Impact**: Low/Medium/High  
**Duration**: Time spent on this work  
```

#### Documentation Style
- Use clear, professional language
- Include practical examples
- Provide performance expectations
- Add troubleshooting sections
- Use structured formatting
- Include attribution where appropriate

## Testing Guidelines

### Required Testing Scenarios

#### Template Creation Testing
```bash
# Test complete template creation workflow
cd /tmp
mkdir test-template
cd test-template

# Run template creation
/path/to/template/scripts/clone-and-clean.sh .

# Verify structure
ls -la  # Should show all template files
git log --oneline  # Should show initial commit

# Test initialization
scripts/init_agent_context.sh
scripts/init_agent_context.sh --create-tldl "TestFeature"

# Run validation suite
python3 src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/
python3 src/SymbolicLinter/symbolic_linter.py --path src/
python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/DebugOverlayValidation/
```

#### Cross-Platform Testing
- **Linux**: Ubuntu 20.04+ with Python 3.8+
- **macOS**: Latest 2 versions with Python 3.8+
- **Windows**: Windows 10+ with WSL2 or native Python

#### Performance Testing
```bash
# Time all critical operations
time scripts/init_agent_context.sh  # Should be <1 second
time python3 src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/  # <200ms
time python3 src/SymbolicLinter/symbolic_linter.py --path src/  # <200ms
```

### Manual Testing Checklist

- [ ] Template creation in clean directory
- [ ] Git repository initialization
- [ ] Script permissions and execution
- [ ] Python dependency installation
- [ ] Validation tools execution
- [ ] TLDL entry creation
- [ ] Console commentary session
- [ ] Code snapshot capture
- [ ] TaskMaster task creation
- [ ] Time tracking functionality

## Submission Process

### Before Submitting

1. **Run complete validation suite**:
   ```bash
   python3 src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/ --verbose
   python3 src/SymbolicLinter/symbolic_linter.py --path src/ --verbose
   python3 src/DebugOverlayValidation/debug_overlay_validator.py --path src/DebugOverlayValidation/ --verbose
   ```

2. **Test template creation**:
   ```bash
   scripts/clone-and-clean.sh /tmp/test-template
   cd /tmp/test-template
   scripts/init_agent_context.sh
   ```

3. **Update documentation** with your contribution details

4. **Use clear commit messages**:
   ```bash
   git commit -m "Add code snapshot preset for architecture analysis"
   git commit -m "Fix security validation timeout handling"
   git commit -m "Update documentation for TaskMaster integration"
   ```

### Pull Request Guidelines

#### PR Title Format
- `[FEATURE] Add description`
- `[FIX] Resolve issue description`
- `[DOCS] Update documentation type`
- `[PERF] Optimize component description`

#### PR Description Template
```markdown
## Summary

Brief description of what this PR accomplishes.

## Changes Made

- [ ] Specific change 1
- [ ] Specific change 2  
- [ ] Documentation updates
- [ ] Tests added/updated

## Testing Completed

- [ ] Template creation test passed
- [ ] Validation suite passed (all tools <200ms)
- [ ] Cross-platform testing completed
- [ ] Manual testing checklist completed

## TLDL Entry

Link to the TLDL entry documenting this work: `TLDL/entries/TLDL-YYYY-MM-DD-YourContribution.md`

## Additional Notes

Any additional context, decisions made, or future considerations.
```

### Review Process

1. **Automated Validation**: CI/CD runs complete validation suite
2. **Code Review**: Maintainer reviews for quality and consistency
3. **Testing Review**: Verification of testing completeness
4. **Documentation Review**: TLDL and documentation quality check
5. **Final Approval**: Approval with feedback

## Recognition System

### Contributor Levels

#### New Contributor
- First successful contribution merged
- Understands the development workflow
- Creates proper TLDL entries

#### Regular Contributor
- 5+ contributions merged
- Helps other contributors with reviews
- Maintains consistent quality standards

#### Core Contributor
- 10+ contributions merged
- Significant impact on template functionality
- Helps guide project direction

#### Maintainer
- Major architectural contributions
- Security and quality leadership
- Community engagement and support

### Achievements

- **Detective**: Found and fixed a critical bug
- **Developer**: Implemented a significant new feature
- **Documenter**: Created high-quality documentation
- **Guardian**: Enhanced security significantly
- **Optimizer**: Improved performance measurably
- **Architect**: Designed major system improvements

## Getting Help

### Communication Channels

#### GitHub
- **Issues**: Bug reports and feature requests
- **Discussions**: General questions and ideas
- **Pull Requests**: Code review and collaboration

#### Getting Started
- **Documentation**: Start with improving guides and examples
- **Small Fixes**: Look for issues labeled `good-first-issue`
- **Testing**: Add test cases for existing functionality
- **Examples**: Create better usage examples

#### Advanced Contributions
- **Create Issue First**: Discuss approach before implementing large features
- **Draft PR Early**: Get feedback on direction before completion
- **Incremental Approach**: Break large features into smaller PRs

## Final Notes

Contributing to this template helps improve development workflows for developers worldwide. Every contribution, whether code, documentation, or testing, makes a difference.

### Template Goals
- Provide useful debugging and development tools
- Maintain fast execution times and reliability
- Support multiple platforms and languages
- Keep documentation clear and practical

### What We Value
- Clear, well-tested code
- Comprehensive documentation
- Cross-platform compatibility
- Performance optimization
- Security best practices

---

## Additional Resources

### Template Documentation
- [Setup Guide](docs/quick-start.md)
- [Architecture Overview](docs/architecture.md)
- [Security Policy](SECURITY.md)

### Jerry's Original Work
- [MetVanDAMN Repository](https://github.com/jmeyer1980/TWG-MetVanDamn)
- [Original Console Commentary](docs/console-commentary-original.md)

### External Resources
- [Python Style Guide (PEP 8)](https://www.python.org/dev/peps/pep-0008/)
- [GitHub Flow](https://guides.github.com/introduction/flow/)
- [Conventional Commits](https://www.conventionalcommits.org/)

---

**Thank you for contributing to the Living Dev Agent Template!**

*Last Updated: 2025-01-15*  
*Next Review: 2025-04-15*
