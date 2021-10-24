using Unity.Entities;
using Unity.Scenes;

[assembly: RegisterGenericComponentType(typeof(LinkedEntityGroup))]
[DisableAutoCreation]
public class ToggleSubSceneSystem : GameObjectConversionSystem {
     protected override void OnUpdate() {

         Entities.WithNone<RequestSceneLoaded>().ForEach((Entity entity, SubScene scene) => {
            EntityManager.AddComponent<RequestSceneLoaded>(entity);
   
         });
     
     }
 }