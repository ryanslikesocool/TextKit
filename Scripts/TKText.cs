// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
#if ODIN_INSPECTOR_3
using Sirenix.OdinInspector;
#endif

namespace TextKit {
    public class TKText : MonoBehaviour {
        // MARK: - Inspector

        [Space, SerializeField, TextArea(1, 10)] private string text = string.Empty;

#if ODIN_INSPECTOR_3
        [BoxGroup("Info"), SerializeField] private TKFontSettings characterSettings = null;

        [BoxGroup("Rendering"), SerializeField] private Material material = null;
        [BoxGroup("Rendering"), SerializeField] private TKCharacterRenderer rendererPrefab = null;

        [BoxGroup("Cleanup")] public HideFlags characterHideFlags = HideFlags.HideAndDontSave;

        [BoxGroup("Pooling")] public int defaultPoolCapacity = 5;
        [BoxGroup("Pooling")] public int maxPoolSize = 15;

        [BoxGroup("Layout")] public HorizontalAlignment horizontalAlignment = HorizontalAlignment.Leading;
        [BoxGroup("Layout")] public VerticalAlignment verticalAlignment = VerticalAlignment.Bottom;
        [BoxGroup("Layout")] public float sizeMultiplier = 1;
        [BoxGroup("Layout")] public bool recalculateBounds = true;
        [BoxGroup("Layout")] public bool monospaced = false;

        [BoxGroup("Debug"), ShowInInspector, ReadOnly] public TKCharacterRenderer[] CharacterRenderers { get; private set; }
#else
        [Header("Info"), SerializeField] private TKFontSettings characterSettings = null;

        [Header("Rendering"), SerializeField] private Material material = null;
        [SerializeField] private TKCharacterRenderer rendererPrefab = null;

        [Header("Cleanup")] public HideFlags characterHideFlags = HideFlags.HideAndDontSave;

        [Header("Pooling")] public int defaultPoolCapacity = 5;
        public int maxPoolSize = 15;

        [Header("Layout")] public HorizontalAlignment horizontalAlignment = HorizontalAlignment.Leading;
        public VerticalAlignment verticalAlignment = VerticalAlignment.Bottom;
        public float sizeMultiplier = 1;
        public bool recalculateBounds = true;
        public bool monospaced = false;

        public TKCharacterRenderer[] CharacterRenderers { get; private set; }
#endif

        // MARK: - Internal Fields

        private float3 SizeMultiplier => characterSettings.textScale * sizeMultiplier;
        private int2 monospacedSize = int2.zero;

        private MaterialPropertyBlock materialPropertyBlock;
        private Dictionary<string, TKCharacter> CharacterLink => characterSettings.characterLink;

        internal ObjectPool<TKCharacterRenderer> characterRendererPool = null;

        // MARK: - Property Access

        public string Text {
            get => text;
            set {
                if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value)) {
                    ClearText();
                    text = value;
                } else {
                    SetText(value);
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

        // MARK: - Unity Hooks

        protected virtual void Start() {
            if (text != string.Empty) {
                SetText(text);
            }
        }

        protected virtual void OnEnable() {
            characterRendererPool = new ObjectPool<TKCharacterRenderer>(
                createFunc: OnPoolCreate,
                //actionOnGet: OnPoolGet,
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

        // MARK: - TextKit

        private void VerifyLinks() {
            characterSettings.SetUpCharacterSets();
        }

        public virtual void DidCreateText() { }

        public void SetText(string text) {
            //ClearText();

            this.text = text;

            CreateRenderers();
            ApplyRendererSettings();
            SetCharacterData();
            ComputeTextLayout();

            if (materialPropertyBlock != null) {
                PropertyBlock = PropertyBlock;
            }

            DidCreateText();
        }

        private void CreateRenderers() {
            TKCharacterRenderer[] oldValues = this.CharacterRenderers;
            TKCharacterRenderer[] result = new TKCharacterRenderer[text.Length];

            Action<int> body;
            if (oldValues == null) {
                body = i => result[i] = characterRendererPool.Get();
            } else if (oldValues.Length < result.Length) {
                body = i => {
                    if (i < oldValues.Length) {
                        result[i] = oldValues[i] ?? characterRendererPool.Get();
                    } else {
                        result[i] = characterRendererPool.Get();
                    }
                };
            } else {
                body = i => result[i] = oldValues[i] ?? characterRendererPool.Get();
            }

            for (int i = 0; i < result.Length; i++) {
                string character = text[i].ToString();
                if (string.IsNullOrWhiteSpace(character)) {
                    continue;
                }
                if (Extensions.TryGetModifiedCharacter(text, i, out string modifiedCharacter, out int newIndex)) {
                    character = modifiedCharacter;
                    i = newIndex;
                }

                body(i);
                //result[i] = oldValues[i] ?? characterRendererPool.Get();
            }

            CharacterRenderers = result;
        }

        private void SetCharacterData() {
            IterateText((index, character) => {
                CharacterLink.TryGetValue(character, out TKCharacter tkChar);
                CharacterRenderers[index].SetCharacter(tkChar, material);
            });
        }

        private void IterateText(Action<int, string> body) {
            for (int i = 0; i < text.Length; i++) {
                string character = text[i].ToString();
                if (string.IsNullOrWhiteSpace(character)) {
                    continue;
                }
                if (Extensions.TryGetModifiedCharacter(text, i, out string modifiedCharacter, out int newIndex)) {
                    character = modifiedCharacter;
                    i = newIndex;
                }

                body(i, character);
            }
        }

        private void SinglePassCreate() {
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

                        TKCharacterRenderer obj = CopyCharacter(character, float3.zero, true, out _);
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

            CharacterRenderers = createdObjects;//.Select(obj => obj ?? null).ToArray();
        }

        public void SetRendererActive(bool state) {
            foreach (TKCharacterRenderer r in CharacterRenderers) {
                r.Enabled = state;
            }
        }

        // MARK: - Cleanup

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

        // MARK: - Pooling

        internal TKCharacterRenderer OnPoolCreate() => GameObject.Instantiate(rendererPrefab);

        internal void OnPoolRelease(TKCharacterRenderer obj) {
            obj.Clear();
            obj.transform.SetParent(null);
            obj.gameObject.SetActive(false);
        }

        internal void OnPoolDestroy(TKCharacterRenderer obj) {
            GameObject.Destroy(obj.gameObject);
        }

        private TKCharacterRenderer CopyCharacter(string source, float3 position, bool active, out float widthExtents) {
            if (CharacterLink.TryGetValue(source, out TKCharacter tkChar)) {
                widthExtents = tkChar.Bounds.extents.x;

                TKCharacterRenderer obj = characterRendererPool.Get();
                obj.SetCharacter(tkChar, material);

                ApplyRendererSettings(obj);

                obj.transform.localPosition = position;

                obj.gameObject.SetActive(active);

                return obj;
            }
            widthExtents = 0;
            Debug.LogWarning($"Missing character \"{source}\"");
            return null;
        }

        private void ApplyRendererSettings() {
            foreach (TKCharacterRenderer item in CharacterRenderers) {
                if (item == null) {
                    continue;
                }
                ApplyRendererSettings(item);
            }
        }

        private void ApplyRendererSettings(TKCharacterRenderer obj) {
            obj.transform.SetParent(transform);
            obj.transform.localEulerAngles = characterSettings.rotation;
            obj.transform.localScale = characterSettings.textScale;

            obj.Enabled = true;

            obj.gameObject.layer = gameObject.layer;
            obj.gameObject.hideFlags = characterHideFlags;
        }

        // MARK: - Layout

        public void ComputeTextLayout() {
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

        public Bounds GetTextBounds() {
            Bounds bounds = CharacterRenderers[0].Bounds;
            for (int i = 1; i < CharacterRenderers.Length; i++) {
                bounds.Encapsulate(CharacterRenderers[i].Bounds);
            }
            return bounds;
        }

        public float3 GetTextSize() {
            Bounds bounds = GetTextBounds();
            if (!monospaced) {
                return bounds.size;
            }

            float2 gridSize = (float2)monospacedSize;
            float2 characterSize = new float2(characterSettings.monospacedWidth, characterSettings.lineHeight) * sizeMultiplier;
            return new float3(gridSize * characterSize, bounds.size.z);
        }

        public float3[] GetCharacterPositions(string text) => GetCharacterPositions(text, text.Split('\n'));

        public float3[] GetCharacterPositions(string text, string[] lines) {
            float[] characterWidth = new float[text.Length];
            int lineStartIndex = 0;
            float lineHeight = characterSettings.lineHeight * SizeMultiplier.y;
            float totalHeight = lineHeight * lines.Length;

            float3[] characterPositions = new float3[text.Length];
            float monospacedWidth = characterSettings.monospacedWidth;
            float halfMonospacedWidth = monospacedWidth * 0.5f;

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];
                float3 charPos = math.down() * lineIndex * characterSettings.lineHeight * SizeMultiplier.y;
                float lineWidth = 0;

                for (int i = 0; i < line.Length; i++) {
                    string character = line[i].ToString();
                    if (string.IsNullOrWhiteSpace(character)) {
                        lineWidth += SizeMultiplier.x * (monospaced
                         ? halfMonospacedWidth
                         : characterSettings.whitespaceWidth);

                        if (i < line.Length - 1) {
                            lineWidth += SizeMultiplier.x * (monospaced
                             ? halfMonospacedWidth
                             : characterSettings.characterSpacing);
                        }
                    } else {
                        if (Extensions.TryGetModifiedCharacter(line, i, out string modifiedCharacter, out int newIndex)) {
                            character = modifiedCharacter;
                            i = newIndex;
                        }

                        if (!CharacterLink.TryGetValue(character, out TKCharacter tkChar)) {
                            Debug.LogWarning($"\"{character}\" is missing from the link");
                            continue;
                        }

                        lineWidth += SizeMultiplier.x * (monospaced
                         ? halfMonospacedWidth
                         : tkChar.Bounds.size.x);

                        if (i < line.Length - 1) {
                            lineWidth += SizeMultiplier.x * (monospaced
                             ? halfMonospacedWidth
                             : characterSettings.characterSpacing);
                        }
                    }
                }

                for (int i = 0; i < line.Length; i++) {
                    bool isLastCharacter = i == line.Length - 1;

                    string character = line[i].ToString();
                    if (string.IsNullOrWhiteSpace(character)) {
                        float delta;
                        if (monospaced) {
                            delta = isLastCharacter ? halfMonospacedWidth : monospacedWidth;
                        } else {
                            delta = characterSettings.whitespaceWidth;
                            if (!isLastCharacter) {
                                delta += characterSettings.characterSpacing;
                            }
                        }
                        charPos.x += SizeMultiplier.x * delta;
                    } else {
                        if (Extensions.TryGetModifiedCharacter(line, i, out string modifiedCharacter, out int newIndex)) {
                            character = modifiedCharacter;
                            i = newIndex;
                        }

                        bool hasCharacter = CharacterLink.ContainsKey(character);
                        if (hasCharacter) {
                            characterPositions[lineStartIndex + i] = charPos;
                            characterWidth[lineStartIndex + i] = CharacterLink[character].Bounds.size.x;
                        }

                        float delta;
                        if (monospaced) {
                            delta = isLastCharacter ? halfMonospacedWidth : monospacedWidth;
                        } else {
                            delta = CharacterLink[character].Bounds.size.x;
                            if (!isLastCharacter) {
                                delta += characterSettings.characterSpacing;
                            }
                        }
                        charPos.x += SizeMultiplier.x * delta;
                    }
                }

                float3 offset = float3.zero;
                offset.x = -lineWidth;
                switch (horizontalAlignment) {
                    case HorizontalAlignment.Center:
                        offset.x *= 0.5f;
                        break;
                    case HorizontalAlignment.Leading:
                        offset.x = 0;
                        break;
                }
                switch (verticalAlignment) {
                    case VerticalAlignment.Bottom:
                        offset.y = totalHeight - lineHeight * lineIndex;
                        break;
                    case VerticalAlignment.Middle:
                        offset.y = totalHeight * 0.5f - lineHeight * lineIndex;
                        break;
                    case VerticalAlignment.Top:
                        offset.y = -lineHeight * lineIndex;
                        break;
                }
                for (int i = 0; i < line.Length; i++) {
                    characterPositions[lineStartIndex + i] += (offset + (monospaced
                     ? math.right() * SizeMultiplier.x * 0.5f * (halfMonospacedWidth - characterWidth[lineStartIndex + i])
                     : 0));
                }

                lineStartIndex += line.Length;
            }

            return characterPositions;
        }
    }
}