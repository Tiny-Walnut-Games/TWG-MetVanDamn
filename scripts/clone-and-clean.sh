#!/usr/bin/env bash
#
# Clone and Clean Script for Living Dev Agent Template
#
# Copyright (C) 2025 Bellok
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program.  If not, see <https://www.gnu.org/licenses/>.
#
set -e

TEMPLATE_DIR="living-dev-agent-template"
TARGET_DIR="$1"

if [[ -z "$TARGET_DIR" ]]; then
  echo "Usage: $0 <new-repo-path>"
  echo ""
  echo "This script clones the Living Dev Agent template and sets up a fresh repository."
  echo ""
  echo "Example:"
  echo "  $0 ../my-new-project"
  echo "  $0 /home/user/projects/awesome-agent"
  exit 1
fi

echo "🚀 Living Dev Agent Template Clone & Clean"
echo "==========================================="
echo ""

# Check if target directory already exists
if [[ -d "$TARGET_DIR" ]]; then
  echo "❌ Error: Target directory '$TARGET_DIR' already exists."
  echo "   Please choose a different path or remove the existing directory."
  exit 1
fi

# Get the current script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATE_SOURCE="$(dirname "$SCRIPT_DIR")"

echo "📂 Source template: $TEMPLATE_SOURCE"
echo "📁 Target directory: $TARGET_DIR"
echo ""

# Create target directory and copy template
echo "📋 Copying template structure..."
mkdir -p "$TARGET_DIR"
cp -r "$TEMPLATE_SOURCE"/* "$TARGET_DIR/"

# Remove any Git history if it exists
if [[ -d "$TARGET_DIR/.git" ]]; then
  echo "🧹 Removing existing Git history..."
  rm -rf "$TARGET_DIR/.git"
fi

# Navigate to target directory
cd "$TARGET_DIR"

echo "🔧 Customizing template for new project..."

# Update project-specific configuration files
if [[ -f "docs/devtimetravel_snapshot.yaml" ]]; then
  # Update project name in DevTimeTravel config
  PROJECT_NAME=$(basename "$TARGET_DIR")
  sed -i.bak "s/Living Dev Agent Project/$PROJECT_NAME/g" docs/devtimetravel_snapshot.yaml
  sed -i.bak "s/username\/repo-name/username\/$PROJECT_NAME/g" docs/devtimetravel_snapshot.yaml
  rm docs/devtimetravel_snapshot.yaml.bak 2>/dev/null || true
  echo "   ✅ Updated DevTimeTravel configuration"
fi

# Update README.md placeholders
if [[ -f "README.md" ]]; then
  PROJECT_NAME=$(basename "$TARGET_DIR")
  sed -i.bak "s/your-username\/your-new-repo/your-username\/$PROJECT_NAME/g" README.md
  rm README.md.bak 2>/dev/null || true
  echo "   ✅ Updated README.md"
fi

# Make scripts executable
echo "🔨 Setting up executable scripts..."
find scripts/ -name "*.sh" -exec chmod +x {} \; 2>/dev/null || true
find scripts/ -name "*.py" -exec chmod +x {} \; 2>/dev/null || true
find src/ -name "*.py" -exec chmod +x {} \; 2>/dev/null || true

# Create .gitignore if it doesn't exist
if [[ ! -f ".gitignore" ]]; then
  echo "📝 Creating .gitignore..."
  cat > .gitignore << 'EOF'
# Living Dev Agent Template - Generated .gitignore

# DevTimeTravel snapshots
.devtimetravel/
*.snapshot

# Python
__pycache__/
*.py[cod]
*$py.class
*.so
.Python
build/
develop-eggs/
dist/
downloads/
eggs/
.eggs/
lib/
lib64/
parts/
sdist/
var/
wheels/
*.egg-info/
.installed.cfg
*.egg
MANIFEST

# Virtual environments
venv/
env/
ENV/

# IDE
.vscode/
.idea/
*.swp
*.swo
*~

# OS
.DS_Store
Thumbs.db

# Logs
*.log
logs/

# Temporary files
*.tmp
*.temp
.cache/

# Project-specific
node_modules/
.env
.env.local
.env.*.local
EOF
  echo "   ✅ Created .gitignore"
fi

# Initialize Git repository
echo "🗂️  Initializing Git repository..."
git init
git add .
git commit -m "Initial Living Dev Agent template

- Scaffolded from living-dev-agent-template
- Configured TLDL (Living Dev Log) workflow
- Set up DevTimeTravel context capture
- Included ECS system linters and validation tools
- Ready for GitHub Copilot integration

Template version: $(date +%Y-%m-%d)
"

echo ""
echo "🎉 Living Dev Agent template setup complete!"
echo ""
echo "Next steps:"
echo "1. Set up remote repository:"
echo "   git remote add origin https://github.com/your-username/$(basename "$TARGET_DIR").git"
echo ""
echo "2. Customize configuration:"
echo "   - Edit docs/devtimetravel_snapshot.yaml for your project"
echo "   - Review TWG-Copilot-Agent.yaml settings"
echo "   - Update mcp-config.json if using MCP servers"
echo ""
echo "3. Install dependencies:"
echo "   pip install -r scripts/requirements.txt"
echo ""
echo "4. Test the setup:"
echo "   scripts/init_agent_context.sh --dry-run"
echo "   python src/SymbolicLinter/validate_docs.py --tldl-path TLDL/entries/"
echo ""
echo "5. Start development:"
echo "   - Create your first TLDL entry: cp docs/tldl_template.yaml TLDL/entries/TLDL-$(date +%Y-%m-%d)-ProjectStart.md"
echo "   - Enable GitHub Copilot in your IDE"
echo "   - Run linters: python src/SymbolicLinter/ecs_system_linter.py --path src/"
echo ""
echo "📚 Documentation: See docs/Copilot-Setup.md for detailed setup instructions"
echo ""
echo "🛠️  Template created at: $TARGET_DIR"
echo "Happy coding with your Living Dev Agent! 🤖✨"