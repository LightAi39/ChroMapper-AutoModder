using BLMapCheck.Classes.Results;
using System;
using System.IO;
using static BLMapCheck.BeatmapScanner.Data.Criteria.InfoCrit;

namespace BLMapCheck.BeatmapScanner.CriteriaCheck.Info
{
    internal static class CoverImage
    {
        private const int MinimumSize = 256;

        // The cover image must be present, at least 256x256, 1:1 and in png, jpg or jpeg format
        public static CritResult Check(CheckContext ctx, string fileName)
        {
            if (ctx.FolderPath == null)
            {
                ctx.Results.CreateInfoResult("Cover Image", Severity.Inconclusive, "SongInfo", "The map was not loaded from a folder, the cover image was not checked");
                return CritResult.Warning;
            }

            var path = string.IsNullOrWhiteSpace(fileName) ? null : Path.Combine(ctx.FolderPath, fileName);
            if (path == null || !File.Exists(path))
            {
                ctx.Results.CreateInfoResult("Cover Image", Severity.Error, "SongInfo", "The cover image is missing",
                    new() { new("FileName", fileName ?? "") });
                return CritResult.Fail;
            }

            var extension = Path.GetExtension(path).ToLowerInvariant();
            var size = ReadImageSize(path);
            if ((extension != ".png" && extension != ".jpg" && extension != ".jpeg") || size == null)
            {
                ctx.Results.CreateInfoResult("Cover Image", Severity.Error, "SongInfo", "The cover image must be a png, jpg or jpeg file",
                    new() { new("FileName", fileName) });
                return CritResult.Fail;
            }

            var (width, height) = size.Value;
            var resolution = new Classes.Results.KeyValuePair("Resolution", width + "x" + height);
            CritResult criteria = CritResult.Success;
            if (width < MinimumSize || height < MinimumSize)
            {
                ctx.Results.CreateInfoResult("Cover Image", Severity.Error, "SongInfo", "The cover image must be at least 256x256 px", new() { resolution });
                criteria = CritResult.Fail;
            }
            if (width != height)
            {
                ctx.Results.CreateInfoResult("Cover Image", Severity.Error, "SongInfo", "The cover image must have a 1:1 aspect ratio", new() { resolution });
                criteria = CritResult.Fail;
            }

            if (criteria == CritResult.Success)
            {
                ctx.Results.CreateInfoResult("Cover Image", Severity.Passed, "SongInfo", "The cover image is valid", new() { resolution });
            }

            return criteria;
        }

        // Width and height from the png or jpeg header, null if the file is neither
        private static (int width, int height)? ReadImageSize(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);
                var header = reader.ReadBytes(8);
                if (header.Length < 8) return null;

                // PNG: signature, then the IHDR chunk starts with the width and height
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                {
                    stream.Position = 16;
                    return (ReadBigEndian(reader, 4), ReadBigEndian(reader, 4));
                }

                // JPEG: go through the segments until a start of frame
                if (header[0] == 0xFF && header[1] == 0xD8)
                {
                    stream.Position = 2;
                    while (stream.Position + 4 <= stream.Length)
                    {
                        if (reader.ReadByte() != 0xFF) return null;
                        byte marker = reader.ReadByte();
                        while (marker == 0xFF) marker = reader.ReadByte(); // Padding
                        if (marker == 0xD8 || (marker >= 0xD0 && marker <= 0xD7) || marker == 0x01) continue; // No length
                        int length = ReadBigEndian(reader, 2);
                        if (length < 2) return null;
                        bool startOfFrame = marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC;
                        if (startOfFrame)
                        {
                            reader.ReadByte(); // Precision
                            int height = ReadBigEndian(reader, 2);
                            int width = ReadBigEndian(reader, 2);
                            return (width, height);
                        }
                        stream.Position += length - 2;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
            return null;
        }

        private static int ReadBigEndian(BinaryReader reader, int bytes)
        {
            int value = 0;
            for (int i = 0; i < bytes; i++)
            {
                value = (value << 8) | reader.ReadByte();
            }
            return value;
        }
    }
}
