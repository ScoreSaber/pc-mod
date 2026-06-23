namespace ScoreSaber.Features.Live.Compete.Domain {
    internal class CompeteSongSelection {
        internal string Name { get; }
        internal string Mapper { get; }
        internal string Difficulty { get; }
        internal string Characteristic { get; }
        internal string CoverSource { get; }
        internal string Duration { get; }
        internal string Bpm { get; }
        internal string Nps { get; }
        internal string Notes { get; }
        internal string Obstacles { get; }
        internal string Bombs { get; }
        internal string Njs { get; }
        internal string JumpDistance { get; }
        internal string Stars { get; }
        internal string MapHash { get; }
        internal string DownloadUrl { get; }
        internal BeatmapLevel BeatmapLevel { get; }
        internal BeatmapKey BeatmapKey { get; }

        internal CompeteSongSelection(
            BeatmapLevel beatmapLevel,
            BeatmapKey beatmapKey,
            string name,
            string mapper,
            string difficulty,
            string characteristic,
            string coverSource,
            string duration,
            string bpm,
            string nps,
            string notes,
            string obstacles,
            string bombs,
            string njs,
            string jumpDistance,
            string stars,
            string mapHash = "",
            string downloadUrl = "") {

            BeatmapLevel = beatmapLevel;
            BeatmapKey = beatmapKey;
            Name = name;
            Mapper = mapper;
            Difficulty = difficulty;
            Characteristic = characteristic;
            CoverSource = coverSource;
            Duration = duration;
            Bpm = bpm;
            Nps = nps;
            Notes = notes;
            Obstacles = obstacles;
            Bombs = bombs;
            Njs = njs;
            JumpDistance = jumpDistance;
            Stars = stars;
            MapHash = mapHash ?? string.Empty;
            DownloadUrl = downloadUrl ?? string.Empty;
        }
    }
}
