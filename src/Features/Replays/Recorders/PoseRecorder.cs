using ScoreSaber.Features.Live.Replay;
using ScoreSaber.Features.Replays.Format;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace ScoreSaber.Features.Replays.Recorders {
    internal class PoseRecorder : TimeSynchronizer, IInitializable, ITickable {
        private const int ExpectedPoseFramesPerSecond = 144;

        private readonly PlayerTransforms _playerTransforms;
        private readonly LiveReplayStreamingService _liveReplayStreamingService;
        private readonly List<VRPoseGroup> _vrPoseGroup;
        private bool _recording;

        public PoseRecorder(PlayerTransforms playerTransforms, LiveReplayStreamingService liveReplayStreamingService) {

            _playerTransforms = playerTransforms;
            _liveReplayStreamingService = liveReplayStreamingService;
            _vrPoseGroup = new List<VRPoseGroup>();
            _recording = true;
        }

        public void Initialize() {
            float songEndTime = audioTimeSyncController.songEndTime;
            if (songEndTime <= 0f) {
                return;
            }

            int expectedFrames = Mathf.CeilToInt(songEndTime * ExpectedPoseFramesPerSecond);
            if (expectedFrames > _vrPoseGroup.Capacity) {
                _vrPoseGroup.Capacity = expectedFrames;
            }
        }

        public void StopRecording() {
            _recording = false;
        }

        public void Tick() {

            if (!_recording)
                return;

            Vector3 headPosition = _playerTransforms.headPseudoLocalPos;
            Quaternion headRotation = _playerTransforms.headPseudoLocalRot;
            Vector3 leftPosition = _playerTransforms.leftHandPseudoLocalPos;
            Quaternion leftRotation = _playerTransforms.leftHandPseudoLocalRot;
            Vector3 rightPosition = _playerTransforms.rightHandPseudoLocalPos;
            Quaternion rightRotation = _playerTransforms.rightHandPseudoLocalRot;
            float songTime = audioTimeSyncController.songTime;
            float deltaTime = Time.unscaledDeltaTime;

            var frame = new VRPoseGroup() {
                Head = new VRPose() {
                    Position = new VRPosition() {
                        X = headPosition.x, Y = headPosition.y, Z = headPosition.z
                    },
                    Rotation = new VRRotation() {
                        X = headRotation.x, Y = headRotation.y, Z = headRotation.z, W = headRotation.w
                    }
                },
                Left = new VRPose() {
                    Position = new VRPosition() {
                        X = leftPosition.x, Y = leftPosition.y, Z = leftPosition.z
                    },
                    Rotation = new VRRotation() {
                        X = leftRotation.x, Y = leftRotation.y, Z = leftRotation.z, W = leftRotation.w
                    }
                },
                Right = new VRPose() {
                    Position = new VRPosition() {
                        X = rightPosition.x, Y = rightPosition.y, Z = rightPosition.z
                    },
                    Rotation = new VRRotation() {
                        X = rightRotation.x, Y = rightRotation.y, Z = rightRotation.z, W = rightRotation.w
                    }
                },
                Time = songTime,
                FPS = (int)(1f / deltaTime)
            };
            _vrPoseGroup.Add(frame);
            _liveReplayStreamingService.RecordPose(frame);
        }

        public List<VRPoseGroup> Export() {

            return _vrPoseGroup;
        }
    }
}
