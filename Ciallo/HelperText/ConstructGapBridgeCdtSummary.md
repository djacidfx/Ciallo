# Gap Bridge Business Rules

This document records which visual gaps Gap Bridge should consider.

## Source Ends

- Gap Bridge starts only from original stroke endpoints.
- A source endpoint must be dangling.
- A source endpoint must have at least the user-selected maximum gap length of visible dangling length before it reaches a junction.
- Short dangling endpoints do not start a bridge.
- Closed-shape endpoints do not start a bridge.

## Targets

- A bridge target must be within the user-selected maximum gap length.
- A bridge target may be another dangling endpoint.
- A bridge target may be the body of another stroke.
- A short dangling endpoint may be a target when reached from a valid long dangling source endpoint.
- Closed shapes may be body targets.

## Same-Stroke Gaps

- Same-stroke body targets are ignored.
- A same-stroke gap may target only the other original endpoint of the same stroke.
- The other same-stroke endpoint must be dangling.
- The other same-stroke endpoint does not need to satisfy the source dangling-length rule.

## Candidate Selection

- Each valid source endpoint keeps only its nearest valid target.
- Candidates are directional: source-to-target direction matters.
- Multiple source endpoints may target the same point.
- There is no score-based ranking.
