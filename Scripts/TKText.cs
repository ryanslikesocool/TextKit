// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    public partial class TKText : MonoBehaviour {
        [Space, SerializeField, TextArea(1, 10)] private string text = string.Empty;

#if ODIN_INSPECTOR_3
        [BoxGroup("Info"), SerializeField] private TKFontSettings characterSettings = null;

        [BoxGroup("Rendering"), SerializeField] private Material material = null;
        [BoxGroup("Rendering"), SerializeField] private TKCharacterRenderer rendererPrefab = null;
#else
        [Header("Info"), SerializeField] private TKFontSettings characterSettings = null;

        [Header("Rendering"), SerializeField] private Material material = null;
        [SerializeField] private TKCharacterRenderer rendererPrefab = null;
#endif

        private MaterialPropertyBlock materialPropertyBlock;
        private Dictionary<string, TKCharacter> CharacterLink => characterSettings.characterLink;

        protected virtual void Start() {
            if (text != string.Empty) {
                CreateDefaultText();
            }
        }

        protected virtual void OnEnable() {
            characterRendererPool = new ObjectPool<TKCharacterRenderer>(
                createFunc: OnPoolCreate,
                actionOnRelease: OnPoolRelease,
                actionOnDestroy: OnPoolDestroy,
                defaultCapacity: defaultPoolCapacity,
                maxSize: maxPoolSize
            );

            VerifyLinks();
            Application.quitting += characterSettings.Dispose;
        }

        protected virtual void OnDisable() {
            characterRendererPool.Clear();

            Application.quitting -= characterSettings.Dispose;
        }

        public void CreateDefaultText() => CreateText(text);

        private void VerifyLinks() {
            characterSettings.SetUpCharacterSets();
        }

        public virtual void DidCreateText() { }

        public void CreateText(string text) {
            if (automaticallyReleaseText) {
                ClearText();
            } else {
                CharacterRenderers = null;
            }

            this.text = text;

            CharacterRenderers = CreateRenderers();

            if (materialPropertyBlock != null) {
                PropertyBlock = PropertyBlock;
            }

            DidCreateText();
        }

        private TKCharacterRenderer[] CreateRenderers() {
            string[] lines = text.Split('\n');
            float3[] characterPositions = GetCharacterPositions(text, lines);

            monospacedSize.x = 0;
            monospacedSize.y = lines.Length;

            int lineStartIndex = 0;

            int indexCounter = 0;
            TKCharacterRenderer[] createdObjects = new TKCharacterRenderer[text.Length];

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];
                monospacedSize.x = math.max(monospacedSize.x, line.Length);

                for (int i = 0; i < line.Length; i++) {
                    string character = line[i].ToString();
                    if (!string.IsNullOrWhiteSpace(character)) {
                        if (Extensions.TryGetModifiedCharacter(line, i, out string modifiedCharacter, out int newIndex)) {
                            character = modifiedCharacter;
                            i = newIndex;
                        }

                        TKCharacterRenderer obj = CopyCharacter(character, transform, float3.zero, true, out _);
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

            return createdObjects.Select(obj => obj ?? null).ToArray();
        }

        public void SetRendererActive(bool state) {
            foreach (TKCharacterRenderer r in CharacterRenderers) {
                r.Enabled = state;
            }
        }
    }

    // MARK: - Cleanup

    public partial class TKText {
#if ODIN_INSPECTOR_3
        [BoxGroup("Cleanup")] public bool automaticallyReleaseText = true;
        [BoxGroup("Cleanup")] public HideFlags characterHideFlags = HideFlags.HideAndDontSave;
#else
        [Header("Cleanup")] public bool automaticallyReleaseText = true;
        public HideFlags characterHideFlags = HideFlags.HideAndDontSave;
#endif

        public virtual void WillClearText() { }

        public void ClearText() {
            if (CharacterRenderers == null) {
                return;
            }

            WillClearText();

            foreach (TKCharacterRenderer renderer in CharacterRenderers) {
                characterRendererPool.Release(renderer);
            }
            monospacedSize = int2.zero;
            CharacterRenderers = null;
        }

        public void ClearCharacters(int[] indices) => ClearCharacters(indices.Select(i => CharacterRenderers[i]).ToArray());

        public void ClearCharacters(TKCharacterRenderer[] objects) {
            if (objects == null) { return; }
            foreach (TKCharacterRenderer obj in objects) {
                characterRendererPool.Release(obj);
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
                    foreach (TKCharacterRenderer r in CharacterRenderers) {
                        r.SetMaterial(value);
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
                    foreach (TKCharacterRenderer r in CharacterRenderers) {
                        r?.SetPropertyBlock(value);
                    }
                }
            }
        }
    }

    // MARK: - Pooling

    public partial class TKText {
#if ODIN_INSPECTOR_3
        [BoxGroup("Pooling")] public int defaultPoolCapacity = 5;
        [BoxGroup("Pooling")] public int maxPoolSize = 15;
#else
        [Header("Pooling")] public int defaultPoolCapacity = 5;
        public int maxPoolSize = 15;
#endif
        internal ObjectPool<TKCharacterRenderer> characterRendererPool = null;

        internal TKCharacterRenderer OnPoolCreate() => GameObject.Instantiate(rendererPrefab);

        internal void OnPoolRelease(TKCharacterRenderer obj) {
            obj.Clear();
            obj.transform.SetParent(null);
            obj.gameObject.SetActive(false);
        }

        internal void OnPoolDestroy(TKCharacterRenderer obj) {
            GameObject.Destroy(obj.gameObject);
        }

        private TKCharacterRenderer CopyCharacter(string source, Transform parent, float3 position, bool active, out float widthExtents) {
            if (CharacterLink.TryGetValue(source, out TKCharacter tkChar)) {
                widthExtents = tkChar.Bounds.extents.x;

                TKCharacterRenderer obj = characterRendererPool.Get();
                obj.SetCharacter(tkChar, material);

                obj.transform.SetParent(parent);
                obj.transform.localPosition = position;
                obj.transform.localEulerAngles = characterSettings.rotation;
                obj.transform.localScale = characterSettings.textScale;

                obj.Enabled = true;

                obj.gameObject.SetActive(active);

                obj.gameObject.layer = gameObject.layer;
                obj.gameObject.hideFlags = characterHideFlags;

                return obj;
            }
            widthExtents = 0;
            Debug.LogWarning($"Missing character \"{source}\"");
            return null;
        }
    }

    // MARK: - Layout

    public partial class TKText {
#if ODIN_INSPECTOR_3
        [BoxGroup("Layout")] public HorizontalAlignment horizontalAlignment = HorizontalAlignment.Leading;
        [BoxGroup("Layout")] public VerticalAlignment verticalAlignment = VerticalAlignment.Bottom;
        [BoxGroup("Layout")] public float sizeMultiplier = 1;
        [BoxGroup("Layout")] public bool recalculateBounds = true;
        [BoxGroup("Layout")] public bool monospaced = false;
#else
        [Header("Layout")] public HorizontalAlignment horizontalAlignment = HorizontalAlignment.Leading;
        public VerticalAlignment verticalAlignment = VerticalAlignment.Bottom;
        public float sizeMultiplier = 1;
        public bool recalculateBounds = true;
        public bool monospaced = false;
#endif
        private float3 SizeMultiplier => characterSettings.textScale * sizeMultiplier;
        private int2 monospacedSize = int2.zero;

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
    }

    // MARK: - Debug

    public partial class TKText {
#if ODIN_INSPECTOR_3
        [BoxGroup("Debug"), ShowInInspector, ReadOnly] public TKCharacterRenderer[] CharacterRenderers { get; private set; }
#else
        public TKCharacterRenderer[] CharacterRenderers { get; private set; }
#endif
    }
}