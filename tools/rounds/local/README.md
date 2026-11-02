# Building a wave on your own computer

The owner's way of working (2026-11-02): about 85–90 % of a wave is built by script on the owner's computer — fast, and
without Claude's time — and Claude builds the hand positions (bridges and other custom mechanics) and checks the rest.

## Once

1. Install **Node.js 22 or newer** (https://nodejs.org). Nothing else is needed for building levels (moving a wave into
   the game needs .NET; Claude does that).
2. Clone the repository and check out the working branch; `git pull` before each wave.

## Each wave

1. Claude prepares the wave's mechanics plan: `content/rounds/runs/L####-L####/mechanics.json` (classes, mechanics shares,
   introductions, breathers, hand positions, pinned pictures).
2. Run (from the repository folder):
   - Windows: `powershell -ExecutionPolicy Bypass -File tools\rounds\local\build-wave.ps1 -From 101 -To 150`
   - Mac / Linux: `tools/rounds/local/build-wave.sh 101 150`
   Options: jobs (parallel builds, default 2; each uses several cores), `--reassign` / `-Reassign` (draw the wave's
   pictures anew), minutes (time limit per position).
3. What it does: picks one approved picture for every position (sizes per wave of 50: 30 × 800–1200, 10 × 700–900,
   5 × 400–700, 5 × 1200–1500, spread evenly, packs alternating), builds each level (queue tuned by short measurements,
   trap links on Hard, hidden pods / cells after the class, breathers on small pictures), runs every bot test of rules
   17 §5, swaps a picture that cannot give its class for another of the same size, places the levels, audits the wave
   and builds the sample pilot. It is resumable: run it again after a stop.
4. Read `content/rounds/runs/L####-L####/summary.md` (open and hand positions at the end) and `audit.md`; play the
   sample in `sample/`.
5. Commit and push `content/rounds` (the script prints the command). Claude then checks the wave, puts the hand levels
   in, and moves the wave into the game.

The same inputs give the same levels on any computer and any number of cores, so Claude can re-check any level exactly.
