using BLMapCheck.Classes.Results;
using Parser.Map.Difficulty.V3.Base;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace BLMapCheck.Classes.Helper
{
    internal class GenericMod
    {
        static public readonly string NumberPattern = @"^\d+([.,])?(\d+)?";
        static public readonly string NumberPattern2 = @"\d+([.,])?(\d+)?";
        static public readonly string SpecialCharPattern = @"^[^\w]+";

        public static void Import(string characteristic, string difficulty, List<string> mod)
        {
            Severity severity = Severity.Info;

            bool loop;
            bool keyword;
            float beat;
            int endIndex;
            string comment;
            string str;
            string substring;
            List<float> beats = new();

            foreach (var line in mod)
            {
                str = line.Trim();
                keyword = false;
                comment = "";

                // SS-style format
                if (line.StartsWith("(X)"))
                {
                    severity = Severity.Error;
                    str = str.Remove(0, 3).Trim();
                }
                else if (line.StartsWith("(?)"))
                {
                    severity = Severity.Inconclusive;
                    str = str.Remove(0, 3).Trim();
                }
                else if (line.StartsWith("(S)"))
                {
                    severity = Severity.Suggestion;
                    str = str.Remove(0, 3).Trim();
                }
                substring = str;
                // Search for first beat
                Match match = Regex.Match(str, NumberPattern2, RegexOptions.Compiled);
                if (match.Success)
                {
                    beat = TryParseFloat(match.Value);
                    if (beat >= 0)
                    {
                        do
                        {
                            loop = false;
                            beats.Add(beat);
                            endIndex = match.Index + match.Length;
                            substring = substring.Substring(endIndex).TrimStart(',', '.').Trim();

                            // If the word "and" is found, get the second number and end loop
                            if (substring.StartsWith("and"))
                            {
                                // Remove and and white space
                                substring = substring.Substring(3).Trim();
                                match = Regex.Match(substring, NumberPattern, RegexOptions.Compiled);
                                if (match.Success)
                                {
                                    keyword = true;
                                    beats.Add(TryParseFloat(match.Value));
                                    endIndex = match.Index + match.Length;
                                    substring = substring.Substring(endIndex).TrimStart(',', '.').Trim();
                                }
                            }
                            else
                            {
                                // Search for more beats on same line
                                match = Regex.Match(substring, NumberPattern, RegexOptions.Compiled);
                                if (match.Success)
                                {
                                    beat = TryParseFloat(match.Value);
                                    if (beat >= 0 && match.Index <= 2) loop = true;
                                }
                            }
                        } while (loop);
                    }

                    // Check for range
                    if (!keyword && substring.StartsWith("to"))
                    {
                        // Remove to and white space
                        string sub = substring.Substring(2).Trim();
                        // Write the whole thing on both beat
                        match = Regex.Match(sub, NumberPattern, RegexOptions.Compiled);
                        if (match.Success)
                        {
                            beats.Add(TryParseFloat(match.Value));
                            comment = str;
                        }
                        else comment = substring.Trim();
                    }
                    else if (!keyword && substring.StartsWith("-"))
                    {
                        // Remove - and white space
                        string sub = substring.Substring(1).Trim();
                        // Write the whole thing on both beat
                        match = Regex.Match(sub, NumberPattern, RegexOptions.Compiled);
                        if (match.Success)
                        {
                            beats.Add(TryParseFloat(match.Value));
                            comment = str;
                        }
                        else comment = substring.Trim();
                    }
                    else comment = substring.Trim();

                    // Remove special symbol and white space
                    comment = Regex.Replace(comment, SpecialCharPattern, "");

                    // BeatLeader difficulty compare format
                    if (line.StartsWith("- Removed"))
                    {
                        comment = comment.Insert(0, "Removed ");
                    }
                    else if (line.StartsWith("+ Added"))
                    {
                        comment = comment.Insert(0, "Added ");
                    }
                    else if (line.StartsWith("/ Modified"))
                    {
                        comment = comment.Insert(0, "Modified ");
                    }

                    DifficultyV3 current = BLMapChecker.map.Difficulties.FirstOrDefault(x => x.Difficulty == difficulty && x.Characteristic == characteristic).Data;

                    foreach (var b in beats)
                    {
                        List<BeatmapObject> beatmapObject = new();
                        beatmapObject.AddRange(current.Notes.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Bombs.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Arcs.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Chains.Where(x => x.Beats == b));
                        beatmapObject.AddRange(current.Walls.Where(x => x.Beats == b));
                        // Create a fake object if necessary, otherwise there won't be any comment
                        if (beatmapObject.Count == 0)
                        {
                            Parser.Map.Difficulty.V3.Grid.Note obj = new()
                            {
                                Beats = b,
                                x = 0,
                                y = 0,
                                Color = 0
                            };
                            beatmapObject.Add(obj);
                        }

                        CheckResults.Instance.CreateDiffResult(characteristic, difficulty, "Mod", severity, "Mod", comment, new(), beatmapObject);
                    }
                    beats.Clear();
                }
            }

            CheckResults.Instance.CheckFinished = true;
        }

        public static float TryParseFloat(string line)
        {
            CultureInfo frenchCulture = new CultureInfo("fr-FR");
            if (float.TryParse(line, NumberStyles.Float, frenchCulture, out float result))
            {
                return result;
            }
            else if (float.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            else
            {
                string periodSeparatedNumber = line.Replace(',', '.');
                if (float.TryParse(periodSeparatedNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}
