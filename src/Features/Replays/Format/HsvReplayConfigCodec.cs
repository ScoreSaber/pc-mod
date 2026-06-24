using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ScoreSaber.Features.Replays.Format {
    internal static class HsvReplayConfigCodec {
        internal const int MaxJsonBytes = 32 * 1024;
        internal const int MaxPayloadBytes = 8 * 1024;

        private const int LatestSupportedMajor = 3;
        private const int LatestSupportedMinor = 7;
        private const int MaxListItems = 32;
        private const int MaxStringBytes = 512;

        private const byte FlagFixedPosition = 1 << 0;
        private const byte FlagTargetPositionOffset = 1 << 1;
        private const byte FlagDoIntermediateUpdates = 1 << 2;
        private const byte FlagAssumeMaxPostSwing = 1 << 3;
        private const byte FlagChainLinkDisplay = 1 << 4;
        private const byte FlagRandomizeBadCutDisplays = 1 << 5;
        private const byte FlagRandomizeMissDisplays = 1 << 6;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter> {
                new StringEnumConverter(),
                new HsvColorConverter(),
                new HsvVector3Converter()
            }
        };

        internal static bool TryEncodeJson(string json, out byte[] payload, out string failure) {

            payload = null;
            failure = null;

            HsvConfigJson config;
            try {
                config = JsonConvert.DeserializeObject<HsvConfigJson>(json, JsonSettings);
            } catch (Exception ex) {
                failure = "failed to parse HSV config: " + ex.Message;
                return false;
            }

            if (!TryNormalize(config, out HsvReplayConfig replayConfig, out failure)) {
                return false;
            }

            payload = Encode(replayConfig);
            if (payload.Length > MaxPayloadBytes) {
                failure = "HSV config payload is too large";
                payload = null;
                return false;
            }

            return true;
        }

        private static bool TryNormalize(HsvConfigJson input, out HsvReplayConfig config, out string failure) {

            config = null;
            failure = null;

            if (input == null) {
                failure = "HSV config is empty";
                return false;
            }

            if (input.MajorVersion != LatestSupportedMajor || input.MinorVersion > LatestSupportedMinor) {
                failure = "unsupported HSV config version";
                return false;
            }

            if (!Enum.IsDefined(typeof(HsvDisplayMode), input.DisplayMode)) {
                failure = "unsupported HSV display mode";
                return false;
            }

            var judgments = NormalizeJudgments(input.Judgments, "judgments", out failure);
            if (judgments == null) {
                return false;
            }

            List<HsvReplayJudgment> chainHeadJudgments;
            if (input.ChainHeadJudgments == null || input.ChainHeadJudgments.Count == 0) {
                chainHeadJudgments = DefaultChainHeadJudgments();
            } else {
                chainHeadJudgments = NormalizeJudgments(input.ChainHeadJudgments, "chain head judgments", out failure);
                if (chainHeadJudgments == null) {
                    return false;
                }
            }

            HsvReplayColoredText chainLinkDisplay = NormalizeColoredText(input.ChainLinkDisplay, "chain link display", out failure);
            if (failure != null) {
                return false;
            }
            List<HsvReplaySegment> beforeCutAngleJudgments = NormalizeSegments(input.BeforeCutAngleJudgments, "before cut angle judgments", out failure);
            if (beforeCutAngleJudgments == null) {
                return false;
            }
            List<HsvReplaySegment> accuracyJudgments = NormalizeSegments(input.AccuracyJudgments, "accuracy judgments", out failure);
            if (accuracyJudgments == null) {
                return false;
            }
            List<HsvReplaySegment> afterCutAngleJudgments = NormalizeSegments(input.AfterCutAngleJudgments, "after cut angle judgments", out failure);
            if (afterCutAngleJudgments == null) {
                return false;
            }
            List<HsvReplayTimeSegment> timeDependenceJudgments = NormalizeTimeSegments(input.TimeDependenceJudgments, "time dependence judgments", out failure);
            if (timeDependenceJudgments == null) {
                return false;
            }
            List<HsvReplayBadCutDisplay> badCutDisplays = NormalizeBadCutDisplays(input.BadCutDisplays, out failure);
            if (badCutDisplays == null) {
                return false;
            }
            List<HsvReplayColoredText> missDisplays = NormalizeMissDisplays(input.MissDisplays, out failure);
            if (missDisplays == null) {
                return false;
            }

            var normalized = new HsvReplayConfig {
                MajorVersion = ToVersionByte(input.MajorVersion),
                MinorVersion = ToVersionByte(input.MinorVersion),
                PatchVersion = ToVersionByte(input.PatchVersion),
                DisplayMode = input.DisplayMode,
                FixedPosition = input.FixedPosition,
                TargetPositionOffset = input.TargetPositionOffset,
                DoIntermediateUpdates = input.DoIntermediateUpdates,
                AssumeMaxPostSwing = input.AssumeMaxPostSwing,
                Judgments = judgments,
                ChainHeadJudgments = chainHeadJudgments,
                ChainLinkDisplay = chainLinkDisplay,
                BeforeCutAngleJudgments = beforeCutAngleJudgments,
                AccuracyJudgments = accuracyJudgments,
                AfterCutAngleJudgments = afterCutAngleJudgments,
                TimeDependenceDecimalPrecision = input.TimeDependenceDecimalPrecision,
                TimeDependenceDecimalOffset = input.TimeDependenceDecimalOffset,
                TimeDependenceJudgments = timeDependenceJudgments,
                RandomizeBadCutDisplays = input.RandomizeBadCutDisplays,
                BadCutDisplays = badCutDisplays,
                RandomizeMissDisplays = input.RandomizeMissDisplays,
                MissDisplays = missDisplays
            };

            if (normalized.TimeDependenceDecimalPrecision < 0 || normalized.TimeDependenceDecimalPrecision > 99) {
                failure = "HSV time dependence decimal precision is out of range";
                return false;
            }
            if (normalized.TimeDependenceDecimalOffset < 0 || normalized.TimeDependenceDecimalOffset > 38) {
                failure = "HSV time dependence decimal offset is out of range";
                return false;
            }

            config = normalized;
            return true;
        }

        private static List<HsvReplayJudgment> NormalizeJudgments(List<HsvJudgmentJson> source, string name, out string failure) {

            failure = null;
            if (source == null || source.Count == 0) {
                failure = "HSV " + name + " are empty";
                return null;
            }
            if (source.Count > MaxListItems) {
                failure = "HSV " + name + " contain too many entries";
                return null;
            }
            if (source.Any(value => value == null)) {
                failure = "HSV " + name + " contain an empty entry";
                return null;
            }

            var values = source
                .OrderByDescending(value => value.Threshold)
                .Select(value => new HsvReplayJudgment {
                    Threshold = value.Threshold,
                    Text = value.Text ?? string.Empty,
                    Color = value.Color ?? HsvReplayColor.White,
                    Fade = value.Fade
                })
                .ToList();

            if (values[0].Fade) {
                failure = "first HSV " + name + " entry cannot fade";
                return null;
            }

            if (!ValidateThresholds(values.Select(value => value.Threshold), name, out failure)) {
                return null;
            }

            foreach (HsvReplayJudgment value in values) {
                if (!ValidateString(value.Text, name, out failure)) {
                    return null;
                }
            }

            return values;
        }

        private static HsvReplayColoredText NormalizeColoredText(HsvColoredTextJson source, string name, out string failure) {

            failure = null;
            if (source == null) {
                return null;
            }

            string text = source.Text ?? string.Empty;
            if (!ValidateString(text, name, out failure)) {
                return null;
            }

            return new HsvReplayColoredText {
                Text = text,
                Color = source.Color ?? HsvReplayColor.White
            };
        }

        private static List<HsvReplaySegment> NormalizeSegments(List<HsvSegmentJson> source, string name, out string failure) {

            failure = null;
            if (source == null || source.Count == 0) {
                return new List<HsvReplaySegment>();
            }
            if (source.Count > MaxListItems) {
                failure = "HSV " + name + " contain too many entries";
                return null;
            }
            if (source.Any(value => value == null)) {
                failure = "HSV " + name + " contain an empty entry";
                return null;
            }

            var values = source
                .OrderByDescending(value => value.Threshold)
                .Select(value => new HsvReplaySegment {
                    Threshold = value.Threshold,
                    Text = value.Text ?? string.Empty
                })
                .ToList();

            if (!ValidateThresholds(values.Select(value => value.Threshold), name, out failure)) {
                return null;
            }

            foreach (HsvReplaySegment value in values) {
                if (!ValidateString(value.Text, name, out failure)) {
                    return null;
                }
            }

            return values;
        }

        private static List<HsvReplayTimeSegment> NormalizeTimeSegments(List<HsvTimeSegmentJson> source, string name, out string failure) {

            failure = null;
            if (source == null || source.Count == 0) {
                return new List<HsvReplayTimeSegment>();
            }
            if (source.Count > MaxListItems) {
                failure = "HSV " + name + " contain too many entries";
                return null;
            }
            if (source.Any(value => value == null)) {
                failure = "HSV " + name + " contain an empty entry";
                return null;
            }

            var values = source
                .OrderByDescending(value => value.Threshold)
                .Select(value => new HsvReplayTimeSegment {
                    Threshold = value.Threshold,
                    Text = value.Text ?? string.Empty
                })
                .ToList();

            if (values.Any(value => float.IsNaN(value.Threshold) || float.IsInfinity(value.Threshold))) {
                failure = "HSV " + name + " contain a non-finite threshold";
                return null;
            }
            for (int i = 1; i < values.Count; i++) {
                if (values[i - 1].Threshold == values[i].Threshold) {
                    failure = "HSV " + name + " contain duplicate thresholds";
                    return null;
                }
            }
            foreach (HsvReplayTimeSegment value in values) {
                if (!ValidateString(value.Text, name, out failure)) {
                    return null;
                }
            }

            return values;
        }

        private static List<HsvReplayBadCutDisplay> NormalizeBadCutDisplays(List<HsvBadCutDisplayJson> source, out string failure) {

            failure = null;
            if (source == null || source.Count == 0) {
                return new List<HsvReplayBadCutDisplay>();
            }
            if (source.Count > MaxListItems) {
                failure = "HSV bad cut displays contain too many entries";
                return null;
            }
            if (source.Any(value => value == null)) {
                failure = "HSV bad cut displays contain an empty entry";
                return null;
            }

            var values = new List<HsvReplayBadCutDisplay>(source.Count);
            foreach (HsvBadCutDisplayJson value in source) {
                string text = value.Text ?? string.Empty;
                if (!ValidateString(text, "bad cut displays", out failure)) {
                    return null;
                }
                values.Add(new HsvReplayBadCutDisplay {
                    Text = text,
                    Color = value.Color ?? HsvReplayColor.White,
                    Type = value.Type.HasValue && Enum.IsDefined(typeof(HsvBadCutDisplayType), value.Type.Value) ? value.Type.Value : HsvBadCutDisplayType.All
                });
            }
            return values;
        }

        private static List<HsvReplayColoredText> NormalizeMissDisplays(List<HsvColoredTextJson> source, out string failure) {

            failure = null;
            if (source == null || source.Count == 0) {
                return new List<HsvReplayColoredText>();
            }
            if (source.Count > MaxListItems) {
                failure = "HSV miss displays contain too many entries";
                return null;
            }
            if (source.Any(value => value == null)) {
                failure = "HSV miss displays contain an empty entry";
                return null;
            }

            var values = new List<HsvReplayColoredText>(source.Count);
            foreach (HsvColoredTextJson value in source) {
                HsvReplayColoredText text = NormalizeColoredText(value, "miss displays", out failure);
                if (failure != null) {
                    return null;
                }
                values.Add(text);
            }
            return values;
        }

        private static bool ValidateThresholds(IEnumerable<int> thresholds, string name, out string failure) {

            failure = null;
            int? previous = null;
            var seen = new HashSet<int>();
            foreach (int threshold in thresholds) {
                if (threshold < 0 || threshold > ushort.MaxValue) {
                    failure = "HSV " + name + " contain an out of range threshold";
                    return false;
                }
                if (previous.HasValue && threshold > previous.Value) {
                    failure = "HSV " + name + " are not descending";
                    return false;
                }
                if (!seen.Add(threshold)) {
                    failure = "HSV " + name + " contain duplicate thresholds";
                    return false;
                }
                previous = threshold;
            }
            return true;
        }

        private static bool ValidateString(string value, string name, out string failure) {

            failure = null;
            if (Encoding.UTF8.GetByteCount(value ?? string.Empty) > MaxStringBytes) {
                failure = "HSV " + name + " contain text that is too long";
                return false;
            }
            return true;
        }

        private static List<HsvReplayJudgment> DefaultChainHeadJudgments() {

            return new List<HsvReplayJudgment> {
                new HsvReplayJudgment {
                    Threshold = 0,
                    Text = "%s",
                    Color = HsvReplayColor.White,
                    Fade = false
                }
            };
        }

        private static byte[] Encode(HsvReplayConfig config) {

            using (var stream = new MemoryStream()) {
                stream.WriteByte(config.MajorVersion);
                stream.WriteByte(config.MinorVersion);
                stream.WriteByte(config.PatchVersion);
                stream.WriteByte((byte)config.DisplayMode);

                byte flags = 0;
                if (config.FixedPosition.HasValue) flags |= FlagFixedPosition;
                if (config.TargetPositionOffset.HasValue) flags |= FlagTargetPositionOffset;
                if (config.DoIntermediateUpdates) flags |= FlagDoIntermediateUpdates;
                if (config.AssumeMaxPostSwing) flags |= FlagAssumeMaxPostSwing;
                if (config.ChainLinkDisplay != null) flags |= FlagChainLinkDisplay;
                if (config.RandomizeBadCutDisplays) flags |= FlagRandomizeBadCutDisplays;
                if (config.RandomizeMissDisplays) flags |= FlagRandomizeMissDisplays;
                stream.WriteByte(flags);

                if (config.FixedPosition.HasValue) WriteVector(config.FixedPosition.Value, stream);
                if (config.TargetPositionOffset.HasValue) WriteVector(config.TargetPositionOffset.Value, stream);

                stream.WriteByte((byte)config.TimeDependenceDecimalPrecision);
                stream.WriteByte((byte)config.TimeDependenceDecimalOffset);

                WriteJudgments(config.Judgments, stream);
                WriteJudgments(config.ChainHeadJudgments, stream);
                if (config.ChainLinkDisplay != null) WriteColoredText(config.ChainLinkDisplay, stream);
                WriteSegments(config.BeforeCutAngleJudgments, stream);
                WriteSegments(config.AccuracyJudgments, stream);
                WriteSegments(config.AfterCutAngleJudgments, stream);
                WriteTimeSegments(config.TimeDependenceJudgments, stream);
                WriteBadCutDisplays(config.BadCutDisplays, stream);
                WriteColoredTexts(config.MissDisplays, stream);
                return stream.ToArray();
            }
        }

        private static void WriteJudgments(List<HsvReplayJudgment> values, MemoryStream stream) {

            stream.WriteByte((byte)values.Count);
            foreach (HsvReplayJudgment value in values) {
                WriteUShort(value.Threshold, stream);
                WriteString(value.Text, stream);
                WriteColor(value.Color, stream);
                stream.WriteByte(value.Fade ? (byte)1 : (byte)0);
            }
        }

        private static void WriteSegments(List<HsvReplaySegment> values, MemoryStream stream) {

            stream.WriteByte((byte)values.Count);
            foreach (HsvReplaySegment value in values) {
                WriteUShort(value.Threshold, stream);
                WriteString(value.Text, stream);
            }
        }

        private static void WriteTimeSegments(List<HsvReplayTimeSegment> values, MemoryStream stream) {

            stream.WriteByte((byte)values.Count);
            foreach (HsvReplayTimeSegment value in values) {
                WriteFloat(value.Threshold, stream);
                WriteString(value.Text, stream);
            }
        }

        private static void WriteBadCutDisplays(List<HsvReplayBadCutDisplay> values, MemoryStream stream) {

            stream.WriteByte((byte)values.Count);
            foreach (HsvReplayBadCutDisplay value in values) {
                WriteString(value.Text, stream);
                WriteColor(value.Color, stream);
                stream.WriteByte((byte)value.Type);
            }
        }

        private static void WriteColoredTexts(List<HsvReplayColoredText> values, MemoryStream stream) {

            stream.WriteByte((byte)values.Count);
            foreach (HsvReplayColoredText value in values) {
                WriteColoredText(value, stream);
            }
        }

        private static void WriteColoredText(HsvReplayColoredText value, MemoryStream stream) {

            WriteString(value.Text, stream);
            WriteColor(value.Color, stream);
        }

        private static void WriteVector(HsvReplayVector3 value, MemoryStream stream) {

            WriteFloat(value.X, stream);
            WriteFloat(value.Y, stream);
            WriteFloat(value.Z, stream);
        }

        private static void WriteColor(HsvReplayColor value, MemoryStream stream) {

            stream.WriteByte(ToColorByte(value.R));
            stream.WriteByte(ToColorByte(value.G));
            stream.WriteByte(ToColorByte(value.B));
            stream.WriteByte(ToColorByte(value.A));
        }

        private static void WriteString(string value, MemoryStream stream) {

            byte[] bytes = Encoding.UTF8.GetBytes(value ?? string.Empty);
            WriteUShort(bytes.Length, stream);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteUShort(int value, MemoryStream stream) {

            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }

        private static void WriteFloat(float value, MemoryStream stream) {

            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static byte ToColorByte(float value) {

            if (float.IsNaN(value) || float.IsInfinity(value)) {
                return 255;
            }
            value = Math.Max(0f, Math.Min(1f, value));
            return (byte)Math.Round(value * 255f);
        }

        private static byte ToVersionByte(ulong value) {

            return value > byte.MaxValue ? byte.MaxValue : (byte)value;
        }

        private class HsvConfigJson {
            public ulong MajorVersion { get; set; }
            public ulong MinorVersion { get; set; }
            public ulong PatchVersion { get; set; }
            public HsvDisplayMode DisplayMode { get; set; } = HsvDisplayMode.Numeric;
            public HsvReplayVector3? FixedPosition { get; set; }
            public HsvReplayVector3? TargetPositionOffset { get; set; }
            public bool DoIntermediateUpdates { get; set; } = true;
            public bool AssumeMaxPostSwing { get; set; }
            public List<HsvJudgmentJson> Judgments { get; set; }
            public List<HsvJudgmentJson> ChainHeadJudgments { get; set; }
            public HsvColoredTextJson ChainLinkDisplay { get; set; }
            public List<HsvSegmentJson> BeforeCutAngleJudgments { get; set; }
            public List<HsvSegmentJson> AccuracyJudgments { get; set; }
            public List<HsvSegmentJson> AfterCutAngleJudgments { get; set; }
            public int TimeDependenceDecimalPrecision { get; set; } = 1;
            public int TimeDependenceDecimalOffset { get; set; } = 2;
            public List<HsvTimeSegmentJson> TimeDependenceJudgments { get; set; }
            public bool RandomizeBadCutDisplays { get; set; } = true;
            public List<HsvBadCutDisplayJson> BadCutDisplays { get; set; }
            public bool RandomizeMissDisplays { get; set; } = true;
            public List<HsvColoredTextJson> MissDisplays { get; set; }
        }

        private class HsvJudgmentJson {
            public int Threshold { get; set; }
            public string Text { get; set; }
            public HsvReplayColor? Color { get; set; }
            public bool Fade { get; set; }
        }

        private class HsvColoredTextJson {
            public string Text { get; set; }
            public HsvReplayColor? Color { get; set; }
        }

        private class HsvSegmentJson {
            public int Threshold { get; set; }
            public string Text { get; set; }
        }

        private class HsvTimeSegmentJson {
            public float Threshold { get; set; }
            public string Text { get; set; }
        }

        private class HsvBadCutDisplayJson : HsvColoredTextJson {
            public HsvBadCutDisplayType? Type { get; set; } = HsvBadCutDisplayType.All;
        }

        private class HsvReplayConfig {
            public byte MajorVersion { get; set; }
            public byte MinorVersion { get; set; }
            public byte PatchVersion { get; set; }
            public HsvDisplayMode DisplayMode { get; set; }
            public HsvReplayVector3? FixedPosition { get; set; }
            public HsvReplayVector3? TargetPositionOffset { get; set; }
            public bool DoIntermediateUpdates { get; set; }
            public bool AssumeMaxPostSwing { get; set; }
            public List<HsvReplayJudgment> Judgments { get; set; }
            public List<HsvReplayJudgment> ChainHeadJudgments { get; set; }
            public HsvReplayColoredText ChainLinkDisplay { get; set; }
            public List<HsvReplaySegment> BeforeCutAngleJudgments { get; set; }
            public List<HsvReplaySegment> AccuracyJudgments { get; set; }
            public List<HsvReplaySegment> AfterCutAngleJudgments { get; set; }
            public int TimeDependenceDecimalPrecision { get; set; }
            public int TimeDependenceDecimalOffset { get; set; }
            public List<HsvReplayTimeSegment> TimeDependenceJudgments { get; set; }
            public bool RandomizeBadCutDisplays { get; set; }
            public List<HsvReplayBadCutDisplay> BadCutDisplays { get; set; }
            public bool RandomizeMissDisplays { get; set; }
            public List<HsvReplayColoredText> MissDisplays { get; set; }
        }

        private class HsvReplayJudgment {
            public int Threshold { get; set; }
            public string Text { get; set; }
            public HsvReplayColor Color { get; set; }
            public bool Fade { get; set; }
        }

        private class HsvReplaySegment {
            public int Threshold { get; set; }
            public string Text { get; set; }
        }

        private class HsvReplayTimeSegment {
            public float Threshold { get; set; }
            public string Text { get; set; }
        }

        private class HsvReplayColoredText {
            public string Text { get; set; }
            public HsvReplayColor Color { get; set; }
        }

        private class HsvReplayBadCutDisplay : HsvReplayColoredText {
            public HsvBadCutDisplayType Type { get; set; }
        }

        private struct HsvReplayVector3 {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        private struct HsvReplayColor {
            public static readonly HsvReplayColor White = new HsvReplayColor { R = 1f, G = 1f, B = 1f, A = 1f };
            public float R { get; set; }
            public float G { get; set; }
            public float B { get; set; }
            public float A { get; set; }
        }

        private enum HsvDisplayMode : byte {
            None = 0,
            Format = 1,
            TextOnly = 2,
            Numeric = 3,
            ScoreOnTop = 4,
            Directions = 5
        }

        private enum HsvBadCutDisplayType : byte {
            All = 0,
            WrongDirection = 1,
            WrongColor = 2,
            Bomb = 3
        }

        private class HsvColorConverter : JsonConverter {
            public override bool CanConvert(Type objectType) {
                return objectType == typeof(HsvReplayColor) || Nullable.GetUnderlyingType(objectType) == typeof(HsvReplayColor);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                if (reader.TokenType == JsonToken.Null) {
                    return Nullable.GetUnderlyingType(objectType) == typeof(HsvReplayColor) ? null : (object)HsvReplayColor.White;
                }

                JToken token = JToken.Load(reader);
                if (token.Type == JTokenType.Array) {
                    JArray array = (JArray)token;
                    if (array.Count >= 4) {
                        return new HsvReplayColor {
                            R = array[0].Value<float>(),
                            G = array[1].Value<float>(),
                            B = array[2].Value<float>(),
                            A = array[3].Value<float>()
                        };
                    }
                } else if (token.Type == JTokenType.Object) {
                    return new HsvReplayColor {
                        R = token["r"]?.Value<float>() ?? token["R"]?.Value<float>() ?? 1f,
                        G = token["g"]?.Value<float>() ?? token["G"]?.Value<float>() ?? 1f,
                        B = token["b"]?.Value<float>() ?? token["B"]?.Value<float>() ?? 1f,
                        A = token["a"]?.Value<float>() ?? token["A"]?.Value<float>() ?? 1f
                    };
                }

                throw new JsonSerializationException("Invalid HSV color");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                throw new NotSupportedException();
            }
        }

        private class HsvVector3Converter : JsonConverter {
            public override bool CanConvert(Type objectType) {
                return objectType == typeof(HsvReplayVector3) || Nullable.GetUnderlyingType(objectType) == typeof(HsvReplayVector3);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                if (reader.TokenType == JsonToken.Null) {
                    return Nullable.GetUnderlyingType(objectType) == typeof(HsvReplayVector3) ? null : (object)new HsvReplayVector3();
                }

                JToken token = JToken.Load(reader);
                if (token.Type == JTokenType.Object) {
                    return new HsvReplayVector3 {
                        X = token["x"]?.Value<float>() ?? token["X"]?.Value<float>() ?? 0f,
                        Y = token["y"]?.Value<float>() ?? token["Y"]?.Value<float>() ?? 0f,
                        Z = token["z"]?.Value<float>() ?? token["Z"]?.Value<float>() ?? 0f
                    };
                }
                if (token.Type == JTokenType.Array) {
                    JArray array = (JArray)token;
                    if (array.Count >= 3) {
                        return new HsvReplayVector3 {
                            X = array[0].Value<float>(),
                            Y = array[1].Value<float>(),
                            Z = array[2].Value<float>()
                        };
                    }
                }

                throw new JsonSerializationException("Invalid HSV vector");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                throw new NotSupportedException();
            }
        }
    }
}
