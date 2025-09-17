#!/bin/bash

# Create missing labels required by Dependabot configuration
# This script creates the labels that Dependabot expects but are missing from the repository

set -e

echo "🔒 Creating missing Dependabot labels..."

# Check if gh CLI is available
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI (gh) is not installed. Please install it first:"
    echo "   https://cli.github.com/"
    exit 1
fi

# Check if authenticated
if ! gh auth status &> /dev/null; then
    echo "❌ Not authenticated with GitHub CLI. Please run: gh auth login"
    exit 1
fi

# Create the missing labels
echo "Creating label: dependencies"
gh label create "dependencies" --description "Pull requests that update dependencies" --color "0366d6" || echo "⚠️  Label 'dependencies' may already exist"

echo "Creating label: github-actions"  
gh label create "github-actions" --description "Pull requests that update GitHub Actions" --color "2188ff" || echo "⚠️  Label 'github-actions' may already exist"

echo "Creating label: security"
gh label create "security" --description "Pull requests that address security issues" --color "d73a49" || echo "⚠️  Label 'security' may already exist"

echo "✅ Done! Dependabot labels have been created."
echo ""
echo "The following labels are now available for Dependabot:"
echo "  🔵 dependencies (blue) - Pull requests that update dependencies"
echo "  🔵 github-actions (blue) - Pull requests that update GitHub Actions"  
echo "  🔴 security (red) - Pull requests that address security issues"
echo ""
echo "Dependabot should now be able to create pull requests with proper labels."