// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    [Serializable]
    internal struct CharacterData {
#if ODIN_INSPECTOR_3
        [SerializeField, HorizontalGroup("H"), LabelWidth(40)] private Mesh mesh;
        [SerializeField, HorizontalGroup("H", Width = 50), LabelWidth(95), LabelText("Override String")] private bool overrideString;
        [SerializeField, HorizontalGroup("H", Width = 0.5f), EnableIf("overrideString"), HideLabel] private string customString;

        public CharacterData(Mesh mesh) {
            this.mesh = mesh;
            overrideString = mesh.name.Length > 1 && mesh.name[0] != '[' && mesh.name[mesh.name.Length - 1] != ']';
            customString = mesh.name;
        }
#else
        [SerializeField] private Mesh mesh;
        [SerializeField] private bool overrideString;
        [SerializeField] private string customString;
#endif

        public Mesh Mesh => mesh;
        public string AccessString => overrideString ? customString : mesh.name;
    }
}