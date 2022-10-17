// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using UnityEngine;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    [CreateAssetMenu(menuName = "Developed With Love/TextKit/Character Set")]
    public class TKCharacterSet : ScriptableObject {
#if ODIN_INSPECTOR_3
        [ListDrawerSettings(Expanded = true), Searchable]
#endif
        public TKCharacter[] characters = new TKCharacter[0];

        public TKCharacter this[int index] => characters[index];
        public int Length => characters.Length;

#if ODIN_INSPECTOR_3
        [Button]
        private void Populate(Mesh[] meshes) {
            if (meshes == null || meshes.Length == 0) {
                return;
            }

            characters = new TKCharacter[meshes.Length];
            for (int i = 0; i < meshes.Length; i++) {
                characters[i] = new TKCharacter(meshes[i]);
            }
        }
#endif
    }
}