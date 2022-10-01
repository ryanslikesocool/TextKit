// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    [CreateAssetMenu(menuName = "Developed With Love/TextKit/Character Set")]
    internal class CharacterSet : ScriptableObject {
#if ODIN_INSPECTOR_3
        [ListDrawerSettings(Expanded = true), Searchable]
#endif
        [SerializeField] internal CharacterData[] characters = new CharacterData[0];

#if ODIN_INSPECTOR_3
        [Button]
        private void Populate(Mesh[] meshes) {
            if (meshes == null || meshes.Length == 0) {
                return;
            }

            characters = new CharacterData[meshes.Length];
            for (int i = 0; i < meshes.Length; i++) {
                characters[i] = new CharacterData(meshes[i]);
            }
        }
#endif
    }
}