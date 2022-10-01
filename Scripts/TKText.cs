// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    public partial class TKText : MonoBehaviour {
        public delegate void TKTextEvent(TKText sender);
        public event TKTextEvent onTextReady;
        public event TKTextEvent beforeTextCleared;

#if ODIN_INSPECTOR_3
        [BoxGroup("Info"), SerializeField] private CharacterSettings characterSettings = null;
        [BoxGroup("Info"), SerializeField] private Material material = null;

        [BoxGroup("Layout")] public HorizontalAlignment horizontalAlignment = HorizontalAlignment.Leading;
        [BoxGroup("Layout")] public VerticalAlignment verticalAlignment = VerticalAlignment.Bottom;
        [BoxGroup("Layout")] public float sizeMultiplier = 1;
        [BoxGroup("Layout")] public bool recalculateBounds = true;
        [BoxGroup("Layout")] public bool monospaced = false;

        [BoxGroup("Cleanup")] public bool automaticallyCleanText = true;
#else
        [Header("Info"), SerializeField] private CharacterSettings characterSettings = null;
        [SerializeField] private Material material = null;

        [Header("Layout")] public HorizontalAlignment horizontalAlignment = HorizontalAlignment.Leading;
        public VerticalAlignment verticalAlignment = VerticalAlignment.Bottom;
        public float sizeMultiplier = 1;
        public bool recalculateBounds = true;
        public bool monospaced = false;

        [Header("Cleanup")] public bool automaticallyCleanText = false;
#endif

        private float3 SizeMultiplier => characterSettings.textScale * sizeMultiplier;

        [Space, SerializeField, TextArea(1, 10)] private string text = string.Empty;
        public string Text {
            get => text;
            set {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) {
                    ClearText();
                    text = value;
                } else {
                    CreateText(value);
                }
            }
        }

        private MaterialPropertyBlock materialPropertyBlock;
        public MeshRenderer[] Renderers { get; private set; }
#if ODIN_INSPECTOR_3
        [BoxGroup("Debug"), ShowInInspector, ReadOnly] private Dictionary<GameObject, string> activeObjects;
#else
        private Dictionary<GameObject, string> activeObjects;
#endif
        private Dictionary<string, TKCharacter> CharacterLink => characterSettings.characterLink;

        public Material Material {
            get => material;
            set {
                this.material = value;
                if (Renderers != null) {
                    foreach (MeshRenderer r in Renderers) {
                        r.sharedMaterial = value;
                    }
                }
            }
        }

        public MaterialPropertyBlock PropertyBlock {
            get {
                if (materialPropertyBlock == null) {
                    materialPropertyBlock = new MaterialPropertyBlock();
                }
                return materialPropertyBlock;
            }
            set {
                materialPropertyBlock = value;
                if (Renderers != null) {
                    foreach (MeshRenderer r in Renderers) {
                        r?.SetPropertyBlock(value);
                    }
                }
            }
        }

        private void Start() {
            if (text != string.Empty) {
                CreateDefaultText();
            }
        }

        private void OnEnable() {
            VerifyLinks();
            Application.quitting += characterSettings.Dispose;
        }

        private void OnDisable() => Application.quitting -= characterSettings.Dispose;

        public void CreateDefaultText() => CreateText(text);

        private void VerifyLinks() {
            characterSettings.SetUpCharacterSets();
            if (this ?? false && activeObjects == null) {
                activeObjects = new Dictionary<GameObject, string>();
            }
        }

        public void ClearText() {
            beforeTextCleared?.Invoke(this);
            foreach (GameObject key in activeObjects.Keys) {
                CharacterLink[activeObjects[key]].pool.Release(key);
            }
            activeObjects.Clear();
            Renderers = null;
        }

        public void ClearCharacters(int[] indices) => ClearCharacters(indices.Select(i => Renderers[i].gameObject).ToArray());

        public void ClearCharacters(GameObject[] objects) {
            if (objects == null) { return; }
            foreach (GameObject obj in objects) {
                if (activeObjects.ContainsKey(obj)) {
                    CharacterLink[activeObjects[obj]].pool.Release(obj);
                    activeObjects.Remove(obj);
                }
            }
        }

        public void CreateText(string text) {
            if (automaticallyCleanText) {
                ClearText();
            } else {
                activeObjects.Clear();
                Renderers = null;
            }

            this.text = text;

            string[] lines = text.Split('\n');

            float3[] characterPositions = GetCharacterPositions(text, lines);

            GameObject[] createdChars = new GameObject[text.Length];
            int lineStartIndex = 0;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];

                for (int i = 0; i < line.Length; i++) {
                    string character = line[i].ToString();
                    if (!string.IsNullOrWhiteSpace(character)) {
                        if (Extensions.TryGetModifiedCharacter(line, i, out string modifiedCharacter, out int newIndex)) {
                            character = modifiedCharacter;
                            i = newIndex;
                        }

                        GameObject obj = CopyCharacter(character, transform, float3.zero, true, out float extents);
                        if (obj != null) {
                            obj.transform.localScale *= sizeMultiplier;
                            createdChars[lineStartIndex + i] = obj;
                            obj.transform.localPosition = characterPositions[lineStartIndex + i];
                        }
                    }
                }

                lineStartIndex += line.Length;
            }

            Renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (MeshRenderer r in Renderers) {
                r.enabled = true;
            }

            if (materialPropertyBlock != null) {
                PropertyBlock = PropertyBlock;
            }

            onTextReady?.Invoke(this);
        }

        private GameObject CopyCharacter(string source, Transform parent, float3 position, bool active, out float widthExtents) {
            if (CharacterLink.TryGetValue(source, out TKCharacter tkChar)) {
                widthExtents = tkChar.Bounds.extents.x;
                GameObject obj = tkChar.CopyCharacter(characterSettings, parent, position, active, material);
                obj.layer = gameObject.layer;
                activeObjects.Add(obj, source);
                return obj;
            }
            widthExtents = 0;
            Debug.LogWarning($"Missing character \"{source}\"");
            return null;
        }

        public void SetRendererActive(bool state) {
            foreach (MeshRenderer r in Renderers) {
                r.enabled = state;
            }
        }
    }
}