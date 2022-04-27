
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RPG.Core {
    [Serializable]
    public class MeshAssetReference : AssetReferenceT<Mesh>
    {
        public MeshAssetReference(string guid) : base(guid)
        {
        }
    }
    [Serializable]
    public class MaterialAssetReference : AssetReferenceT<Material>
    {
        public MaterialAssetReference(string guid) : base(guid)
        {
        }
    }
    public class  AddressableMeshRender : MonoBehaviour {
        public MaterialAssetReference[] Materials;
        public MeshAssetReference Mesh;
    }
}