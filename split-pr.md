---
name: split-pr
description: Analyzes current changes and suggests how to split them into smaller, reviewable PRs
---

# Split Large PR into Smaller Changes

## Purpose

Help developers break down large changesets into logical, reviewable pull requests. This skill analyzes the current diff and proposes a splitting strategy that keeps changes atomic and reviewable.

## Instructions

### 1. Analyze Current Changes

Run these commands to understand the scope:

```bash
# Get detailed file statistics
git diff main...HEAD --stat

# List all changed files
git diff main...HEAD --name-only

# Show commit history for context
git log main...HEAD --oneline

# Count non-generated files changed
git diff main...HEAD --name-only | grep -v 'vendor/' | grep -v '\.pb\.go$' | grep -v 'zz_generated' | grep -v '^docs/' | wc -l

# Count lines changed (excluding generated code)
git diff main...HEAD --stat -- . ':(exclude)vendor/*' ':(exclude)*.pb.go' ':(exclude)zz_generated*' ':(exclude)docs/*' | tail -1
```

### 2. Evaluate Size and Complexity

Assess whether the changes exceed recommended limits:

- **Target limits per PR**:
  - < 10 files changed (excluding tests, generated code, docs)
  - < 400 lines of code changed (excluding tests, generated code, docs)
  - Changes represent one logical unit of work

If changes exceed these limits or mix multiple concerns, proceed to split analysis.

### 3. Identify Logical Groupings

Examine the changed files and identify natural boundaries:

- **By component/package**: Group changes by the package or component they affect
- **By layer**: Separate model changes, business logic, API changes, CLI changes
- **By concern**: Separate refactoring from new features, bug fixes from enhancements
- **By dependency**: Identify which changes depend on others

Use these commands to help:

```bash
# Group changed files by directory
git diff main...HEAD --name-only | grep -v 'vendor/' | grep -v '\.pb\.go$' | cut -d'/' -f1-2 | sort | uniq -c

# Show changes by package
git diff main...HEAD --name-only | grep '\.go$' | grep -v '_test\.go$' | cut -d'/' -f1-3 | sort | uniq -c
```

### 4. Propose Split Strategy

Create a structured plan with multiple PRs:

For each proposed PR, specify:
- **PR Name**: Brief description (e.g., "Add base container interface")
- **Purpose**: What this PR accomplishes and why it's needed
- **Files included**: List of files that would be in this PR
- **Estimated size**: Approximate lines changed
- **Dependencies**: Which other proposed PRs this depends on (if any)
- **Test coverage**: What tests are included
- **Order**: Suggest the sequence for creating PRs (e.g., "Create this first")

### 5. Recommend Creation Order

Determine the optimal order for creating PRs:

1. **Foundation PRs first**: New interfaces, base types, shared utilities
2. **Refactoring PRs second**: Changes that use the new foundation
3. **Feature PRs last**: New functionality that builds on the foundation
4. **Independent PRs anytime**: Changes that don't depend on others

### 6. Present Action Plan

Provide a clear, actionable plan:

```markdown
## Proposed PR Split

### Summary
Currently [X] files changed with [Y] lines modified. Recommend splitting into [N] PRs:

### PR 1: [Name] (Create First)
**Purpose**: [What and why]
**Files**:
- path/to/file1.go
- path/to/file2.go
**Size**: ~100 LOC
**Dependencies**: None
**Tests**: Includes unit tests for new functionality

### PR 2: [Name] (After PR 1)
**Purpose**: [What and why]
**Files**:
- path/to/file3.go
**Size**: ~150 LOC
**Dependencies**: Requires PR 1 (uses new interface)
**Tests**: Integration tests

[... continue for each PR ...]

## Next Steps
1. Would you like me to help create PR 1 first?
2. Should I create a tracking issue for the overall work?
3. Any changes to this split strategy?
```

## Best Practices

### Splitting Principles

- **Each PR should pass tests independently**: Don't create PRs that break builds
- **Prefer multiple small PRs over one large PR**: Easier to review and revert
- **Keep related changes together**: Don't artificially split code that changes together
- **Foundation before features**: Establish abstractions before using them
- **Use feature flags for incomplete work**: If a feature spans multiple PRs

### Common Split Patterns

1. **Refactoring + Feature**:
   - PR 1: Extract interface and refactor existing code
   - PR 2: Add new feature using the interface

2. **Multi-layer Feature**:
   - PR 1: Add data models and database changes
   - PR 2: Add business logic layer
   - PR 3: Add API endpoints
   - PR 4: Add CLI commands

3. **Package Restructuring**:
   - PR 1: Create new package structure (empty or minimal)
   - PR 2: Move code to new structure
   - PR 3: Update imports and references
   - PR 4: Clean up old structure

4. **Kubernetes Operator Changes**:
   - PR 1: Update CRD definitions and generate code
   - PR 2: Update controller logic
   - PR 3: Add validation and defaulting
   - PR 4: Update documentation and examples

### What NOT to Split

- **Atomic refactorings**: Renaming that touches many files but is one logical change
- **Generated code updates**: Proto, CRD, mock updates should stay together
- **Dependency updates**: Keep go.mod and vendor changes in one PR
- **Tightly coupled changes**: Changes that don't make sense independently

## Examples

### Example 1: Adding New CLI Command

**Current state**: 8 files changed, 450 lines

**Split strategy**:
- PR 1: Add business logic to `pkg/` package (3 files, 200 lines)
- PR 2: Add CLI command and E2E tests (5 files, 250 lines)

**Rationale**: Business logic is independently testable and reusable

### Example 2: Refactoring + Feature

**Current state**: 15 files changed, 800 lines

**Split strategy**:
- PR 1: Extract common interface (2 files, 100 lines)
- PR 2: Refactor existing implementations to use interface (6 files, 300 lines)
- PR 3: Add new implementation with feature (7 files, 400 lines)

**Rationale**: Each PR is independently valuable and testable

### Example 3: Operator Enhancement

**Current state**: 12 files changed, 600 lines

**Split strategy**:
- PR 1: Update CRD with new fields and generate code (4 files, 150 lines, mostly generated)
- PR 2: Update controller to handle new fields (5 files, 300 lines)
- PR 3: Add validation webhook (3 files, 150 lines)

**Rationale**: Each PR represents a complete vertical slice of functionality

## User Interaction

After presenting the split strategy:

1. **Ask for feedback**: "Does this split make sense for your workflow?"
2. **Offer to adjust**: Be flexible based on user's preferences
3. **Help with first PR**: "Would you like me to help create PR 1?"
4. **Create tracking**: "Should I create a GitHub issue to track all PRs?"

## Notes

- **Be pragmatic**: The goal is reviewable PRs, not arbitrary rules
- **Consider the team**: Some teams prefer different split strategies
- **Document dependencies**: Make it clear which PRs block others
- **Test independently**: Each PR should pass CI/CD checks
