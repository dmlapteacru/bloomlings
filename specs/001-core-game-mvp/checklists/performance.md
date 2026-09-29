# Performance on reference low-end devices (T150)

**Feature**: 001-core-game-mvp · **Research**: R16 · **Requirements**: SC-008, SC-009 · **Date**: 2026-09-29

Status: **open**. These measurements need the Unity Editor profiler and the physical reference devices. This file lists
the targets, what is already in place, and the table to fill in.

## Reference devices (R16)

- Android: a device with about 3 GB of RAM and a Mali-G52-class GPU (Galaxy A1x class).
- iOS: iPhone SE (2nd generation).

## What is already in place

- [x] Walkers come from a bounded pool (`WorkerPool`, 60 active on low-end devices), and a saturated wave merges walkers
  (T046, R4).
- [x] When the pending animation time exceeds `fx.backlogThresholdMs` (Remote Config, default 1500 ms), playback
  speeds up to 4× and walkers are merged (T045, R4).
- [x] Input never waits for the timeline: taps apply to the logical state at once (FR-016, SC-008).
- [x] Content packs are parsed off the main thread at boot (`BundledContentLoader`, `Task.Run`).
- [ ] Sprite atlases: all art is procedural and cached per shape (`ProceduralSprites`). Pack the final art into atlases
  when it arrives.

## Measurements (open)

Profile a development build with the Unity Profiler, then confirm on a release build.

| Metric | Target | Android reference | iPhone SE 2 | Notes |
|---|---|---|---|---|
| Frame rate on the largest board (16×20, 6 variants, full backlog) | 30 fps floor | | | |
| Hitches | none over 100 ms | | | |
| Tap feedback | ≤ 0.1 s | | | |
| Level load | ≤ 1 s | | | |
| Cold start to Home | ≤ 5 s | | | |
| Memory | ≤ 350 MB | | | |
| Install size | ≤ 150 MB | | | |

## Tuning to try if a target is missed

- Lower `WorkerCapacity` on the device class (for example to 40).
- Lower `fx.backlogThresholdMs` so compression starts earlier.
- Pre-render the finished picture once per level (it is already a single texture).
