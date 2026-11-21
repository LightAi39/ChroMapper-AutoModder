using BLMapCheck.BeatmapScanner.MapCheck;
using BLMapCheck.Classes.ChroMapper;
using BLMapCheck.Classes.Helper;
using BLMapCheck.Classes.Results;
using BLMapCheck.Classes.Unity;
using Parser.Map.Difficulty.V3.Base;
using Parser.Map.Difficulty.V3.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using static BLMapCheck.Classes.Helper.Helper;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Difficulty
{
    public class FakeVB
    {
        public BeatmapGridObject Note { get; set; }
        public FakeVB(int x, int y, float beat) 
        {
            Note = new()
            {
                x = x,
                y = y,
                Beats = beat
            };
        }
    }

    internal static class VisionBlock
    {
        // Detect notes and bombs VB based on BeatLeader current criteria
        public static CritResult Check(CheckContext ctx, List<BeatmapGridObject> beatmapGridObject, List<Chain> chains, List<SwingInfo> swings, double pass, double tech, float noteJumpStartBeatOffset)
        {
            CritResult criteria = CritResult.Success;

            // Create a new list to not mess up with the original reference
            var beatmapGridObjects = beatmapGridObject.ToList();

            if (beatmapGridObjects.Any())
            {
                // Try to find VB links
                foreach (var chain in chains)
                {
                    var x = Math.Abs(chain.tx - chain.x) * chain.Squish;
                    var y = Math.Abs(chain.ty - chain.y) * chain.Squish;
                    var distance = Math.Sqrt(x * x + y * y);
                    double value = 1;
                    if(chain.SliceCount >= 2) value = distance / (chain.SliceCount - 1);
                    // Difference between expected and current distance, multiplied by current squish to know maximum value
                    double max;
                    if (chain.ty == chain.y) max = Math.Round(ctx.Config.ChainLinkVsAir / 3 / value * chain.Squish, 2);
                    else max = Math.Round(ctx.Config.ChainLinkVsAir / 3 * 1.1 / value * chain.Squish, 2);
                    if (chain.Squish - 0.01 <= max) // << play with this value to change the limit
                    {
                        List<Vector3> a = new();
                        var pos = FindChainLinksPosition(chain);
                        foreach (var p in pos)
                        {
                            x = 1f - p.x;
                            y = 1f - p.y;
                            distance = Math.Sqrt(x * x + y * y);
                            var time = (chain.TailInBeats - chain.Beats) * (pos.FindIndex(x => x.x == p.x && x.y == p.y) / ((float)pos.Count - 1)) + chain.Beats;
                            if (distance < 0.35)
                            {
                                FakeVB fakeVB = new(1, 1, time);
                                beatmapGridObjects.Add(fakeVB.Note);
                            }
                            x = 2f - p.x;
                            y = 1f - p.y;
                            distance = Math.Sqrt(x * x + y * y);
                            if (distance < 0.35)
                            {
                                FakeVB fakeVB = new(2, 1, time);
                                beatmapGridObjects.Add(fakeVB.Note);
                            }
                        }
                    }
                }

                beatmapGridObjects = beatmapGridObjects.OrderBy(x => x.Beats).ToList();

                // Notes
                var hidden = new List<(Note Note, float Gap, float MinTime)>();
                List<BeatmapGridObject> lastMidL = new();
                List<BeatmapGridObject> lastMidR = new();

                for (var i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var note = beatmapGridObjects[i];

                    // Reaction Time need to be recalculated due to VNJS
                    var reactionTime = ReactionTime(ctx, note.njs, noteJumpStartBeatOffset);

                    // Calculate thresholds according to BPM
                    var MaxBottomNoteTime = ctx.SecondsToBeats(ctx.Config.VBMinBottomNoteTime, note.Beats);
                    var MaxOuterNoteTime = ctx.SecondsToBeats(ctx.Config.VBMaxOuterNoteTime, note.Beats);
                    var Overall = ctx.SecondsToBeats(ctx.Config.VBMinimum, note.Beats);
                    var MinTimeWarning = ctx.SecondsToBeats(((750 - 300) * Math.Pow(Math.E, -pass / 7.6 - tech * 0.06) + 325) / 1000, note.Beats);
                    if (ctx.Config.UseMapRT) MinTimeWarning = ctx.SecondsToBeats(reactionTime / 1000, note.Beats);

                    // Clean up out-of-range VB notes
                    lastMidL.RemoveAll(l => note.Beats - l.Beats > MinTimeWarning);
                    lastMidR.RemoveAll(l => note.Beats - l.Beats > MinTimeWarning);

                    if (lastMidL.Count > 0)
                    {
                        if (note.Beats - lastMidL.First().Beats >= Overall) // Further than 0.025
                        {
                            if (IsTooClose(ctx, note.Beats - lastMidL.First().Beats, MinTimeWarning, note.Beats)) // Warning
                            {
                                if (note.x == 0 && note.Beats - lastMidL.First().Beats <= MaxOuterNoteTime) // Closer than 0.15 in outer lane
                                {
                                    // Fine
                                }
                                else if (note.x == 1 && note.y == 0 && note.Beats - lastMidL.First().Beats <= MaxBottomNoteTime) // Closer than 0.075 at bottom layer
                                {
                                    // Also fine
                                }
                                else if (note.x < 2) // Possible VB
                                {
                                    if (note is Note n) hidden.Add((n, note.Beats - lastMidL.First().Beats, MinTimeWarning));
                                }
                            }
                        }
                    }
                    if (lastMidR.Count > 0)
                    {
                        if (note.Beats - lastMidR.First().Beats >= Overall)
                        {
                            if (IsTooClose(ctx, note.Beats - lastMidR.First().Beats, MinTimeWarning, note.Beats))
                            {
                                if (note.x == 3 && note.Beats - lastMidR.First().Beats <= MaxOuterNoteTime)
                                {
                                    // Fine
                                }
                                else if (note.x == 2 && note.y == 0 && note.Beats - lastMidR.First().Beats <= MaxBottomNoteTime)
                                {
                                    // Also fine
                                }
                                else if (note.x > 1) // Possible VB
                                {
                                    if (note is Note n && !hidden.Any(h => h.Note == n)) hidden.Add((n, note.Beats - lastMidR.First().Beats, MinTimeWarning));
                                }
                            }
                        }
                    }

                    // Store new VB note
                    if (note.y == 1 && note.x == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.y == 1 && note.x == 2)
                    {
                        lastMidR.Add(note);
                    }
                }

                // A note of a multi-note swing is visible enough if the head of the swing can be seen
                var heads = new Dictionary<Note, Note>();
                foreach (var swingNotes in Slider.MultiNoteSwings(swings))
                {
                    foreach (var note in swingNotes) heads.TryAdd(note, swingNotes[0]);
                }
                var hiddenNotes = new HashSet<Note>(hidden.Select(h => h.Note));
                foreach (var (note, gap, minTime) in hidden)
                {
                    if (heads.TryGetValue(note, out var head) && head != note && !hiddenNotes.Contains(head)) continue;
                    ctx.Results.CreateDiffResult("Vision Block", Severity.Warning, "Vision", "Notes must be placed with enough time to react", new() {
                            new("currentReactionTime", Math.Round(ctx.BeatsToSeconds(gap, note.Beats) * 1000, 0).ToString()),
                            new("targetReactionTime", Math.Round(ctx.BeatsToSeconds(minTime, note.Beats) * 1000, 0).ToString()),
                        }, new() { note });
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                }

                // Bombs
                lastMidL = new List<BeatmapGridObject>();
                lastMidR = new List<BeatmapGridObject>();
                // Last note of each hand before the current bomb, to know the swing direction
                Note left = null;
                Note right = null;
                var noteIndex = 0;
                // Every bomb, with the gap to what hides it if it is vision blocked, and whether it forces a movement
                var bombs = new List<(BeatmapGridObject Bomb, float? HiddenGap, float MinTime, bool ForcesMovement)>();
                for (var i = 0; i < beatmapGridObjects.Count; i++)
                {
                    var bomb = beatmapGridObjects[i];
                    if (bomb is Bomb)
                    {
                        while (noteIndex < beatmapGridObjects.Count && beatmapGridObjects[noteIndex].Beats < bomb.Beats)
                        {
                            if (beatmapGridObjects[noteIndex] is Note no)
                            {
                                if (no.Color == 0) left = no;
                                else if (no.Color == 1) right = no;
                            }
                            noteIndex++;
                        }

                        // Reaction Time need to be recalculated due to VNJS
                        var reactionTime = ReactionTime(ctx, bomb.njs, noteJumpStartBeatOffset);

                        // Calculate thresholds according to BPM
                        var MaxTimeBomb = ctx.SecondsToBeats(ctx.Config.VBMaxBombTime, bomb.Beats);
                        var MinTimeBomb = ctx.SecondsToBeats(ctx.Config.VBMinBombTime, bomb.Beats);
                        if (ctx.Config.UseMapRT) MinTimeBomb = ctx.SecondsToBeats(reactionTime / 1000, bomb.Beats);
                        var Overall = ctx.SecondsToBeats(ctx.Config.VBMinimum, bomb.Beats);

                        // Clean up out-of-range VB notes
                        lastMidL.RemoveAll(l => bomb.Beats - l.Beats > MinTimeBomb);
                        lastMidR.RemoveAll(l => bomb.Beats - l.Beats > MinTimeBomb);

                        float? hiddenGap = null;
                        if (lastMidL.Count > 0)
                        {
                            var gap = bomb.Beats - lastMidL.First().Beats;
                            if (IsTooClose(ctx, gap, MinTimeBomb, bomb.Beats)) // Closer than 0.20
                            {
                                if (bomb.x == 0 && gap <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((bomb.x != 1 || bomb.y != 1) && gap <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (bomb.x < 2) // Possible VB
                                {
                                    hiddenGap = gap;
                                }
                            }
                        }
                        if (lastMidR.Count > 0 && hiddenGap == null)
                        {
                            var gap = bomb.Beats - lastMidR.First().Beats;
                            if (IsTooClose(ctx, gap, MinTimeBomb, bomb.Beats)) // Closer than 0.20
                            {
                                if (bomb.x == 3 && gap <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((bomb.x != 2 || bomb.y != 1) && gap <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (bomb.x > 1) // Possible VB
                                {
                                    hiddenGap = gap;
                                }
                            }
                        }
                        // Bombs that don't force the player to move their sabers or body are exempt
                        bombs.Add((bomb, hiddenGap, MinTimeBomb, hiddenGap != null && (IsInSwingPath(bomb, left) || IsInSwingPath(bomb, right))));
                    }

                    // Store new VB bomb
                    if (bomb.y == 1 && bomb.x == 1)
                    {
                        lastMidL.Add(bomb);
                    }
                    if (bomb.y == 1 && bomb.x == 2)
                    {
                        lastMidR.Add(bomb);
                    }
                }

                // Bombs part of a cluster partially visible with enough context to tell it's a bomb cluster are exempt
                var visibleInCluster = BombClusters(ctx, bombs.Select(b => b.Bomb).ToList(), bombs.Select(b => b.HiddenGap == null).ToList());
                for (int i = 0; i < bombs.Count; i++)
                {
                    var (bomb, hiddenGap, minTime, forcesMovement) = bombs[i];
                    if (hiddenGap == null || !forcesMovement || visibleInCluster[i] >= VisibleClusterContext) continue;
                    ReportBomb(ctx, bomb, hiddenGap.Value, minTime);
                    if (CritResult.Warning > criteria) criteria = CritResult.Warning;
                }
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateDiffResult("Vision Block", Severity.Passed, "Vision", "No issue with vision from notes and bombs detected");
            }

            return criteria;
        }

        // Visible bombs needed in a cluster to tell it's a bomb cluster
        private const int VisibleClusterContext = 2;
        // Bombs of a cluster touch each other: in adjacent cells, at most this many seconds apart
        private const double ClusterSeconds = 0.1;

        // Number of visible bombs in the cluster of each bomb
        private static int[] BombClusters(CheckContext ctx, List<BeatmapGridObject> bombs, List<bool> visible)
        {
            var parent = Enumerable.Range(0, bombs.Count).ToArray();
            int Find(int i) => parent[i] == i ? i : parent[i] = Find(parent[i]);
            var seconds = bombs.Select(b => ctx.Timescale.BPM.ToRealTime(b.Beats)).ToList();
            for (int i = 0; i < bombs.Count; i++)
            {
                for (int j = i + 1; j < bombs.Count && seconds[j] - seconds[i] <= ClusterSeconds + 0.0001; j++)
                {
                    if (Math.Abs(bombs[i].x - bombs[j].x) <= 1 && Math.Abs(bombs[i].y - bombs[j].y) <= 1) parent[Find(i)] = Find(j);
                }
            }
            var counts = new Dictionary<int, int>();
            for (int i = 0; i < bombs.Count; i++) if (visible[i]) counts[Find(i)] = counts.TryGetValue(Find(i), out var c) ? c + 1 : 1;
            return Enumerable.Range(0, bombs.Count).Select(i => counts.TryGetValue(Find(i), out var c) ? c : 0).ToArray();
        }

        // Check if the bomb is around where the previous swing of that hand ends
        private static bool IsInSwingPath(BeatmapGridObject bomb, Note hand)
        {
            if (hand == null) return false;

            if (hand.CutDirection == 8)
            {
                // Dots are only considered in the middle of the grid
                if (hand.y != 1 || (hand.x != 1 && hand.x != 2)) return false;
                return Math.Sqrt(Math.Pow(bomb.x - hand.x, 2) + Math.Pow(bomb.y - hand.y, 2)) < 1.001;
            }

            var pos = (hand.x, hand.y);
            int index = 1;
            while (!NoteDirection.IsLimit(pos, hand.CutDirection))
            {
                pos = NoteDirection.Move(hand, index);
                index++;
            }
            return Math.Sqrt(Math.Pow(bomb.x - pos.x, 2) + Math.Pow(bomb.y - pos.y, 2)) < 1.001;
        }

        // Less than the target reaction time. Compared in whole ms like the reported values, so an object exactly at the limit is fine.
        private static bool IsTooClose(CheckContext ctx, float gap, float limit, float beat)
        {
            return Math.Round(ctx.BeatsToSeconds(gap, beat) * 1000) < Math.Round(ctx.BeatsToSeconds(limit, beat) * 1000);
        }

        // Reaction time in ms. The game sets the jump duration from the song BPM, BPM changes don't affect it.
        private static double ReactionTime(CheckContext ctx, float njs, float noteJumpStartBeatOffset)
        {
            var bpm = ctx.Map.Info._beatsPerMinute;
            var halfJumpDuration = SpawnParameterHelper.CalculateHalfJumpDuration(njs, noteJumpStartBeatOffset, bpm);
            return 60000 / bpm * halfJumpDuration;
        }

        private static void ReportBomb(CheckContext ctx, BeatmapGridObject bomb, float gap, float minTimeBomb)
        {
            // Like notes, a vision block needs to be evaluated and may require justification
            ctx.Results.CreateDiffResult("Vision Block", Severity.Warning, "Vision", "Bombs must be placed to give the player acceptable time to react", new() {
                new("currentReactionTime", Math.Round(ctx.BeatsToSeconds(gap, bomb.Beats) * 1000, 0).ToString()),
                new("targetReactionTime", Math.Round(ctx.BeatsToSeconds(minTimeBomb, bomb.Beats) * 1000, 0).ToString()),
            }, new() { bomb });
        }
    }
}
