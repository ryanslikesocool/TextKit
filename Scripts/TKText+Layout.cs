// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using Unity.Mathematics;
using UnityEngine;

namespace TextKit {
    public partial class TKText {
        public Bounds GetTextBounds() {
            Bounds bounds = new Bounds();
            foreach (MeshRenderer r in Renderers) {
                bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        public float3[] GetCharacterPositions(string text) => GetCharacterPositions(text, text.Split('\n'));

        public float3[] GetCharacterPositions(string text, string[] lines) {
            float totalHeight = 0;
            float[] characterWidth = new float[text.Length];
            int lineStartIndex = 0;

            float3[] characterPositions = new float3[text.Length];

            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                string line = lines[lineIndex];
                float3 charPos = math.down() * lineIndex * characterSettings.lineSpacing * SizeMultiplier.y;
                float lineWidth = 0;
                float lineHeight = 0;

                for (int i = 0; i < line.Length; i++) {
                    string character = line[i].ToString();
                    if (string.IsNullOrWhiteSpace(character)) {
                        lineWidth += SizeMultiplier.x * (monospaced
                         ? characterSettings.monospacedWidth
                         : characterSettings.whitespaceWidth);
                        lineHeight = math.max(lineHeight, characterSettings.whitespaceHeight);

                        if (i < line.Length - 1) {
                            lineWidth += SizeMultiplier.x * (monospaced
                             ? characterSettings.monospacedWidth
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
                         ? characterSettings.monospacedWidth
                         : tkChar.Bounds.size.x);
                        lineHeight = math.max(lineHeight, tkChar.Bounds.size.y);

                        if (i < line.Length - 1) {
                            lineWidth += SizeMultiplier.x * (monospaced
                             ? characterSettings.monospacedWidth
                             : characterSettings.characterSpacing);
                        }
                    }
                }

                if (lineIndex == 0) {
                    totalHeight = lineHeight * lines.Length + characterSettings.lineSpacing * (lines.Length - 1);
                    totalHeight *= SizeMultiplier.y;
                }
                lineHeight *= SizeMultiplier.y;

                for (int i = 0; i < line.Length; i++) {
                    string character = line[i].ToString();
                    if (string.IsNullOrWhiteSpace(character)) {
                        charPos.x += SizeMultiplier.x * (monospaced
                         ? characterSettings.monospacedWidth
                         : characterSettings.whitespaceWidth);

                        if (i < line.Length - 1) {
                            charPos.x += SizeMultiplier.x * (monospaced
                             ? characterSettings.monospacedWidth
                             : characterSettings.characterSpacing);
                        }
                    } else {
                        if (Extensions.TryGetModifiedCharacter(line, i, out string modifiedCharacter, out int newIndex)) {
                            character = modifiedCharacter;
                            i = newIndex;
                        }

                        if (CharacterLink.ContainsKey(character)) {
                            characterPositions[lineStartIndex + i] = charPos;
                            charPos.x += SizeMultiplier.x * (monospaced
                             ? characterSettings.monospacedWidth
                             : CharacterLink[character].Bounds.size.x);
                            characterWidth[lineStartIndex + i] = CharacterLink[character].Bounds.size.x;
                        }

                        if (i < line.Length - 1) {
                            charPos.x += SizeMultiplier.x * (monospaced
                             ? characterSettings.monospacedWidth
                             : characterSettings.characterSpacing);
                        }
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
                     ? math.right() * SizeMultiplier.x * 0.5f * (characterSettings.monospacedWidth - characterWidth[lineStartIndex + i])
                     : 0));
                }

                lineStartIndex += line.Length;
            }

            return characterPositions;
        }
    }
}