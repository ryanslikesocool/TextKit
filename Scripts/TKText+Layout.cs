// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using Unity.Mathematics;
using UnityEngine;

namespace TextKit {
    public partial class TKText {
        public Bounds GetTextBounds() {
            Bounds bounds = CharacterRenderers[0].bounds;
            for (int i = 1; i < CharacterRenderers.Length; i++) {
                bounds.Encapsulate(CharacterRenderers[i].bounds);
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