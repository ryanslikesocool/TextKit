// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    [CreateAssetMenu(menuName = "Developed With Love/TextKit/Character Settings")]
    internal class CharacterSettings : ScriptableObject {
        [SerializeField] internal GameObject characterPrefab = null;

#if ODIN_INSPECTOR_3
        [SerializeField, BoxGroup("Transform")] internal bool recenter = true;
        [SerializeField, BoxGroup("Transform")] internal float3 textScale = 1;
        [SerializeField, BoxGroup("Transform")] internal float3 rotation = new float3(0, 180, 0);
        [SerializeField, BoxGroup("Transform")] internal float3 pivot = new float3(0, 0.5f, 0);

        [SerializeField, BoxGroup("Rendering")] internal ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;
        [SerializeField, BoxGroup("Rendering")] internal LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
        [SerializeField, BoxGroup("Rendering")] internal ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.Off;

        [SerializeField, BoxGroup("Font")] internal float characterSpacing = 0.0375f;
        [SerializeField, BoxGroup("Font")] internal float whitespaceWidth = 0.1f;
        [SerializeField, BoxGroup("Font")] internal float lineHeight = 0.06f;
        [SerializeField, BoxGroup("Font")] internal float monospacedWidth = 0.075f;
        [SerializeField, BoxGroup("Font"), Searchable] internal CharacterSet[] characterSets = new CharacterSet[0];

        [SerializeField, BoxGroup("Debug")] internal bool hiddenGameObjects = true;
        [ShowInInspector, BoxGroup("Debug")] internal Dictionary<string, TKCharacter> characterLink = null;
#else
        [Header("Transform"), SerializeField] internal bool recenter = true;
        [SerializeField] internal float3 textScale = 1;
        [SerializeField] internal float3 rotation = new float3(0, 180, 0);
        [SerializeField] internal float3 pivot = new float3(0, 0.5f, 0);

        [Header("Rendering"), SerializeField] internal ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;
        [SerializeField] internal LightProbeUsage lightProbeUsage = LightProbeUsage.Off;
        [SerializeField] internal ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.Off;

        [Header("Font"), SerializeField] internal float characterSpacing = 0.0375f;
        [SerializeField] internal float whitespaceWidth = 0.1f;
        [SerializeField] internal float lineHeight = 0.06f;
        [SerializeField] internal float monospacedWidth = 0.075f;
        [SerializeField] internal CharacterSet[] characterSets = new CharacterSet[0];

        [Header("Debug"), SerializeField] internal bool hiddenGameObjects = true;
        [SerializeField] internal Dictionary<string, TKCharacter> characterLink = null;
#endif

        internal void Dispose() {
            if (characterLink?.Count > 0) {
                foreach (TKCharacter tkChar in characterLink.Values) {
                    tkChar.pool.Clear();
                    DestroyImmediate(tkChar.Prefab);
                }
                characterLink.Clear();
            }
            characterLink = null;
        }

        internal void SetUpCharacterSets() {
            if (characterLink == null) {
                characterLink = new Dictionary<string, TKCharacter>();
                foreach (CharacterSet set in characterSets) {
                    foreach (CharacterData character in set.characters) {
                        if (!characterLink.ContainsKey(character.AccessString)) {
                            TKCharacter newCharacter = new TKCharacter(characterPrefab, character, null, hiddenGameObjects);
                            //newCharacter.RecalculateMesh(recalculateBounds);
                            characterLink.Add(character.AccessString, newCharacter);
                        }
                    }
                }
            }
        }
    }
}