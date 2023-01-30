// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TextKit {
    public static class Extensions {
        public const string MODIFIER = "$";
        public const string START_MODIFIED = "[";
        public const string END_MODIFIED = "]";

        public static float3 CenterOfPoints(IEnumerable<float3> source) {
            float3 min = new float3(1) * float.MaxValue;
            float3 max = new float3(1) * float.MinValue;
            foreach (float3 point in source) {
                min = math.min(point, min);
                max = math.max(point, max);
            }
            float3 size = max - min;
            float3 result = min + size * 0.5f;
            return result;
        }

        public static Bounds CalculateRendererBounds(GameObject container) {
            Bounds containerBounds = new Bounds(container.transform.position, float3.zero);
            foreach (MeshRenderer meshRenderer in container.GetComponentsInChildren<MeshRenderer>()) {
                containerBounds.Encapsulate(meshRenderer.bounds);
            }
            return containerBounds;
        }

        public static Bounds GetRendererBounds(GameObject container) => container.GetComponent<MeshRenderer>().bounds;

        public static bool TryGetModifiedCharacter(string text, int startIndex, out string result, out int newIndex) {
            result = string.Empty;
            newIndex = startIndex;

            if (text[startIndex].ToString() == MODIFIER && text[startIndex + 1].ToString() == START_MODIFIED) {
                int originalIndex = startIndex;
                int modifiedLength = 0;

                for (int j = startIndex; j < text.Length; j++) {
                    modifiedLength++;
                    if (text[j].ToString() == END_MODIFIED) {
                        modifiedLength = j - startIndex + 1;
                        newIndex = j;
                        break;
                    }
                }

                if (modifiedLength > 3) {
                    result = text.Substring(originalIndex + 1, modifiedLength - 1);
                    return true;
                }

                Debug.Log("Something went wrong when trying to parse a modified character");
                return false;
            }
            return false;
        }
    }
}