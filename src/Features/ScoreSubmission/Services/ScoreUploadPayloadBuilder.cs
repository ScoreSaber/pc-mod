using Newtonsoft.Json;
using ScoreSaber.Core;
using ScoreSaber.Core.Gameplay;
using ScoreSaber.Features.Players.Domain;
using ScoreSaber.Features.ScoreSubmission.Domain;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ScoreSaber.Features.ScoreSubmission.Services {

    internal class ScoreUploadPayloadBuilder {
        private const string UploadSecret = "f0b4a81c9bd3ded1081b365f7628781f";
        private readonly ScoreSaberRuntimeInfo _runtimeInfo;

        public ScoreUploadPayloadBuilder(ScoreSaberRuntimeInfo runtimeInfo) {
            _runtimeInfo = runtimeInfo;
        }

        internal ScoreUploadPayload Build(BeatmapLevel beatmapLevel, BeatmapKey beatmapKey, LevelCompletionResults results, LocalPlayerInfo playerInfo, float playOutcomeTime, ScoreSaberPlayOutcome? playOutcomeOverride) {
            ScoreSaberUploadData scoreData = ScoreSaberUploadData.Create(beatmapLevel, beatmapKey, results, playerInfo, _runtimeInfo.UploadVersionHash, playOutcomeTime, playOutcomeOverride);
            string serializedScore = JsonConvert.SerializeObject(scoreData);
            string key = BuildUploadKey(playerInfo);

            return new ScoreUploadPayload {
                ScoreData = scoreData,
                EncryptedScoreData = BitConverter.ToString(panda(Encoding.UTF8.GetBytes(serializedScore), Encoding.UTF8.GetBytes(key))).Replace("-", string.Empty)
            };
        }

        private static string BuildUploadKey(LocalPlayerInfo playerInfo) {
            byte[] encodedPassword = Encoding.UTF8.GetBytes($"{UploadSecret}-{playerInfo.playerKey}-{playerInfo.playerId}-{UploadSecret}");
            using (var md5 = MD5.Create()) {
                return BitConverter.ToString(md5.ComputeHash(encodedPassword)).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static byte[] panda(byte[] scoreData, byte[] key) {
            int n1 = 11;
            int n2 = 13;
            int ns = 257;

            for (int i = 0; i <= key.Length - 1; i++) {
                ns += ns % (key[i] + 1);
            }

            byte[] encrypted = new byte[scoreData.Length];
            for (int i = 0; i <= scoreData.Length - 1; i++) {
                ns = key[i % key.Length] + ns;
                n1 = (ns + 5) * (n1 & 255) + (n1 >> 8);
                n2 = (ns + 7) * (n2 & 255) + (n2 >> 8);
                ns = ((n1 << 8) + n2) & 255;
                encrypted[i] = (byte)(scoreData[i] ^ (byte)ns);
            }

            return encrypted;
        }
    }

    internal class ScoreUploadPayload {
        internal ScoreSaberUploadData ScoreData { get; set; }
        internal string EncryptedScoreData { get; set; } = string.Empty;
    }
}
