
using Unity.Entities;
using UnityEngine.UIElements;



namespace RPG.Hybrid
{

    [UpdateInGroup(typeof(GameObjectDeclareReferencedObjectsGroup))]
    public class UIDocumentDeclareReferenceConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UIDocument uiDocument) =>
            {

                DeclareReferencedAsset(uiDocument.visualTreeAsset);
                DeclareAssetDependency(uiDocument.gameObject, uiDocument.visualTreeAsset);
            });
        }
    }

    public class UIDocumentConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((UIDocument uiDocument) =>
            {
                AddHybridComponent(uiDocument);
            });
        }
    }

}