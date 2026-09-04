# 0015 — Deterministic job-skill quality analysis

**Status:** Accepted
**Date:** 2026-09-04

## Context

Job descriptions need quality feedback about the relationship between selected
skills and recorded tasks. The project does not currently have an approved
external AI service, semantic model, provider, budget, or data-sharing policy
for this purpose.

## Decision

The initial quality analysis uses an explicit catalog relationship. Each catalog
task may declare required skills. For a person's selected catalog tasks, the
system compares the union of required skills with the person's selected skills.

It reports missing required skills, selected skills without supporting catalog
tasks, uncatalogued task/skill values, and other explicitly defined structural
findings as review suggestions with source evidence. A free-text task or
description without a catalog mapping is marked as needing review; it is not
treated as a definitive semantic mismatch.

The analyzer never automatically edits a description, changes a catalog value,
changes approval status, or publishes a record. No external AI service is
required or assumed for the initial implementation.

## Rationale

Explicit relationships make the result explainable, testable, permission-safe,
and independent of an unapproved external service. They also allow the catalog
to improve over time without silently rewriting personnel records.

## Consequences

- The task catalog needs a required-skills relationship before definitive
  task/skill comparisons can be reported.
- Findings must identify the affected person, task or skill, and catalog/source
  evidence.
- Semantic analysis of free text remains a separate future decision requiring
  explicit approval of provider, security, cost, and data handling.
