# Implementation Plan: Feature 006 - Expanded SharePoint Retrieval Filters

## Overview

Extend the existing immutable filter with explicit property factories and logical composition. Preserve the raw string integration boundary and avoid a public generic KQL model.

## Architecture Decisions

- Preserve the existing public type and add static factories only.
- Track the private composition kind so repeated operators can flatten without parsing generated KQL.
- Use `DateTimeOffset` and canonical UTC output for temporal filters.
- Reject unsupported structural characters in text values rather than guessing at KQL escaping behavior.
- Keep `.Expression` explicit and retain raw advanced KQL through `FilterExpression`.

## Task List

1. Add failing contract and behavior tests for the expanded surface.
2. Generalize private composition and add `AllOf` and `Not`.
3. Add typed factories for document, people, label, and date properties.
4. Prove unchanged request serialization and update public guidance.
5. Run focused tests, full solution tests, Release build, and diff hygiene.

## Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Mixed operators change precedence | Parenthesize compositions and test nested `AND`, `OR`, and `NOT` |
| Text values alter KQL structure | Reject structural characters and keep raw KQL as an explicit advanced path |
| Date values depend on local culture or time zone | Convert to UTC and format with invariant culture |
| Existing consumers depend on exact output | Preserve and rerun Feature 005 tests |
| The helper grows into a general KQL DSL | Support only Retrieval API properties and documented operators |

## Verification

```sh
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --solution Acterion.Agents.AI.slnx --configuration Release
```

If the `dotnet test` Microsoft Testing Platform adapter reports zero tests, execute each generated xUnit v3 test assembly directly and record that runner compatibility issue separately from test results.