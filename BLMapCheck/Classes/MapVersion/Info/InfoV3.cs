using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLMapCheck.Classes.MapVersion.Info
{
    public class InfoV3
    {
        public string _version { get; set; }
        public string _songName { get; set; }
        public string _songSubName { get; set; }
        public string _songAuthorName { get; set; }
        public string _levelAuthorName { get; set; }
        public float _beatsPerMinute { get; set; }
        public float _songTimeOffset { get; set; }
        public float _shuffle { get; set; }
        public float _shufflePeriod { get; set; }
        public float _previewStartTime { get; set; }
        public float _previewDuration { get; set; }
        public string _songFilename { get; set; }
        public string _coverImageFilename { get; set; }
        public string _environmentName { get; set; }
        public string _allDirectionsEnvironmentName { get; set; }
        public object[] _environmentNames { get; set; }
        public object[] _colorSchemes { get; set; }
        public _Customdata _customData { get; set; }
        public List<_Difficultybeatmapsets> _difficultyBeatmapSets { get; set; }
    }

    public class _Customdata
    {
        public List<_Contributors> _contributors { get; set; }
        public _Editors _editors { get; set; }
    }

    public class _Editors
    {
        public string _lastEditedBy { get; set; }
        public Chromapper ChroMapper { get; set; }
    }

    public class Chromapper
    {
        public string version { get; set; }
    }

    public class _Contributors
    {
        public string _role { get; set; }
        public string _name { get; set; }
        public string _iconPath { get; set; }
    }

    public class _Difficultybeatmapsets
    {
        public string _beatmapCharacteristicName { get; set; }
        public List<_Difficultybeatmaps> _difficultyBeatmaps { get; set; }
    }

    public class _Difficultybeatmaps
    {
        public string _difficulty { get; set; }
        public int _difficultyRank { get; set; }
        public string _beatmapFilename { get; set; }
        public float _noteJumpMovementSpeed { get; set; }
        public float _noteJumpStartBeatOffset { get; set; }
        public int _beatmapColorSchemeIdx { get; set; }
        public int _environmentNameIdx { get; set; }
        public _Customdata1 _customData { get; set; }
    }

    public class _Customdata1
    {
        public string _difficultyLabel { get; set; }
        public List<string> _requirements { get; set; }
        public List<string> _suggestions { get; set; }
        public _Colorleft _colorLeft { get; set; }
        public _Colorright _colorRight { get; set; }
    }

    public class _Colorleft
    {
        public float r { get; set; }
        public float g { get; set; }
        public float b { get; set; }
    }

    public class _Colorright
    {
        public float r { get; set; }
        public float g { get; set; }
        public float b { get; set; }
    }
}
