/*using BLMapCheck.BeatmapScanner.Data;
using BLMapCheck.BeatmapScanner.Data.Criteria;
using BLMapCheck.BeatmapScanner.MapCheck;
using JoshaParity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;
using Parity = BLMapCheck.BeatmapScanner.MapCheck.Parity;
using Configs = BLMapCheck.Configs.Config;
using BLMapCheck.Classes.ChroMapper;
using BLMapCheck.Classes.Unity;

namespace BLMapCheck.BeatmapScanner
{
    internal class CriteriaCheck
    {
        #region Variables
        public static Configs config = new();
        #endregion

        #region Properties
        //private Plugin plugin;
        private string characteristic;
        private int difficultyRank;
        private string difficulty;
        //private BaseDifficulty baseDifficulty;
        private double songOffset;
        private BeatPerMinute bpm;
        private MapAnalyser analysedMap;
        private List<JoshaParity.SwingData> swings;
        private double averageSliderDuration;
        private (double pass, double tech, double ebpm, double slider, double reset, int crouch, double linear, double sps, string handness) BeatmapScannerData;
        #endregion

        #region Constructors
        public CriteriaCheck()
        {
            //this.plugin = plugin;
        }
        #endregion

        #region Method
        public InfoCrit AutoInfoCheck()
        {
            InfoCrit infoCrit = new()
            {
                SongName = SongNameCheck(),
                SubName = SubNameCheck(),
                SongAuthor = SongAuthorCheck(),
                Creator = CreatorCheck(),
                Offset = OffsetCheck(),
                BPM = BPMCheck(),
                DifficultyOrdering = DifficultyOrderingCheck(),
                Preview = PreviewCheck()
            };

            return infoCrit;
        }

        public DiffCrit AutoDiffCheck(string characteristic, int difficultyRank, string difficulty)
        {
            this.characteristic = characteristic;
            this.difficultyRank = difficultyRank;
            this.difficulty = difficulty;

            *//* TODO: Rewrite this song loading code
            var song = plugin.BeatSaberSongContainer.Info;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            songOffset = BeatSaberSongContainer.Instance.Info.SongTimeOffset;
            bpm = BeatPerMinute.Create(BeatSaberSongContainer.Instance.Info.BeatsPerMinute, baseDifficulty.BpmEvents.Where(x => x.Bpm < 10000 && x.Bpm > 0).ToList(), songOffset);
            analysedMap = new(song.Directory);
            swings = analysedMap.GetSwingData((BeatmapDifficultyRank)difficultyRank, characteristic.ToLower());

            if (baseDifficulty.Notes.Any())
            {
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();

                if (notes.Count > 0)
                {
                    List<BaseSlider> chains = baseDifficulty.Chains.Cast<BaseSlider>().ToList();
                    chains = chains.OrderBy(o => o.JsonTime).ToList();

                    List<BaseNote> bombs = baseDifficulty.Notes.Where(n => n.Type == 3).ToList();
                    bombs = bombs.OrderBy(b => b.JsonTime).ToList();

                    List<BaseObstacle> obstacles = baseDifficulty.Obstacles.ToList();
                    obstacles = obstacles.OrderBy(o => o.JsonTime).ToList();

                    BeatmapScannerData = BeatmapScanner.Analyzer(notes, chains, bombs, obstacles, BeatSaberSongContainer.Instance.Info.BeatsPerMinute);
                }
            }
            *//*

            DiffCrit diffCrit = new()
            {
                HotStart = HotStartCheck(),
                ColdEnd = ColdEndCheck(),
                MinSongDuration = MinSongDurationCheck(),
                Slider = SliderCheck(),
                DifficultyLabelSize = DifficultyLabelSizeCheck(),
                DifficultyName = DifficultyNameCheck(),
                Requirement = RequirementsCheck(),
                NJS = NJSCheck(),
                FusedObject = FusedObjectCheck(),
                Outside = OutsideCheck(),
                Light = LightCheck(),
                Wall = WallCheck(),
                Chain = ChainCheck(),
                Parity = ParityCheck(),
                VisionBlock = VisionBlockCheck(),
                ProlongedSwing = ProlongedSwingCheck(),
                Loloppe = LoloppeCheck(),
                SwingPath = SwingPathCheck(),
                Hitbox = HitboxCheck(),
                HandClap = HandClapCheck()
            };

            if (*//*Plugin.configs.HighlightOffbeat TODO: fix*//* true)
            {
                HighlightOffbeat();
            }
            if (*//*Plugin.configs.DisplayFlick TODO: fix *//* true)
            {
                RollingEBPM();
            }

            FuseBombComments();

            return diffCrit;
        }

        #endregion

        #region Difficulty

        #region Outside

        // Detect objects that are outside of the audio boundary
        public CritSeverity OutsideCheck()
        {
            var issue = CritSeverity.Success;
            var cubes = BeatmapScanner.Cubes;
            var chains = BeatmapScanner.Chains;
            var bombs = BeatmapScanner.Bombs;
            var walls = BeatmapScanner.Walls;

            var end = bpm.ToBeatTime(BeatSaberSongContainer.Instance.LoadedSongLength, true);
            if (cubes.Exists(c => c.Time < 0 || c.Time > end) || chains.Exists(c => c.JsonTime < 0 || c.JsonTime > end)
                || bombs.Exists(b => b.JsonTime < 0 || b.JsonTime > end) || walls.Exists(w => w.JsonTime < 0 || w.JsonTime + w.Duration > end))
            {
                //ExtendOverallComment("R1B - Object outside of playable length"); TODO: USE NEW METHOD
                issue = CritSeverity.Fail;
            }

            return issue;
        }

        #endregion

        #region Light

        // Fetch the average event per beat, and compare it to a configurable value
        // Also check for well-lit bombs
        public CritSeverity LightCheck()
        {
            var issue = CritSeverity.Success;
            var diff = BeatSaberSongContainer.Instance.Info.DifficultyBeatmapSets.Where(d => d.BeatmapCharacteristicName == characteristic).SelectMany(d => d.DifficultyBeatmaps).Where(d => d.Difficulty == difficulty).FirstOrDefault();
            BaseDifficulty baseDifficulty = BeatSaberSongContainer.Instance.Info.GetMapFromDifficultyBeatmap(diff);
            var v3events = baseDifficulty.LightColorEventBoxGroups.OrderBy(e => e.JsonTime).ToList();
            var events = baseDifficulty.Events.OrderBy(e => e.JsonTime).ToList();
            var end = bpm.ToBeatTime(BeatSaberSongContainer.Instance.LoadedSongLength, true);
            var bombs = BeatmapScanner.Bombs.OrderBy(b => b.JsonTime).ToList();
            if (!events.Any() || !events.Exists(e => e.Type >= 0 && e.Type <= 5))
            {
                //ExtendOverallComment("R6A - Map has no light"); TODO: USE NEW METHOD
                return CritSeverity.Fail;
            }
            else
            {
                var lights = events.Where(e => e.Type >= 0 && e.Type <= 5).OrderBy(e => e.JsonTime).ToList();
                var average = lights.Count() / end;
                if (v3events.Count > 0)
                {
                    average = v3events.Count() / end;
                }
                if (average < config.AverageLightPerBeat)
                {
                    //ExtendOverallComment("R6A - Map doesn't have enough light"); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                // Based on: https://github.com/KivalEvan/BeatSaber-MapCheck/blob/main/src/ts/tools/events/unlitBomb.ts
                var eventLitTime = new List<List<EventLitTime>>();
                if (v3events.Count > 0)
                {
                    //ExtendOverallComment("R6A - Warning - V3 Lights detected. Bombs visibility won't be checked."); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }
                else
                {
                    for (var i = 0; i < 12; i++)
                    {
                        eventLitTime.Add(new());
                    }
                    for (int i = 0; i < lights.Count; i++)
                    {
                        var ev = lights[i];
                        bpm.SetCurrentBPM(ev.JsonTime);
                        var fadeTime = bpm.ToBeatTime(config.LightFadeDuration, true);
                        var reactTime = bpm.ToBeatTime(config.LightBombReactionTime, true);
                        if (ev.IsOn || ev.IsFlash || ev.IsFade)
                        {
                            eventLitTime[ev.Type].Add(new(ev.JsonTime, true));
                            if (ev.IsFade)
                            {
                                eventLitTime[ev.Type].Add(new(ev.JsonTime + fadeTime, false));
                            }
                        }
                        if (ev.FloatValue < 0.25 || ev.IsOff)
                        {
                            eventLitTime[ev.Type].Add(new(ev.JsonTime + reactTime, false));
                        }
                    }
                    foreach (var elt in eventLitTime)
                    {
                        elt.Reverse();
                    }
                    for (int i = 0; i < bombs.Count; i++)
                    {
                        var bomb = bombs[i];
                        var isLit = false;
                        foreach (var el in eventLitTime)
                        {
                            var t = el.Find(e => e.Time < bomb.JsonTime);
                            if (t != null)
                            {
                                isLit = isLit || t.State;
                            }
                        }
                        if (!isLit)
                        {
                            //CreateDiffCommentBomb("R5B - Light missing for bomb", CommentTypesEnum.Issue, bomb); TODO: USE NEW METHOD
                            issue = CritSeverity.Fail;
                        }
                    }
                }
            }
            bpm.ResetCurrentBPM();
            return issue;
        }

        public class EventState
        {
            public bool State { get; set; } = false;
            public double Time { get; set; } = 0;
            public double FadeTime { get; set; } = 0;

            public EventState(bool state, double time, double fade)
            {
                State = state;
                Time = time;
                FadeTime = fade;
            }
        }
        public class EventLitTime
        {
            public double Time { get; set; } = 0;
            public bool State { get; set; } = false;

            public EventLitTime(double time, bool state)
            {
                Time = time;
                State = state;
            }
        }

        #endregion

        #region Wall

        // Calculate dodge wall per seconds, objects hidden behind walls, walls that force players outside of boundary, walls that are too short in middle lane and negative walls.
        // Subjective and max dodge wall, min wall duration and trail duration is configurable
        // TODO: I think the dodge wall calc is pretty bad
        public CritSeverity WallCheck()
        {
            var issue = CritSeverity.Success;

            var walls = BeatmapScanner.Walls;
            var notes = BeatmapScanner.Cubes;
            var bombs = BeatmapScanner.Bombs;

            var leftWall = walls.Where(w => w.PosX == 1 && w.Width == 1);
            var rightWall = walls.Where(w => w.PosX == 2 && w.Width == 1);

            foreach (var w in leftWall)
            {
                var note = notes.Where(n => n.Line == 0 && !(n.Layer == 0 && w.PosY == 0 && w.Height == 1) && ((n.Layer >= w.PosY && n.Layer < w.PosY + w.Height) || (n.Layer >= 0 && w.PosY == 0 && w.Height > 1)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    //CreateDiffCommentNote("R3B - Hidden behind wall", CommentTypesEnum.Issue, n); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                var bomb = bombs.Where(b => b.PosX == 0 && !(b.PosY == 0 && w.PosY == 0 && w.Height == 1) && ((b.PosY >= w.PosY && b.PosY < w.PosY + w.Height) || (b.PosY >= 0 && w.PosY == 0 && w.Height > 1)) && b.JsonTime > w.JsonTime && b.JsonTime <= w.JsonTime + w.Duration).ToList();
                foreach (var b in bomb)
                {
                    //CreateDiffCommentBomb("R5E - Hidden behind wall", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }

            foreach (var w in rightWall)
            {
                var note = notes.Where(n => n.Line == 3 && !(n.Layer == 0 && w.PosY == 0 && w.Height == 1) && ((n.Layer >= w.PosY && n.Layer < w.PosY + w.Height) || (n.Layer >= 0 && w.PosY == 0 && w.Height > 1)) && n.Time > w.JsonTime && n.Time <= w.JsonTime + w.Duration && (n.Head || !n.Pattern)).ToList();
                foreach (var n in note)
                {
                    //CreateDiffCommentNote("R3B - Hidden behind wall", CommentTypesEnum.Issue, n); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                var bomb = bombs.Where(b => b.PosX == 3 && !(b.PosY == 0 && w.PosY == 0 && w.Height == 1) && ((b.PosY >= w.PosY && b.PosY < w.PosY + w.Height) || (b.PosY >= 0 && w.PosY == 0 && w.Height > 1)) && b.JsonTime > w.JsonTime && b.JsonTime <= w.JsonTime + w.Duration).ToList();
                foreach (var b in bomb)
                {
                    //CreateDiffCommentBomb("R5E - Hidden behind wall", CommentTypesEnum.Issue, b); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }

            BaseObstacle previous = null;
            foreach (var w in walls)
            {
                bpm.SetCurrentBPM(w.JsonTime);
                var min = bpm.ToBeatTime(config.MinimumWallDuration);
                var max = bpm.ToBeatTime(config.ShortWallTrailDuration);

                if (w.PosY <= 0 && w.Height > 1 && ((w.PosX + w.Width == 2 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 3 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime)) ||
                    (w.PosX + w.Width == 3 && walls.Exists(wa => wa != w && wa.PosY == 0 && wa.Height > 0 && wa.PosX + wa.Width == 2 && wa.JsonTime <= w.JsonTime + w.Duration && wa.JsonTime >= w.JsonTime))))
                {
                    //CreateDiffCommentObstacle("R4C - Force the player to move into the outer lanes", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                else if (w.PosY <= 0 && w.Height > 1 && ((w.Width >= 3 && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3 || w.PosX == 1)) || (w.Width >= 2 && w.PosX == 1 && w.PosY == 0 && w.Height > 0) || (w.Width >= 4 && w.PosX + w.Width >= 4 && w.PosX <= 0 && w.PosY == 0)))
                {
                    //CreateDiffCommentObstacle("R4C - Force the player to move into the outer lanes", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                if (w.Width <= 0 || w.Duration <= 0 || // Negative width or duration
                    (w.Height <= 0 && w.PosX >= 0 && w.PosX <= 3 && (w.PosY > 0 || w.PosY + w.Height >= 0)) // In or above with negative height
                    || ((w.PosX == 1 || w.PosX == 2 || (w.PosX + w.Width >= 2 && w.PosX <= 3)) && w.Height < 0)  // Under middle lane with negative height
                    || (w.PosX + w.Width >= 1 && w.PosX <= 4) && w.PosY + w.Height >= 0 && w.Height < 0) // Stretch above with negative height
                {
                    //CreateDiffCommentObstacle("R4D - Must have positive width, height and duration", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                if (w.Duration < min && (w.PosX + w.Width == 2 || w.PosX + w.Width == 3) && w.PosY + w.Height > 1 &&
                    !walls.Exists(wa => wa != w && wa.PosX + wa.Width >= w.PosX + w.Width && wa.PosX <= w.PosX + w.Width && wa.Duration >= min && w.JsonTime >= wa.JsonTime && w.JsonTime <= wa.JsonTime + wa.Duration + max))
                {
                    //CreateDiffCommentObstacle("R4E - Shorter than 13.8ms in the middle two lanes", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }

                previous = w;
            }

            for (int i = walls.Count - 1; i >= 0; i--)
            {
                var dodge = 0d;
                var side = 0;
                var w = walls[i];
                bpm.SetCurrentBPM(w.JsonTime);
                var sec = bpm.ToBeatTime(1);
                // All the walls under 1 second
                var wallinsec = walls.Where(x => x.JsonTime < w.JsonTime && x.JsonTime >= w.JsonTime - sec).ToList();
                wallinsec.Reverse();
                if (w.PosX + w.Width == 2 && w.PosY <= 2 && w.PosY + w.Height >= 3)
                {
                    side = 2;
                    dodge++;
                }
                else if (w.PosX == 2 && w.PosY <= 2 && w.PosY + w.Height >= 3)
                {
                    side = 1;
                    dodge++;
                }
                if (dodge == 1) // Ignore non-dodge walls
                {
                    // Count the amount of dodge in the last second
                    foreach (var wall in wallinsec)
                    {
                        if (wall.PosX + wall.Width == 2 && side != 2 && wall.PosY <= 2 && wall.PosY + wall.Height >= 3)
                        {
                            side = 2;
                            dodge++;
                        }
                        else if (wall.PosX == 2 && side != 1 && wall.PosY <= 2 && wall.PosY + wall.Height >= 3)
                        {
                            side = 1;
                            dodge++;
                        }
                    }
                    if (dodge >= config.MaximumDodgeWallPerSecond)
                    {
                        //CreateDiffCommentObstacle("R4B - Over the " + config.MaximumDodgeWallPerSecond + " dodge per second limit", CommentTypesEnum.Issue, w); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                    else if (dodge >= config.SubjectiveDodgeWallPerSecond)
                    {
                        //CreateDiffCommentObstacle("Y4A - " + Plugin.configs.SubjectiveDodgeWallPerSecond + "+ dodge per second need justification", CommentTypesEnum.Suggestion, w); TODO: USE NEW METHOD
                        issue = CritSeverity.Warning;
                    }
                }
            }

            bpm.ResetCurrentBPM();
            return issue;
        }

        #endregion

        #region Chain

        // Check if chains is part of the first 16 notes, link spacing, reverse direction, max distance, reach, and angle
        public CritSeverity ChainCheck()
        {
            var issue = CritSeverity.Success;
            var links = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            var notes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();

            if (notes.Count >= 16)
            {
                var link = links.Where(l => l.JsonTime <= notes[15].Time).ToList();
                foreach (var l in link)
                {
                    //CreateDiffCommentLink("R2D - Cannot be part of the first 16 notes", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }
            else if (links.Any())
            {
                var link = links.Where(l => l.JsonTime >= notes.Last().Time).Take(16 - notes.Count).ToList();
                foreach (var l in link)
                {
                    //CreateDiffCommentLink("R2D - Cannot be part of the first 16 notes", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }

            // TODO: Make this mess better idk
            foreach (var l in links)
            {
                var chain = (BaseChain)l;
                var x = Math.Abs(l.TailPosX - l.PosX) * chain.Squish;
                var y = Math.Abs(l.TailPosY - l.PosY) * chain.Squish;
                var distance = Math.Sqrt(x * x + y * y);
                var value = distance / (chain.SliceCount - 1);
                // Difference between expected and current distance, multiplied by current squish to know maximum value
                double max;
                if(l.TailPosY == l.PosY) max = Math.Round(config.ChainLinkVsAir / value * chain.Squish, 2);
                else max = Math.Round(config.ChainLinkVsAir * 1.1 / value * chain.Squish, 2);
                if (chain.Squish - 0.01 > max)
                {
                    //CreateDiffCommentLink("R2D - Link spacing issue. Maximum squish for placement: " + max, CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                var newX = l.PosX + (l.TailPosX - l.PosX) * chain.Squish;
                var newY = l.PosY + (l.TailPosY - l.PosY) * chain.Squish;
                if (newX > 4 || newX < -1 || newY > 2.33 || newY < -0.33)
                {
                    //CreateDiffCommentLink("R2D - Lead too far", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                if (l.TailJsonTime < l.JsonTime)
                {
                    //CreateDiffCommentLink("R2D - Reverse Direction", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
                var note = notes.Find(x => x.Time >= l.TailJsonTime && x.Type == l.Color);
                if (note != null)
                {
                    if (l.TailJsonTime + (l.TailJsonTime - l.JsonTime) > note.Time)
                    {
                        //CreateDiffCommentLink("R2D - Duration between tail and next note is too short", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }

                var temp = new Cube(notes.First())
                {
                    Direction = ScanMethod.Mod(ScanMethod.DirectionToDegree[l.CutDirection] + l.AngleOffset, 360),
                    Line = l.PosX,
                    Layer = l.PosY
                };
                var temp2 = new Cube(notes.First())
                {
                    Line = l.TailPosX,
                    Layer = l.TailPosY
                };
                var temp3 = new List<Cube>
                {
                    temp,
                    temp2
                };
                if (!ScanMethod.IsSameDirection(ScanMethod.ReverseCutDirection(ScanMethod.FindAngleViaPosition(temp3, 0, 1)), temp.Direction, config.MaxChainRotation))
                {
                    //CreateDiffCommentLink("R2D - Over 45°", CommentTypesEnum.Issue, l); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region Parity

        // JoshaParity is used to detect reset, high angle parity, and warn while playing inverted.
        // Parity warning angle is configurable
        public CritSeverity ParityCheck()
        {
            bool hadIssue = false;
            bool hadWarning = false;

            foreach (var swing in swings.Where(x => x.resetType == ResetType.Rebound).ToList())
            {
                //CreateDiffCommentNotes("R2 - Parity Error", CommentTypesEnum.Issue, swing.notes); TODO: USE NEW METHOD
                hadIssue = true;
            }
            foreach (var swing in swings.Where(x => x.swingEBPM == float.PositiveInfinity).ToList())
            {
                //CreateDiffCommentNotes("R2 - Parity Mismatch on same beat", CommentTypesEnum.Issue, swing.notes); TODO: USE NEW METHOD
                hadIssue = true;
            }

            List<JoshaParity.SwingData> rightHandSwings = swings.Where(x => x.rightHand).ToList();
            List<JoshaParity.SwingData> leftHandSwings = swings.Where(x => !x.rightHand).ToList();

            for (int i = 0; i < rightHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = rightHandSwings[i].startPos.rotation - rightHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= config.ParityWarningAngle)
                    {
                        //CreateDiffCommentNotes("Parity Warning - " + config.ParityWarningAngle + " degree difference", CommentTypesEnum.Unsure, rightHandSwings[i].notes); TODO: USE NEW METHOD
                        hadWarning = true;
                    }
                    else if (Math.Abs(rightHandSwings[i].startPos.rotation) > 135 || Math.Abs(rightHandSwings[i].endPos.rotation) > 135)
                    {
                        if(config.ParityInvertedWarning)
                        {
                            //CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, rightHandSwings[i].notes); TODO: USE NEW METHOD
                        }
                        hadWarning = true;
                    }
                }
            }

            for (int i = 0; i < leftHandSwings.Count; i++)
            {
                if (i != 0)
                {
                    float difference = leftHandSwings[i].startPos.rotation - leftHandSwings[i - 1].endPos.rotation;
                    if (Math.Abs(difference) >= config.ParityWarningAngle)
                    {
                        //CreateDiffCommentNotes("Parity Warning - " + config.ParityWarningAngle + " degree difference", CommentTypesEnum.Unsure, leftHandSwings[i].notes); TODO: USE NEW METHOD
                        hadWarning = true;
                    }
                    else if (Math.Abs(leftHandSwings[i].startPos.rotation) > 135 || Math.Abs(leftHandSwings[i].endPos.rotation) > 135)
                    {
                        if (config.ParityInvertedWarning)
                        {
                            //CreateDiffCommentNotes("Parity Warning - playing inverted", CommentTypesEnum.Unsure, leftHandSwings[i].notes); TODO: USE NEW METHOD
                        }
                        hadWarning = true;
                    }
                }
            }

            if (config.ParityDebug)
            {
                foreach (var swing in swings)
                {
                    var swingWithoutNotes = swing;
                    swingWithoutNotes.notes = null;
                    string message = JsonConvert.SerializeObject(swingWithoutNotes);
                    CommentTypesEnum commentType = CommentTypesEnum.Info;
                    if (swing.resetType == ResetType.Rebound) commentType = CommentTypesEnum.Issue;
                    if (Math.Abs(swing.endPos.rotation) > 135 || Math.Abs(swing.endPos.rotation) > 135) commentType = CommentTypesEnum.Unsure;
                    CreateDiffCommentNotes(message, commentType, swing.notes);
                }
            }

            if (hadIssue)
            {
                return CritSeverity.Fail;
            }
            else if (hadWarning)
            {
                return CritSeverity.Warning;
            }
            else
            {
                return CritSeverity.Success;
            }
        }

        #endregion

        #region VisionBlock

        // Detect notes and bombs VB based on BeatLeader current criteria
        // Most of the minimum and maximum duration are configurable
        public CritSeverity VisionBlockCheck()
        {
            var issue = CritSeverity.Success;
            var song = plugin.BeatSaberSongContainer.Info;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList(); ;
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).ToList();
                List<BaseNote> lastMidL = new();
                List<BaseNote> lastMidR = new();
                List<BaseNote> arr = new();
                for (var i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    bpm.SetCurrentBPM(note.JsonTime);
                    var MaxBottomNoteTime = bpm.ToBeatTime(config.VBMinBottomNoteTime);
                    var MaxOuterNoteTime = bpm.ToBeatTime(config.VBMaxOuterNoteTime);
                    var Overall = bpm.ToBeatTime(config.VBMinimum);
                    var MinTimeWarning = bpm.ToBeatTime(((800 - 300) * Math.Pow(Math.E, -BeatmapScannerData.pass / 7.6 - BeatmapScannerData.tech * 0.04) + 300) / 1000);
                    lastMidL.RemoveAll(l => note.JsonTime - l.JsonTime > MinTimeWarning);
                    lastMidR.RemoveAll(l => note.JsonTime - l.SongBpmTime > MinTimeWarning);
                    if (lastMidL.Count > 0)
                    {
                        if (note.JsonTime - lastMidL.First().JsonTime >= Overall) // Further than 0.025
                        {
                            if (note.JsonTime - lastMidL.First().JsonTime <= MinTimeWarning) // Warning
                            {
                                if (note.PosX == 0 && note.JsonTime - lastMidL.First().JsonTime <= MaxOuterNoteTime) // Closer than 0.15 in outer lane
                                {
                                    // Fine
                                }
                                else if (note.PosX == 1 && note.PosY == 0 && note.JsonTime - lastMidL.First().JsonTime <= MaxBottomNoteTime) // Closer than 0.075 at bottom layer
                                {
                                    // Also fine
                                }
                                else if(note.PosX < 2)
                                {
                                    arr.Add(note);
                                    if (note.Type == 0 || note.Type == 1)
                                    {
                                        //CreateDiffCommentNote("R2B - Possible VB - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Unsure,
                                        //  cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer)); TODO: USE NEW METHOD
                                        issue = CritSeverity.Warning;
                                    }
                                }
                            }
                        }
                    }
                    if (lastMidR.Count > 0)
                    {
                        if (note.JsonTime - lastMidR.First().JsonTime >= Overall)
                        {
                            if (note.JsonTime - lastMidR.First().JsonTime <= MinTimeWarning)
                            {
                                if (note.PosX == 3 && note.JsonTime - lastMidR.First().JsonTime <= MaxOuterNoteTime)
                                {
                                    // Fine
                                }
                                else if (note.PosX == 2 && note.PosY == 0 && note.JsonTime - lastMidR.First().JsonTime <= MaxBottomNoteTime)
                                {
                                    // Also fine
                                }
                                else if (note.PosX > 1)
                                {
                                    arr.Add(note);
                                    if (note.Type == 0 || note.Type == 1)
                                    {
                                        //CreateDiffCommentNote("R2B - Possible VB - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Unsure,
                                        //    cubes.Find(c => c.Time == note.JsonTime && c.Type == note.Type && note.PosX == c.Line && note.PosY == c.Layer)); TODO: USE NEW METHOD
                                        issue = CritSeverity.Warning;
                                    }
                                }
                            }
                        }
                    }
                    if (note.PosY == 1 && note.PosX == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.PosY == 1 && note.PosX == 2)
                    {
                        lastMidR.Add(note);
                    }
                }

                // Bombs
                lastMidL = new List<BaseNote>();
                lastMidR = new List<BaseNote>();
                arr = new();
                for (var i = 0; i < notes.Count; i++)
                {
                    var note = notes[i];
                    if (note.Type == 3)
                    {
                        bpm.SetCurrentBPM(note.JsonTime);
                        var MaxTimeBomb = bpm.ToBeatTime(config.VBMaxBombTime);
                        var MinTimeBomb = bpm.ToBeatTime(config.VBMinBombTime);
                        var Overall = bpm.ToBeatTime(config.VBMinimum);
                        var left = notes.Where(x => x.JsonTime < note.JsonTime && x.Type == 0).OrderBy(o => o.JsonTime).LastOrDefault();
                        var right = notes.Where(x => x.JsonTime < note.JsonTime && x.Type == 1).OrderBy(o => o.JsonTime).LastOrDefault();
                        lastMidL.RemoveAll(l => note.JsonTime - l.JsonTime > MinTimeBomb);
                        lastMidR.RemoveAll(l => note.JsonTime - l.JsonTime > MinTimeBomb);
                        if (lastMidL.Count > 0)
                        {
                            if (note.JsonTime - lastMidL.First().JsonTime <= MinTimeBomb) // Closer than 0.20
                            {
                                if (note.PosX == 0 && note.JsonTime - lastMidL.First().JsonTime <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((note.PosX != 1 || note.PosY != 1) && note.JsonTime - lastMidL.First().JsonTime <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (note.PosX < 2)
                                {
                                    if (left != null)
                                    {
                                        if (left.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - left.PosX, 2) + Math.Pow(note.PosY - left.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                //TODO: USE NEW METHOD
                                                issue = CritSeverity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.PosX, left.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.CutDirection))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            //TODO: USE NEW METHOD
                                            issue = CritSeverity.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - right.PosX, 2) + Math.Pow(note.PosY - right.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                //TODO: USE NEW METHOD
                                                issue = CritSeverity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.PosX, right.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.CutDirection))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidL.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            //TODO: USE NEW METHOD
                                            issue = CritSeverity.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        if (lastMidR.Count > 0)
                        {
                            if (note.JsonTime - lastMidR.First().JsonTime <= MinTimeBomb) // Closer than 0.20
                            {
                                if (note.PosX == 3 && note.JsonTime - lastMidR.First().JsonTime <= MaxTimeBomb) // Closer than 0.15
                                {
                                    // Fine
                                }
                                else if ((note.PosX != 2 || note.PosY != 1) && note.JsonTime - lastMidR.First().JsonTime <= Overall) // Closer than 0.025
                                {
                                    // Also fine
                                }
                                else if (note.PosX > 1)
                                {
                                    if (left != null)
                                    {
                                        if (left.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - left.PosX, 2) + Math.Pow(note.PosY - left.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                //TODO: USE NEW METHOD
                                                issue = CritSeverity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (left.PosX, left.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, left.CutDirection))
                                        {
                                            pos = NoteDirection.Move(left, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            //TODO: USE NEW METHOD
                                            issue = CritSeverity.Fail;
                                            continue;
                                        }
                                    }
                                    if (right != null)
                                    {
                                        if (right.CutDirection == 8)
                                        {
                                            var di = Math.Sqrt(Math.Pow(note.PosX - right.PosX, 2) + Math.Pow(note.PosY - right.PosY, 2));
                                            if (di >= 0 && di < 1.001)
                                            {
                                                //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                                //TODO: USE NEW METHOD
                                                issue = CritSeverity.Fail;
                                            }
                                            continue;
                                        }

                                        var pos = (right.PosX, right.PosY);
                                        int index = 1;
                                        while (!NoteDirection.IsLimit(pos, right.CutDirection))
                                        {
                                            pos = NoteDirection.Move(right, index);
                                            index++;
                                        }

                                        var d = Math.Sqrt(Math.Pow(note.PosX - pos.PosX, 2) + Math.Pow(note.PosY - pos.PosY, 2));
                                        if (d >= 0 && d < 1.001)
                                        {
                                            //CreateDiffCommentBomb("R5E - Is vision blocked - " + Math.Round(bpm.ToRealTime(note.JsonTime - lastMidR.First().JsonTime) * 1000, 0) + "ms", CommentTypesEnum.Issue, note);
                                            //TODO: USE NEW METHOD
                                            issue = CritSeverity.Fail;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (note.PosY == 1 && note.PosX == 1)
                    {
                        lastMidL.Add(note);
                    }
                    if (note.PosY == 1 && note.PosX == 2)
                    {
                        lastMidR.Add(note);
                    }
                }
            }

            bpm.ResetCurrentBPM();
            return issue;
        }

        #endregion

        #region ProlongedSwing

        // Very basic check for stuff like Pauls, Dotspam, long chain duration, etc.
        public CritSeverity ProlongedSwingCheck()
        {
            var issue = false;
            var unsure = false;
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var chains = BeatmapScanner.Chains.OrderBy(c => c.JsonTime).ToList();
            var slider = false;
            if (cubes.Exists(x => x.Slider))
            {
                slider = true;
            }
            foreach (var ch in chains)
            {
                if (ch.TailJsonTime - ch.JsonTime >= averageSliderDuration * 4.2)
                {
                    if (slider)
                    {
                        //CreateDiffCommentLink("R2D - Duration is too high", CommentTypesEnum.Issue, ch); TODO: USE NEW METHOD
                        issue = true;
                    }
                    else if (ch.TailJsonTime - ch.JsonTime > 0.125)
                    {
                        //CreateDiffCommentLink("R2D - Duration might be too high", CommentTypesEnum.Unsure, ch); TODO: USE NEW METHOD
                        unsure = true;
                    }
                }
                else if (ch.TailJsonTime - ch.JsonTime >= averageSliderDuration * 3.15)
                {
                    if (slider)
                    {
                        //CreateDiffCommentLink("Y2A - Recommend shorter chain", CommentTypesEnum.Suggestion, ch); TODO: USE NEW METHOD
                        unsure = true;
                    }
                    else if (ch.TailJsonTime - ch.JsonTime > 0.125)
                    {
                        //CreateDiffCommentLink("Y2A - Duration might be too high", CommentTypesEnum.Unsure, ch); TODO: USE NEW METHOD
                        unsure = true;
                    }
                }
                if (!cubes.Exists(c => c.Time == ch.JsonTime && c.Type == ch.Color && c.Line == ch.PosX && c.Layer == ch.PosY))
                {
                    // Link spam maybe idk
                    //CreateDiffCommentLink("R2D - No head note", CommentTypesEnum.Issue, ch); TODO: USE NEW METHOD
                    issue = true;
                }
            }
            // Dot spam and pauls maybe
            var leftCube = cubes.Where(d => d.Type == 0).ToList();
            var rightCube = cubes.Where(d => d.Type == 1).ToList();
            Cube previous = null;
            foreach (var left in leftCube)
            {
                if (previous != null)
                {
                    if (((left.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(left.Direction, previous.Direction, 67.5)) || (left.Time - previous.Time <= 0.142857)) && left.Time != previous.Time && left.Line == previous.Line && left.Layer == previous.Layer)
                    {
                        if (left.CutDirection == 8)
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Unsure, left); TODO: USE NEW METHOD
                            unsure = true;
                        }
                        else
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Issue, left); TODO: USE NEW METHOD
                            issue = true;
                        }
                    }
                }

                previous = left;
            }

            previous = null;
            foreach (var right in rightCube)
            {
                if (previous != null)
                {
                    if (((right.Time - previous.Time <= 0.25 && ScanMethod.IsSameDirection(right.Direction, previous.Direction, 67.5)) || (right.Time - previous.Time <= 0.142857)) && right.Time != previous.Time && right.Line == previous.Line && right.Layer == previous.Layer)
                    {
                        if (right.CutDirection == 8)
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Unsure, right); TODO: USE NEW METHOD
                            unsure = true;
                        }
                        else
                        {
                            //CreateDiffCommentNote("R2A - Swing speed", CommentTypesEnum.Issue, right); TODO: USE NEW METHOD
                            issue = true;
                        }
                    }
                }

                previous = right;
            }

            if (issue)
            {
                return CritSeverity.Fail;
            }
            else if (unsure)
            {
                return CritSeverity.Warning;
            }

            return CritSeverity.Success;
        }

        #endregion

        #region Loloppe

        // Detect parallel notes
        public CritSeverity LoloppeCheck()
        {
            var issue = CritSeverity.Success;


            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
            var red = cubes.Where(c => c.Type == 0).ToList();
            var blue = cubes.Where(c => c.Type == 1).ToList();
            for (int i = 1; i < red.Count; i++)
            {
                if (red[i].CutDirection == 8 || red[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (red[i].Time - red[i - 1].Time < 0.125)
                {
                    var sliderAngle = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle - red[i].Direction) >= 90)
                    {
                        var temp = red[i];
                        red[i] = red[i - 1];
                        red[i - 1] = temp;
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(red[i].Layer - red[i - 1].Layer, red[i].Line - red[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - red[i].Direction) >= 45 && Math.Abs(sliderAngle2 - red[i].Direction) <= 90)
                    {
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, red[i - 1]); TODO: USE NEW METHOD 
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, red[i]); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
            }
            for (int i = 1; i < blue.Count; i++)
            {
                if (blue[i].CutDirection == 8 || blue[i - 1].CutDirection == 8)
                {
                    continue;
                }
                if (blue[i].Time - blue[i - 1].Time < 0.125)
                {
                    var sliderAngle = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle - blue[i].Direction) >= 90)
                    {
                        var temp = blue[i];
                        blue[i] = blue[i - 1];
                        blue[i - 1] = temp;
                    }
                    var sliderAngle2 = ScanMethod.Mod(ScanMethod.ConvertRadiansToDegrees(Math.Atan2(blue[i].Layer - blue[i - 1].Layer, blue[i].Line - blue[i - 1].Line)), 360);
                    if (Math.Abs(sliderAngle2 - blue[i].Direction) >= 45 && Math.Abs(sliderAngle2 - blue[i].Direction) <= 90)
                    {
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, blue[i - 1]); TODO: USE NEW METHOD 
                        //CreateDiffCommentNote("R3C - Loloppe", CommentTypesEnum.Issue, blue[i]); TODO: USE NEW METHOD
                        issue = CritSeverity.Fail;
                    }
                }
            }

            return issue;
        }

        #endregion

        #region SwingPath

        public static bool NearestPointOnFiniteLine(Vector2 A, Vector2 B, Vector2 P)
        {
            Vector2 direction = B - A;
            Vector2 pointAP = P - A;

            float t = Vector2.Dot(pointAP, direction) / Vector2.Dot(direction, direction);
            if (t < 0)
            {
                // Before A
            }
            else if (t > 1)
            {
                // After B
                Vector2 closestPoint = B;
                float distance = Vector2.Distance(P, closestPoint);
                if (distance < 0.4) return true;
            }
            else
            {
                // In between
                Vector2 closestPoint = A + t * direction;
                float distance = Vector2.Distance(P, closestPoint);
                if (distance < 0.4) return true;
            }
            return false;
        }

        // Check if a note block the swing path of another note of a different color
        public CritSeverity SwingPathCheck()
        {
            var issue = CritSeverity.Success;

            var song = plugin.BeatSaberSongContainer.Info;
            var bpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                List<List<Cube>> doubleNotes = new();
                foreach (var note in cubes) // Find the double and group them together
                {
                    var n = cubes.Where(n => n.Time == note.Time && n != note && ((n.Type == 0 && note.Type == 1) || (n.Type == 1 && note.Type == 0))).FirstOrDefault();
                    if (n != null)
                    {
                        if(doubleNotes.Count == 0)
                        {
                            doubleNotes.Add(new());
                        }
                        else if (!doubleNotes.Last().Exists(x => x.Time == n.Time))
                        {
                            doubleNotes.Add(new());
                            doubleNotes[doubleNotes.Count - 1] = new();
                        }
                        if (!doubleNotes.Last().Contains(note)) doubleNotes.Last().Add(note);
                        if (!doubleNotes.Last().Contains(n)) doubleNotes.Last().Add(n);
                    }
                }

                // No dot support for now
                foreach (var group in doubleNotes)
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        var note = group[i];
                        for (int j = 0; j < group.Count; j++)
                        {
                            if (i == j) continue;
                            var note2 = group[j];
                            if (note.Time != note2.Time) break; // Not a double anymore
                            if (note.Type == note2.Type) continue; // Same color
                                                                   // Fetch previous note, simulate swing
                            var previous = cubes.Where(c => c.Time < note2.Time && c.Type == note2.Type).LastOrDefault();
                            if (previous != null)
                            {
                                var angleOfAttack = note2.Direction;
                                var prevDirection = ScanMethod.ReverseCutDirection(previous.Direction);
                                // This is calculating the angle between the previous note + extra swing and the next note
                                if (note2.CutDirection != 8 && previous.CutDirection != 8)
                                {
                                    var di = Math.Abs(prevDirection - angleOfAttack);
                                    di = Math.Min(di, 360 - di) / 4;
                                    if (angleOfAttack < prevDirection)
                                    {
                                        if (prevDirection - angleOfAttack < 180)
                                        {
                                            angleOfAttack += di;
                                        }
                                        else
                                        {
                                            angleOfAttack -= di;
                                        }
                                    }
                                    else
                                    {
                                        if (angleOfAttack - prevDirection < 180)
                                        {
                                            angleOfAttack -= di;
                                        }
                                        else
                                        {
                                            angleOfAttack += di;
                                        }
                                    }
                                    // Simulate the position of the line based on the new angle found
                                    var simulatedLineOfAttack = ScanMethod.SimulateSwingPos(note2.Line, note2.Layer, ScanMethod.ReverseCutDirection(angleOfAttack), 2);
                                    // Check if the other note is close to the line
                                    var InPath = NearestPointOnFiniteLine(new(note2.Line, note2.Layer), new((float)simulatedLineOfAttack.x, (float)simulatedLineOfAttack.y), new(note.Line, note.Layer));
                                    if (InPath)
                                    {
                                        //CreateDiffCommentNote("Swing Path", CommentTypesEnum.Info, note); TODO: USE NEW METHOD
                                    }
                                }
                            }
                        }
                    }
                }
                
                List<BaseNote> arr = new();
                List<BaseNote> arr2 = new();
                var lastTime = 0d;
                
                for (int i = 0; i < notes.Count; i++)
                {
                    var currentNote = notes[i];
                    if (currentNote.Type == 3 || (currentNote.JsonTime / bpm * 60) < lastTime + 0.01)
                    {
                        continue;
                    }
                    for (int j = i + 1; j < notes.Count; j++)
                    {
                        var compareTo = notes[j];
                        if ((compareTo.JsonTime / bpm * 60) > (currentNote.JsonTime / bpm * 60) + 0.01)
                        {
                            break;
                        }
                        if ((compareTo.Type == 0 || compareTo.Type == 1) && currentNote.Type == compareTo.Type)
                        {
                            continue;
                        }
                        double[,] angle = { { 45, 1 }, { 15, 2 } };
                        double[,] angle2 = { { 45, 1 }, { 15, 1.5 } };
                        var IsDiagonal = false;
                        var dX = Math.Abs(currentNote.PosX - compareTo.PosX);
                        var dY = Math.Abs(currentNote.PosY - compareTo.PosY);
                        if (dX == dY)
                        {
                            IsDiagonal = true;
                        }
                        var a = swings.Where(x => x.notes.Any(y => y.b == currentNote.JsonTime && y.c == currentNote.Type && y.d == currentNote.CutDirection && y.x == currentNote.PosX && y.y == currentNote.PosY)).FirstOrDefault();
                        var b = swings.Where(x => x.notes.Any(y => y.b == compareTo.JsonTime && y.c == compareTo.Type && y.d == compareTo.CutDirection && y.x == compareTo.PosX && y.y == compareTo.PosY)).FirstOrDefault();
                        var d = Math.Sqrt(Math.Pow(currentNote.PosX - compareTo.PosX, 2) + Math.Pow(currentNote.PosY - compareTo.PosY, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {
                            if(currentNote.CutDirection == compareTo.CutDirection && SwingType.Diagonal.Contains(currentNote.CutDirection))
                            {
                                arr.Add(currentNote);
                                lastTime = (currentNote.JsonTime / bpm * 60);
                                continue;
                            }
                        }
                        if(IsDiagonal)
                        {
                            var pos = (currentNote.PosX, currentNote.PosY);
                            var target = (compareTo.PosX, compareTo.PosY);
                            var index = 1;
                            var rev = Reverse.Get(currentNote.CutDirection);
                            if(currentNote.CutDirection != 8)
                            {
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(currentNote, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        arr2.Add(currentNote);
                                        lastTime = (currentNote.JsonTime / bpm * 60);
                                        continue;
                                    }
                                }
                            }
                            if(compareTo.CutDirection != 8)
                            {
                                target = (currentNote.PosX, currentNote.PosY);
                                pos = (compareTo.PosX, compareTo.PosY);
                                index = 1;
                                rev = Reverse.Get(compareTo.CutDirection);
                                while (!NoteDirection.IsLimit(pos, rev))
                                {
                                    pos = NoteDirection.Move(compareTo, -index);
                                    index++;
                                    if (pos == target)
                                    {
                                        arr2.Add(currentNote);
                                        lastTime = (currentNote.JsonTime / bpm * 60);
                                        continue;
                                    }
                                }
                            }
                        }
                        if (((currentNote.PosY == compareTo.PosY || currentNote.PosX == compareTo.PosX) && Swing.IsIntersect(currentNote, compareTo, angle, 2)) ||
                                (IsDiagonal && Swing.IsIntersect(currentNote, compareTo, angle2, 2)))
                        {
                            arr.Add(currentNote);
                            lastTime = (currentNote.JsonTime / bpm * 60);
                        }
                    }
                }
                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3E - Swing Path", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                    //                    && item.PosX == c.Line && item.PosY == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region Hitbox

        // Implementation of Kival Evan hitboxInline.ts, hitboxStair.ts and hitboxReverseStaircase.ts
        public CritSeverity HitboxCheck()
        {
            var issue = CritSeverity.Success;

            var song = plugin.BeatSaberSongContainer.Info;
            var bpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);

            if (baseDifficulty.Notes.Any())
            {
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                var njs = diff.NoteJumpMovementSpeed;
                BaseNote[] lastNote = { null, null };
                List<List<BaseNote>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<BaseNote>();

                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (lastNote[note.Type] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Type], bpm, swingNoteArray[note.Type]))
                        {
                            swingNoteArray[note.Type].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Type + 1) % 2])
                    {
                        if (other.Type != 0 && other.Type != 1)
                        {
                            continue;
                        }
                        var isInline = false;
                        var distance = Math.Sqrt(Math.Pow(note.PosX - other.PosX, 2) + Math.Pow(note.PosY - other.PosY, 2));
                        if (distance <= 0.5)
                        {
                            isInline = true;
                        }
                        if (njs < 1.425 / ((60 * (note.JsonTime - other.JsonTime)) / bpm) && isInline)
                        {
                            arr.Add(note);
                            break;
                        }
                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3G - Low NJS Inline", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                    //                    && item.PosX == c.Line && item.PosY == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }

                var hitboxTime = (0.15 * bpm) / 60;
                int[] lastNoteDirection = { -1, -1 };
                double[] lastSpeed = { -1, -1 };
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                Cube[] noteOccupy = { new(), new() };
                arr.Clear();
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (lastNote[note.Type] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Type], bpm, swingNoteArray[note.Type]))
                        {
                            lastSpeed[note.Type] = note.JsonTime - lastNote[note.Type].JsonTime;
                            if (note.CutDirection != NoteDirection.ANY)
                            {
                                noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(note.CutDirection)[0];
                                noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(note.CutDirection)[1];
                            }
                            else
                            {
                                noteOccupy[note.Type].Line = -1;
                                noteOccupy[note.Type].Layer = -1;
                            }
                            swingNoteArray[note.Type].Clear();
                            lastNoteDirection[note.Type] = note.CutDirection;
                        }
                        else if (Parity.IsEnd(note, lastNote[note.Type], lastNoteDirection[note.Type]))
                        {
                            if (note.CutDirection != NoteDirection.ANY)
                            {
                                noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(note.CutDirection)[0];
                                noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(note.CutDirection)[1];
                                lastNoteDirection[note.Type] = note.CutDirection;
                            }
                            else
                            {
                                noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(lastNoteDirection[note.Type])[0];
                                noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(lastNoteDirection[note.Type])[1];
                            }
                        }
                        if (lastNote[(note.Type + 1) % 2] != null)
                        {
                            if (note.JsonTime - lastNote[(note.Type + 1) % 2].JsonTime != 0 &&
                                note.JsonTime - lastNote[(note.Type + 1) % 2].JsonTime < Math.Min(hitboxTime, lastSpeed[(note.Type + 1) % 2]))
                            {
                                if (note.PosX == noteOccupy[(note.Type + 1) % 2].Line && note.PosY == noteOccupy[(note.Type + 1) % 2].Layer && !Swing.IsDouble(note, notes, i))
                                {
                                    arr.Add(note);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (note.CutDirection != NoteDirection.ANY)
                        {
                            noteOccupy[note.Type].Line = note.PosX + NoteDirectionSpace.Get(note.CutDirection)[0];
                            noteOccupy[note.Type].Layer = note.PosY + NoteDirectionSpace.Get(note.CutDirection)[1];
                        }
                        else
                        {
                            noteOccupy[note.Type].Line = -1;
                            noteOccupy[note.Type].Layer = -1;
                        }
                        lastNoteDirection[note.Type] = note.CutDirection;
                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3G - Staircase", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                    //                    && item.PosX == c.Line && item.PosY == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }

                var constant = 0.03414823529;
                var constantDiagonal = 0.03414823529;
                lastNote[0] = null;
                lastNote[1] = null;
                swingNoteArray = new()
                {
                    new(),
                    new()
                };
                arr.Clear();
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (lastNote[note.Type] != null)
                    {
                        if (Swing.Next(note, lastNote[note.Type], bpm, swingNoteArray[note.Type]))
                        {
                            swingNoteArray[note.Type].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Type + 1) % 2])
                    {
                        if (other.Type != 0 && other.Type != 1)
                        {
                            continue;
                        }
                        if (other.CutDirection != NoteDirection.ANY)
                        {
                            if (!((note.JsonTime / bpm * 60) > (other.JsonTime / bpm * 60) + 0.01))
                            {
                                continue;
                            }
                            var isDiagonal = Swing.NoteDirectionAngle[other.CutDirection] % 90 > 15 && Swing.NoteDirectionAngle[other.CutDirection] % 90 < 75;
                            double[,] value = { { 15, 1.5 } };
                            if (njs < 1.425 / ((60 * (note.JsonTime - other.JsonTime)) / bpm + (isDiagonal ? constantDiagonal : constant)) &&
                                Swing.IsIntersect(note, other, value, 1))
                            {
                                arr.Add(other);
                                break;
                            }
                        }

                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3G - Reverse Staircase", CommentTypesEnum.Unsure, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                    //                    && item.PosX == c.Line && item.PosY == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }
            }

            return issue;
        }

        #endregion

        #region handclap

        // Attempt to detect specific note and angle placement based on BeatLeader criteria
        public CritSeverity HandClapCheck()
        {
            var issue = CritSeverity.Success;
            var song = plugin.BeatSaberSongContainer.Info;
            var bpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
            BeatSaberSong.DifficultyBeatmap diff = song.DifficultyBeatmapSets.Where(x => x.BeatmapCharacteristicName == characteristic).FirstOrDefault().DifficultyBeatmaps.Where(y => y.Difficulty == difficulty && y.DifficultyRank == difficultyRank).FirstOrDefault();
            BaseDifficulty baseDifficulty = song.GetMapFromDifficultyBeatmap(diff);
            if (baseDifficulty.Notes.Any())
            {
                var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();
                List<BaseNote> notes = baseDifficulty.Notes.Where(n => n.Type == 0 || n.Type == 1 || n.Type == 3).ToList();
                notes = notes.OrderBy(o => o.JsonTime).ToList();
                BaseNote previous = notes[0];
                BaseNote[] lastNote = { null, null };
                List<List<BaseNote>> swingNoteArray = new()
                {
                    new(),
                    new()
                };
                var arr = new List<BaseNote>();
                var arr2 = new List<BaseNote>();
                for (int i = 0; i < notes.Count; i++)
                {
                    if (notes[i].Type != 0 && notes[i].Type != 1)
                    {
                        continue;
                    }
                    var note = notes[i];
                    if (note.CutDirection == 8)
                    {
                        continue;
                    }
                    if (lastNote[note.Type] != null)
                    {
                        if (note.JsonTime != lastNote[note.Type].JsonTime)
                        {
                            swingNoteArray[note.Type].Clear();
                        }
                    }
                    foreach (var other in swingNoteArray[(note.Type + 1) % 2])
                    {
                        if (other.CutDirection == 8)
                        {
                            continue;
                        }
                        if (other.Type != 0 && other.Type != 1)
                        {
                            continue;
                        }
                        if (note.JsonTime != other.JsonTime)
                        {
                            continue;
                        }
                        var d = Math.Sqrt(Math.Pow(note.PosX - other.PosX, 2) + Math.Pow(note.PosY - other.PosY, 2));
                        if (d > 0.499 && d < 1.001) // Adjacent
                        {

                            if (other.PosX == note.PosX)
                            {
                                if ((SwingType.Up.Contains(note.CutDirection) && SwingType.Down.Contains(other.CutDirection)) ||
                                    (SwingType.Down.Contains(note.CutDirection) && SwingType.Up.Contains(other.CutDirection)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                            else if (other.PosY == note.PosY)
                            {
                                if ((SwingType.Left.Contains(note.CutDirection) && SwingType.Right.Contains(other.CutDirection)) ||
                                    (SwingType.Right.Contains(note.CutDirection) && SwingType.Left.Contains(other.CutDirection)))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d >= 1.001 && d < 2) // Diagonal
                        {
                            if (((note.CutDirection == 6 && note.PosY > other.PosY && note.PosX > other.PosX) ||
                                (note.CutDirection == 7 && note.PosY > other.PosY && note.PosX < other.PosX) ||
                                (note.CutDirection == 4 && note.PosY < other.PosY && note.PosX > other.PosX) ||
                                (note.CutDirection == 5 && note.PosY < other.PosY && note.PosX < other.PosX)) && Reverse.Get(note.CutDirection) == other.CutDirection)
                            {
                                arr.Add(other);
                                arr.Add(note);
                                break;
                            }
                        }
                        else if (d >= 2 && d <= 2.99) // 1-2 wide
                        {
                            if (NoteDirection.Move(note) == NoteDirection.Move(other))
                            {
                                if ((note.Type == 0 && note.PosX > other.PosX) || (note.Type == 1 && note.PosX < other.PosX)) // Crossover
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                                else if ((note.PosX == other.PosX + 2 && note.PosY == other.PosY + 2) || (other.PosX == note.PosX + 2 && other.PosY == note.PosY + 2) // Facing directly
                                || (note.PosX == other.PosX + 2 && note.PosY == other.PosY - 2) || (other.PosX == note.PosX + 2 && other.PosY == note.PosY - 2)
                                || (note.PosX == other.PosX && note.PosY == other.PosY + 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (note.PosX == other.PosX && note.PosY == other.PosY - 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (other.PosY == note.PosY && other.PosX == note.PosX + 2 && Reverse.Get(note.CutDirection) == other.CutDirection) ||
                                (other.PosY == note.PosY && other.PosX == note.PosX - 2 && Reverse.Get(note.CutDirection) == other.CutDirection))
                                {
                                    arr.Add(other);
                                    arr.Add(note);
                                    break;
                                }
                            }
                        }
                        else if (d > 2.99 && ((note.Type == 0 && note.PosX > 2) || (note.Type == 1 && note.PosX < 1))) // 3-wide
                        {
                            // TODO: This is trash, could easily be done better
                            if (other.PosY == note.PosY)
                            {
                                if (((SwingType.Up_Left.Contains(note.CutDirection) && SwingType.Up_Right.Contains(other.CutDirection) && note.Type == 1) ||
                                    (SwingType.Up_Right.Contains(note.CutDirection) && SwingType.Up_Left.Contains(other.CutDirection) && note.Type == 0)))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }
                                if ((SwingType.Down_Left.Contains(note.CutDirection) && SwingType.Down_Right.Contains(other.CutDirection) && note.Type == 1) ||
                                (SwingType.Down_Right.Contains(note.CutDirection) && SwingType.Down_Left.Contains(other.CutDirection) && note.Type == 0))
                                {
                                    arr2.Add(other);
                                    arr2.Add(note);
                                    break;
                                }

                            }
                        }
                    }
                    lastNote[note.Type] = note;
                    swingNoteArray[note.Type].Add(note);
                }

                foreach (var item in arr2)
                {
                    //CreateDiffCommentNote("R3D - Hand clap", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                    //                    && item.PosX == c.Line && item.PosY == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Warning;
                }

                foreach (var item in arr)
                {
                    //CreateDiffCommentNote("R3D - Hand clap", CommentTypesEnum.Issue, cubes.Find(c => c.Time == item.JsonTime && c.Type == item.Type
                    //                    && item.PosX == c.Line && item.PosY == c.Layer)); TODO: USE NEW METHOD
                    issue = CritSeverity.Fail;
                }
            }

            return issue;
        }

        #endregion

        #region offbeat

        public static readonly float[] AllowedSnap = { 0, 0.125f, 0.167f, 0.25f, 0.333f, 0.375f, 0.5f, 0.625f, 0.667f, 0.75f, 0.833f, 0.875f };

        public void HighlightOffbeat()
        {
            var swings = BeatmapScanner.Datas.OrderBy(c => c.Time).ToList();
            if (swings.Any())
            {
                foreach (var swing in swings)
                {
                    var precision = (float)Math.Round(swing.Start.Time % 1, 3);
                    if (!AllowedSnap.Contains(precision))
                    {
                        var reality = ScanMethod.RealToFraction(precision, 0.01);
                        CreateDiffCommentNote(reality.N.ToString() + "/" + reality.D.ToString(), CommentTypesEnum.Info, swing.Start);
                    }
                }
            }
        }

        #endregion

        #region ebpm

        public class EbpmData
        {
            public JoshaParity.SwingData Swing { get; set; } = new();
            public float Average { get; set; } = 0f;
            public bool Flick { get; set; } = false;
        }

        public void RollingEBPM()
        {
            var windowSize = 4f; // beats
            Queue<JoshaParity.SwingData> dataWindowLeft = new();
            Queue<JoshaParity.SwingData> dataWindowRight = new();
            List<EbpmData> rollingAverageLeft = new();
            List<EbpmData> rollingAverageRight = new();
            List<EbpmData> ReverseRollingAverageLeft = new();
            List<EbpmData> ReverseRollingAverageRight = new();
            var cubes = BeatmapScanner.Cubes.OrderBy(c => c.Time).ToList();

            foreach (var swing in swings)
            {
                EbpmData data = new();
                var clean = true;
                if(!swing.rightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        if (dataWindowLeft.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowLeft.Select(d => d.swingEBPM).Average();
                    rollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().swingStartBeat < swing.swingStartBeat - windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.swingEBPM).Average();
                    rollingAverageRight.Add(data);
                }
            }
            dataWindowLeft.Clear();
            dataWindowRight.Clear();
            for (int i = swings.Count - 1; i >= 0; i--)
            {
                var swing = swings[i];
                EbpmData data = new();
                var clean = true;
                if (!swing.rightHand)
                {
                    dataWindowLeft.Enqueue(swing);
                    do
                    {
                        if (dataWindowLeft.Peek().swingStartBeat > swing.swingStartBeat + windowSize) dataWindowLeft.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowLeft.Select(d => d.swingEBPM).Average();
                    ReverseRollingAverageLeft.Add(data);
                }
                else
                {
                    dataWindowRight.Enqueue(swing);
                    do
                    {
                        if (dataWindowRight.Peek().swingStartBeat > swing.swingStartBeat + windowSize) dataWindowRight.Dequeue();
                        else clean = false;
                    } while (clean);
                    data.Swing = swing;
                    data.Average = dataWindowRight.Select(d => d.swingEBPM).Average();
                    ReverseRollingAverageRight.Add(data);
                }
            }

            foreach (var data in rollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageLeft)
            {
                if (data.Average * 2 < data.Swing.swingEBPM ) data.Flick = true;
            }
            foreach (var data in rollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            foreach (var data in ReverseRollingAverageRight)
            {
                if (data.Average * 2 < data.Swing.swingEBPM) data.Flick = true;
            }
            rollingAverageLeft.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageLeft.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);
            rollingAverageRight.ForEach(r => r.Flick = r.Flick == true && true == ReverseRollingAverageRight.Where(a => a.Swing.Equals(r.Swing)).FirstOrDefault().Flick);
            
            foreach(var data in rollingAverageLeft)
            {
                if(data.Flick)
                {
                    var note = data.Swing.notes.FirstOrDefault();
                    var index = cubes.FindIndex(c => c.Time == note.b && c.Type == note.c && note.x == c.Line && note.y == c.Layer);
                    var cube = cubes[index];
                    if (index < cubes.Count - 3)
                    {
                        if (cubes[index + 1].Time - cube.Time != cubes[index + 2].Time - cubes[index + 1].Time) continue;
                            //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                    }
                    else continue; //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                }
            }
            foreach (var data in rollingAverageRight)
            {
                if (data.Flick)
                {
                    var note = data.Swing.notes.FirstOrDefault();
                    var index = cubes.FindIndex(c => c.Time == note.b && c.Type == note.c && note.x == c.Line && note.y == c.Layer);
                    var cube = cubes[index];
                    if(index < cubes.Count - 3)
                    {
                        if (cubes[index + 1].Time - cube.Time != cubes[index + 2].Time - cubes[index + 1].Time) continue;
                        //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                    }
                    else continue; //CreateDiffCommentNote("Unexpected flick", CommentTypesEnum.Info, cube); TODO: USE NEW METHOD
                }
            }
        }

        #endregion

        #region Comments
        // THIS IS ALL OLD TODO: USE NEW METHOD

        /// <summary>
        /// Create a comment in the mapsetreview file
        /// </summary>
        /// <param name="message">the message</param>
        /// <param name="type">the type</param>
        //private void CreateSongInfoComment(string message, CommentTypesEnum type)
        //{
        //    string id = Guid.NewGuid().ToString();


        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = 0,
        //        Objects = new(),
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };
        //    List<Comment> comments = plugin.currentMapsetReview.Comments;
        //    comments.Add(comment);
        //    comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //}


        ///// <summary>
        ///// Create a comment in a difficultyreview for a note
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="cube">the cube</param>
        //private void CreateDiffCommentNote(string message, CommentTypesEnum type, Cube cube)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = cube.Time,
        //        PosX = cube.Line,
        //        PosY = cube.Layer,
        //        Color = cube.Type,
        //        ObjectType = ObjectType.Note
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = cube.Time,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Create a comment in a difficultyreview for a note
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="cube">the cube</param>
        //private void CreateDiffCommentLink(string message, CommentTypesEnum type, BaseSlider chainLink)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = chainLink.JsonTime,
        //        PosX = chainLink.PosX,
        //        PosY = chainLink.PosY,
        //        Color = chainLink.Color,
        //        ObjectType = ObjectType.Chain
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = chainLink.JsonTime,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Create a comment in a difficultyreview for a bomb
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="bomb">the bomb</param>
        //private void CreateDiffCommentBomb(string message, CommentTypesEnum type, BaseNote bomb)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = bomb.JsonTime,
        //        PosX = bomb.PosX,
        //        PosY = bomb.PosY,
        //        Color = 3,
        //        ObjectType = bomb.ObjectType
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = bomb.JsonTime,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Create a comment in a difficultyreview for a wall
        ///// </summary>
        ///// <param name="message">the mesasge</param>
        ///// <param name="type">the severity</param>
        ///// <param name="wall">the wall</param>
        //private void CreateDiffCommentObstacle(string message, CommentTypesEnum type, BaseObstacle wall)
        //{
        //    string id = Guid.NewGuid().ToString();

        //    SelectedObject note = new()
        //    {
        //        Beat = wall.JsonTime,
        //        PosX = wall.PosX,
        //        PosY = wall.PosY,
        //        Color = 0,
        //        ObjectType = wall.ObjectType
        //    };

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = wall.JsonTime,
        //        Objects = new() { note },
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        ///// <summary>
        ///// Add another line to the OverallComment in the difficultyreview
        ///// </summary>
        ///// <param name="message">the message</param>
        //private void ExtendOverallComment(string message)
        //{
        //    DifficultyReview review = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault();

        //    review.OverallComment += $" \n{message}";
        //}

        //private bool CheckIfCommentAlreadyExists(Comment comment)
        //{
        //    List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

        //    return comments.Any(c => comment.Message == c.Message && c.Objects.Any(o => String.Equals(o.ToStringFull().ToLower(), comment.Objects.FirstOrDefault().ToStringFull().ToLower(), StringComparison.InvariantCulture)));
        //}

        //private void FuseBombComments()
        //{
        //    List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;
        //    var bombComments = comments.Where(c => c.Objects.All(o => o.Color == 3)).ToList(); // Only bombs comments
        //    for (int i = bombComments.Count() - 2; i >= 0; i--)
        //    {
        //        if (bombComments[i + 1].Message == bombComments[i].Message && bombComments[i + 1].StartBeat >= bombComments[i].StartBeat && bombComments[i + 1].StartBeat <= bombComments[i].StartBeat + 0.25)
        //        {
        //            bombComments[i + 1].Objects.ForEach(o => bombComments[i].Objects.Add(o));
        //            comments.Remove(bombComments[i + 1]);
        //        }
        //    }
        //}

        //private void CreateDiffCommentNotes(string message, CommentTypesEnum type, List<Note> notes)
        //{
        //    if (notes.Count == 0) return;
        //    string id = Guid.NewGuid().ToString();

        //    List<SelectedObject> objects = new();

        //    foreach (var note in notes)
        //    {
        //        objects.Add(new()
        //        {
        //            Beat = note.b,
        //            PosX = note.x,
        //            PosY = note.y,
        //            Color = note.c,
        //            ObjectType = ObjectType.Note
        //        });
        //    }

        //    Comment comment = new()
        //    {
        //        Id = id,
        //        StartBeat = objects.FirstOrDefault().Beat,
        //        Objects = objects,
        //        Type = type,
        //        Message = message,
        //        IsAutogenerated = true
        //    };

        //    List<Comment> comments = plugin.currentMapsetReview.DifficultyReviews.Where(x => x.DifficultyCharacteristic == characteristic && x.DifficultyRank == difficultyRank && x.Difficulty == difficulty).FirstOrDefault().Comments;

        //    if (!CheckIfCommentAlreadyExists(comment))
        //    {
        //        comments.Add(comment);
        //        comments.Sort((a, b) => a.StartBeat.CompareTo(b.StartBeat));
        //    }
        //}

        #endregion
    }
}
*/
