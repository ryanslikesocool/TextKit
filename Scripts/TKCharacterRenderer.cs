using UnityEngine;

namespace TextKit {
    public class TKCharacterRenderer : MonoBehaviour {
        [SerializeField] protected MeshFilter meshFilter = null;
        [SerializeField] protected MeshRenderer meshRenderer = null;

        public Bounds Bounds => meshRenderer.bounds;

        public virtual bool Enabled {
            get => meshRenderer.enabled;
            set => meshRenderer.enabled = value;
        }

        public virtual void SetPropertyBlock(MaterialPropertyBlock propertyBlock) {
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        public virtual void SetCharacter(TKCharacter characterData, Material material) {
            meshFilter.sharedMesh = characterData.Mesh;
            meshRenderer.sharedMaterial = material;
        }

        public virtual void SetMaterial(Material material) {
            meshRenderer.sharedMaterial = material;
        }

        public virtual void Clear() {
            meshFilter.sharedMesh = null;
            meshRenderer.sharedMaterial = null;
        }
    }
}