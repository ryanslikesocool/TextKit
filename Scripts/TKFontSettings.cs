// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    [CreateAssetMenu(menuName = "Developed With Love/TextKit/Font Settings")]
    public class TKFontSettings : ScriptableObject {
#if ODIN_INSPECTOR_3
        [BoxGroup("Transform")] public float3 textScale = 1;
        [BoxGroup("Transform")] public float3 rotation = 0;

        [BoxGroup("Font")] public float characterSpacing = 0.0375f;
        [BoxGroup("Font")] public float whitespaceWidth = 0.1f;
        [BoxGroup("Font")] public float lineHeight = 0.06f;
        [BoxGroup("Font")] public float monospacedWidth = 0.075f;
        [BoxGroup("Font"), Searchable] public TKCharacterSet[] characterSets = new TKCharacterSet[0];

        [ShowInInspector, BoxGroup("Debug")] internal Dictionary<string, TKCharacter> characterLink = null;
#else
        [Header("Transform")] public float3 textScale = 1;
        public float3 rotation = 0;

        [Header("Font")] public float characterSpacing = 0.0375f;
        public float whitespaceWidth = 0.1f;
        public float lineHeight = 0.06f;
        public float monospacedWidth = 0.075f;
        public TKCharacterSet[] characterSets = new TKCharacterSet[0];

        internal Dictionary<string, TKCharacter> characterLink = null;
#endif

        internal void Dispose() {
            characterLink?.Clear();
            characterLink = null;
        }

        internal void SetUpCharacterSets() {
            if (characterLink == null) {
                characterLink = new Dictionary<string, TKCharacter>();
                foreach (TKCharacterSet set in characterSets) {
                    foreach (TKCharacter character in set.characters) {
                        if (!characterLink.ContainsKey(character.AccessString)) {
                            characterLink.Add(character.AccessString, character);
                        }
                    }
                }
            }
        }
    }
}