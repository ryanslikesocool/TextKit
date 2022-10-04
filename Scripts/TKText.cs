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

        private MaterialPropertyBlock materialPropertyBlock;
#if ODIN_INSPECTOR_3
        [BoxGroup("Debug"), ShowInInspector, ReadOnly] public MeshRenderer[] CharacterRenderers { get; private set; }
        [BoxGroup("Debug"), ShowInInspector, ReadOnly] private Dictionary<GameObject, string> activeObjects;
#else
        public MeshRenderer[] CharacterRenderers { get; private set; }
        private Dictionary<GameObject, string> activeObjects;
#endif
        private Dictionary<string, TKCharacter> CharacterLink => characterSettings.characterLink;

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

        public void CreateText(string text) {
            if (automaticallyCleanText) {
                ClearText();
            } else {
                activeObjects.Clear();
                CharacterRenderers = null;
            }

            this.text = text;

            CharacterRenderers = CreateInitialText();

            if (materialPropertyBlock != null) {
                PropertyBlock = PropertyBlock;
            }

            onTextReady?.Invoke(this);
        }

        private MeshRenderer[] CreateInitialText() {
            string[] lines = text.Split('\n');
            float3[] characterPositions = GetCharacterPositions(text, lines);

            int lineStartIndex = 0;

            int indexCounter = 0;
            GameObject[] createdObjects = new GameObject[text.Length];

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];

                for (int i = 0; i < line.Length; i++) {
                    string character = line[i].ToString();
                    if (!string.IsNullOrWhiteSpace(character)) {
                        if (Extensions.TryGetModifiedCharacter(line, i, out string modifiedCharacter, out int newIndex)) {
                            character = modifiedCharacter;
                            i = newIndex;
                        }

                        GameObject obj = CopyCharacter(character, transform, float3.zero, true, out _);
                        if (obj != null) {
                            createdObjects[indexCounter] = obj;
                            obj.transform.localScale *= sizeMultiplier;
                            obj.transform.localPosition = characterPositions[lineStartIndex + i];
                        }
                    }
                    indexCounter++;
                }

                lineStartIndex += line.Length;
            }

            return createdObjects.Select(obj => obj?.GetComponent<MeshRenderer>() ?? null).ToArray();
        }

        public void RecomputeTextLayout() {
            string[] lines = text.Split('\n');
            float3[] characterPositions = GetCharacterPositions(text, lines);

            int lineStartIndex = 0;
            int indexCounter = 0;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];

                for (int i = 0; i < line.Length; i++) {
                    if (!char.IsWhiteSpace(line[i])) {
                        if (Extensions.TryGetModifiedCharacter(line, i, out _, out int newIndex)) {
                            i = newIndex;
                        }

                        GameObject obj = CharacterRenderers[indexCounter].gameObject;
                        if (obj != null) {
                            obj.transform.localScale = SizeMultiplier;
                            obj.transform.localPosition = characterPositions[lineStartIndex + i];
                        }
                    }
                    indexCounter++;
                }

                lineStartIndex += line.Length;
            }
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
            foreach (MeshRenderer r in CharacterRenderers) {
                r.enabled = state;
            }
        }
    }

    // MARK: - Clear

    public partial class TKText {
        public void ClearText() {
            beforeTextCleared?.Invoke(this);
            foreach (GameObject key in activeObjects.Keys) {
                CharacterLink[activeObjects[key]].pool.Release(key);
            }
            activeObjects.Clear();
            CharacterRenderers = null;
        }

        public void ClearCharacters(int[] indices) => ClearCharacters(indices.Select(i => CharacterRenderers[i].gameObject).ToArray());

        public void ClearCharacters(GameObject[] objects) {
            if (objects == null) { return; }
            foreach (GameObject obj in objects) {
                if (activeObjects.ContainsKey(obj)) {
                    CharacterLink[activeObjects[obj]].pool.Release(obj);
                    activeObjects.Remove(obj);
                }
            }
        }
    }

    // MARK: - Property Access

    public partial class TKText {
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

        public Material Material {
            get => material;
            set {
                this.material = value;
                if (CharacterRenderers != null) {
                    foreach (MeshRenderer r in CharacterRenderers) {
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
                if (CharacterRenderers != null) {
                    foreach (MeshRenderer r in CharacterRenderers) {
                        r?.SetPropertyBlock(value);
                    }
                }
            }
        }

        /*
                public float SizeMultiplier {
                    get => sizeMultiplier;
                    set {
                        sizeMultiplier = value;
                    }
                }
                */
    }
}