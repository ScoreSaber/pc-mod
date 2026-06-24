using Newtonsoft.Json;

namespace ScoreSaber.Core.BeatSaver {
    internal sealed class BeatSaverMap {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("metadata")]
        public BeatSaverMapMetadata Metadata { get; set; }

        [JsonProperty("uploader")]
        public BeatSaverUploader Uploader { get; set; }

        [JsonProperty("versions")]
        public BeatSaverVersion[] Versions { get; set; }
    }

    internal sealed class BeatSaverMapMetadata {
        [JsonProperty("songName")]
        public string SongName { get; set; }

        [JsonProperty("songSubName")]
        public string SongSubName { get; set; }

        [JsonProperty("songAuthorName")]
        public string SongAuthorName { get; set; }

        [JsonProperty("levelAuthorName")]
        public string LevelAuthorName { get; set; }

        [JsonProperty("duration")]
        public float? Duration { get; set; }

        [JsonProperty("bpm")]
        public float? Bpm { get; set; }
    }

    internal sealed class BeatSaverUploader {
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal sealed class BeatSaverVersion {
        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("downloadURL")]
        public string DownloadUrl { get; set; }

        [JsonProperty("coverURL")]
        public string CoverUrl { get; set; }

        [JsonProperty("diffs")]
        public BeatSaverDifficulty[] Diffs { get; set; }
    }

    internal sealed class BeatSaverDifficulty {
        [JsonProperty("difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("characteristic")]
        public string Characteristic { get; set; }

        [JsonProperty("nps")]
        public float? Nps { get; set; }

        [JsonProperty("notes")]
        public int? Notes { get; set; }

        [JsonProperty("obstacles")]
        public int? Obstacles { get; set; }

        [JsonProperty("bombs")]
        public int? Bombs { get; set; }

        [JsonProperty("njs")]
        public float? Njs { get; set; }

        [JsonProperty("offset")]
        public float? Offset { get; set; }
    }
}
