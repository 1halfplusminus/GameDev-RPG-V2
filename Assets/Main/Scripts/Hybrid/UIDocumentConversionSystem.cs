
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;


[assembly: RegisterGenericComponentType(typeof(TemplateContainer.UxmlFactory))]

namespace RPG.Hybrid
{

    public struct VisualElementContainer
    {

    }

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
                var entity = TryGetPrimaryEntity(uiDocument);

            });
        }
    }

}