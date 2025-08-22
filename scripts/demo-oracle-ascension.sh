#!/bin/bash
# Oracle Ascension Demonstration Script
# Shows the complete end-to-end workflow from Intel to Archive

echo "🔮 Oracle Ascension Demonstration"
echo "=================================="
echo

echo "📊 Initial State:"
node scripts/cid-faculty/vision-queue.js status
echo

echo "🧙‍♂️ Running Advisor (generates high-priority items for Oracle)..."
node scripts/cid-faculty/advisor.js --context=out/cid/context.json --out=out/cid/demo-advisor.json
echo

echo "🔍 Simulating System Pattern Detection..."
node scripts/cid-faculty/system-pattern-detector.js test-failure critical-module 4
echo

echo "📋 Current Vision Queue Status:"
node scripts/cid-faculty/vision-queue.js list | head -20
echo

echo "🔮 Running Oracle Vision Ritual (processes 2 visions)..."
node scripts/cid-faculty/oracle.js ritual --max-visions=2 --out=out/cid/demo-ritual.json
echo

echo "📚 Checking Archive:"
echo "Vision Reports Created:"
ls -la docs/oracle_visions/2025/08/ | tail -5
echo

echo "📊 Final Queue Status:"
node scripts/cid-faculty/vision-queue.js status
echo

echo "🎯 Cross-Reference Index:"
echo "Total Visions Archived: $(cat docs/oracle_visions/index.json | jq '.metadata.totalVisions')"
echo "Latest Vision: $(cat docs/oracle_visions/index.json | jq -r '.visions[-1].id')"
echo

echo "✅ Oracle Ascension Complete!"
echo "   - Intel flows from Advisor and System Patterns"
echo "   - Visions are queued with priority and context"
echo "   - Oracle Ritual processes visions systematically"
echo "   - All visions archived with full lineage tracking"
echo "   - Cross-links maintained for future reference"