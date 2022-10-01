// Developed With Love by Ryan Boyer http://ryanjboyer.com <3

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace TextKit {
    internal class TKCharacter {
        internal GameObject Prefab { get; private set; }
        internal Bounds Bounds { get; private set; }
        internal ObjectPool<GameObject> pool;
        internal CharacterData CharacterData { get; private set; }
        internal string Character => CharacterData.AccessString;
        internal Mesh Mesh => CharacterData.Mesh;

        internal TKCharacter(GameObject sourcePrefab, in CharacterData sourceData, Transform parent, bool hidden) => Initialize(sourcePrefab, sourceData, parent, hidden);

        private void Initialize(GameObject sourcePrefab, in CharacterData sourceData, Transform parent, bool hidden) {
            Prefab = GameObject.Instantiate(sourcePrefab, parent);
            if (hidden) {
                Prefab.hideFlags = HideFlags.HideAndDontSave;
            }
            foreach (Transform child in Prefab.transform) {
                child.localScale = new float3(3);
                child.localPosition = float3.zero;
                child.eulerAngles = float3.zero;
                //child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            Prefab.GetComponent<MeshFilter>().sharedMesh = sourceData.Mesh;
            Prefab.SetActive(false);

            pool = new ObjectPool<GameObject>(
                createFunc: OnPoolCreate,
                actionOnRelease: OnPoolRelease,
                actionOnDestroy: OnPoolDestroy,
                defaultCapacity: 1,
                maxSize: 10
            );

            CharacterData = sourceData;
            Bounds = sourceData.Mesh.bounds;
        }

        internal void EnqueueMany(IEnumerable<GameObject> objects) {
            foreach (GameObject obj in objects) {
                pool.Release(obj);
            }
        }

        internal GameObject OnPoolCreate() => GameObject.Instantiate(Prefab);

        internal void OnPoolRelease(GameObject obj) {
            obj.transform.SetParent(null);
            obj.SetActive(false);
        }

        internal void OnPoolDestroy(GameObject obj) {
            GameObject.Destroy(obj);
        }

        internal GameObject CopyCharacter(CharacterSettings settings, Transform parent, float3 position, bool active, Material material) {
            GameObject result = pool.Get();

            if (settings.hiddenGameObjects) {
                result.hideFlags = HideFlags.HideAndDontSave;
            }

            result.transform.SetParent(parent);
            result.transform.localPosition = position;
            result.transform.localEulerAngles = settings.rotation;
            result.transform.localScale = settings.textScale;

            Bounds resultBounds = Extensions.CalculateRendererBounds(result);

            float3 offset = float3.zero;
            if (settings.recenter) {
                offset = -Bounds.extents;
            }
            offset += new float3(
                resultBounds.size.x * settings.pivot.x * 0.1f,
                resultBounds.size.y * settings.pivot.y * 0.1f,
                resultBounds.size.z * settings.pivot.z * 0.1f
            );

            foreach (Transform child in result.transform) {
                child.localPosition = offset;
                child.localEulerAngles = settings.rotation;
            }

            foreach (MeshRenderer meshRenderer in result.GetComponentsInChildren<MeshRenderer>()) {
                meshRenderer.enabled = true;
                meshRenderer.sharedMaterial = material;
            }

            result.SetActive(active);
            return result;
        }

        internal void VerifyLinks(GameObject sourcePrefab, Transform parent, bool hidden) {
            if (Prefab == null) {
                Initialize(sourcePrefab, CharacterData, parent, hidden);
            }
        }

        internal void RecalculateMesh(bool recalculateBounds) {
            if (recalculateBounds) {
                Mesh.RecalculateBounds();
            }
            Mesh.Optimize();
        }
    }
}