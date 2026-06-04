#pragma warning disable IDE1006 // Naming Styles
using System;
using UnityEngine;

// Old replay "format"
public class Z : MonoBehaviour {

    public class Keyframe {
        public Vector3 _pos1;
        public Vector3 _pos2;
        public Vector3 _pos3;
        public Quaternion _rot1;
        public Quaternion _rot2;
        public Quaternion _rot3;
        public float _time;
        public int combo;
        public int score;
    }

    [Serializable]
    public class SavedData {
        [Serializable]
        public class KeyframeSerializable {
            public float _xPos1;
            public float _yPos1;
            public float _zPos1;

            public float _xPos2;
            public float _yPos2;
            public float _zPos2;

            public float _xPos3;
            public float _yPos3;
            public float _zPos3;

            public float _xRot1;
            public float _yRot1;
            public float _zRot1;
            public float _wRot1;

            public float _xRot2;
            public float _yRot2;
            public float _zRot2;
            public float _wRot2;

            public float _xRot3;
            public float _yRot3;
            public float _zRot3;
            public float _wRot3;

            public float _time;
            public int combo;
            public int score;
        }
        public KeyframeSerializable[] _keyframes;
    }

}
