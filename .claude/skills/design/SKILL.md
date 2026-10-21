---
name: design
description: Settle an open design question with 2-3 blind lanes - the same neutral brief to independent Plan agents, convergence by the caller, a supervisor check, then the user's choice. Read-only. Use when a system or feature has several defensible solutions (data format, sync model, ownership, algorithm) before planning or writing its doc.
argument-hint: <the design question>
---

# /design $ARGUMENTS

Read-only: no edits, no Editor. The output is a decision the user approved, ready for `/task`,
`/lead` or `/write-doc`. One model's first answer is a coin toss that then argues for itself; blind
lanes give the user real alternatives.

## 1. Frame (you are the lead)

1. Read the project card in `CLAUDE.md`, the lookup row for the system, that doc's first 20 lines,
   and the entry files it names (grep to the symbol).
2. Write down your own hypothesis in one line **for yourself only**. It never goes into a brief.
3. Write one **neutral brief**: the problem, hard constraints (rules, invariants from the doc, the
   project card, platform limits), what is out of scope, and what each lane must return. No
   preferred option, no "we are thinking of", no A/B menu, no adjectives that lean.

Each lane returns: approach in 5-10 lines; the contracts it adds or changes (interfaces, data,
enums, assets); what it costs (files, runtime, bandwidth, memory); how it fails; what the test
asserts; the assumptions it made; the questions it would put to the user.

## 2. Lanes

Spawn 2 lanes (3 when the question is foundational) as `Plan` agents **in one message**, each with
the identical brief and "Do not ask the user; state assumptions instead." Lanes never see each
other or your hypothesis.

## 3. Converge

Lay the lanes side by side: where they agree, where they differ, and why. Then:

- Agreement on a point is evidence, not proof: check it against the doc.
- On a real disagreement that matters, run one more round: send each lane the other's position on
  that point only (SendMessage to the same agent) and ask for its strongest counter or a concession.
  You decide what each lane learns; never pool them into one conversation.
- Compare with your hypothesis last. If it lost, say so; if no lane considered it, add it as an
  option with the same fields.

## 4. Supervisor

Spawn the `supervisor` agent with checkpoint "design converged", the brief, every lane's answer and
your convergence. Answer each question it returns in one line, changing the proposal where the
answer is "no".

## 5. Present and stop

```
## Design: <question>
Recommended: <option> — <why, 2-3 lines>
Alternatives: <option — why not, one line each>
Dissent kept: <objections still standing, or "none">
Contracts: <what gets added or changed>
Decisions for you: <open questions only the user can settle>
```

Ask with `AskUserQuestion` (recommended option first). The approved design becomes the plan input
of `/task` or `/lead`; a new system gets its doc through `/write-doc`.
