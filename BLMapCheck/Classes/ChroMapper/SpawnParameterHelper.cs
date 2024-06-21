namespace BLMapCheck.Classes.ChroMapper
{
    internal static class SpawnParameterHelper
    {
        public static float CalculateHalfJumpDuration(float noteJumpSpeed, float startBeatOffset, float bpm)
        {
            float num = 4f;
            float num2 = 60f / bpm;
            while (noteJumpSpeed * num2 * num > 17.999f)
            {
                num /= 2f;
            }

            num += startBeatOffset;
            if (num < 0.25f)
            {
                num = 0.25f;
            }

            return num;
        }

        public static float CalculateJumpDistance(float noteJumpSpeed, float startBeatOffset, float bpm)
        {
            float num = 60f / bpm;
            return CalculateHalfJumpDuration(noteJumpSpeed, startBeatOffset, bpm) * num * noteJumpSpeed * 2f;
        }
    }
}
