using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RPG.Gameplay;
using Unity.Entities;
using RPG.UI;
using UnityEngine.UIElements;

namespace RPG.Test
{
    public class DialogTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void ConvertDialogToBlobDialog()
        {
            var dialogAsset = Resources.Load<DialogGraph>("Dialog");
            Assert.IsNotNull(dialogAsset);
            Assert.IsTrue(dialogAsset.nodes.Count > 0);
            var store = new BlobAssetStore();
            var dialog = store.GetDialog(dialogAsset);
            Assert.IsTrue(dialog.IsCreated);
            Assert.IsTrue(dialog.Value.Nodes.Length == 4);
            Assert.IsTrue(dialog.Value.Start.Value.Choices.Length == 2);
            Assert.IsTrue(dialog.Value.Start.Value.Text == "Hello {username}");
            Assert.IsTrue(dialog.Value.StartIndex == 3);
            // Assert.IsTrue(dialog.Value.Start.Value.Choices[1].Value.Text.ToString() == "Howdy!");
            Assert.IsTrue(dialog.Value.Start.Value.Choices[0].Value.Text.ToString() == "Hey !");
            Assert.IsTrue(dialog.Value.Start.Value.Choices[1].Value.Text.ToString() == "Howdy!");
            Assert.IsTrue(dialog.Value.Start.Value.ChoicesIndex[0] == 2);

            Assert.IsTrue(dialog.Value.Nodes[1].Text.Value == "This is a test !");

            Assert.IsTrue(dialog.Value.Start.Value.Choices[1].Value.NextIndex == 1);
            Assert.IsTrue(dialog.Value.Nodes[dialog.Value.StartIndex].Choices[1].Value.Next.Value.Text == "This is a test !");

        }
        [Test]
        public void DialogAuthoring()
        {
            var dialogAuthoring = Resources.Load<GameObject>("DialogAuthoring");
            Assert.IsNotNull(dialogAuthoring);

            var world = World.DefaultGameObjectInjectionWorld;
            var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
            var conversionSetting = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
            GameObjectConversionUtility.ConvertGameObjectHierarchy(dialogAuthoring, conversionSetting);
            var em = world.EntityManager;
            var countDialog = em.CreateEntityQuery(typeof(Dialog));
            Assert.IsTrue(countDialog.CalculateEntityCount() == 1);
        }

        [UnityTest]
        public IEnumerator DialogUITest()
        {

            var dialogUIPrefab = Resources.Load<GameObject>("dialogUIPrefab");
            Assert.IsNotNull(dialogUIPrefab);
            var dialogAsset = Resources.Load<DialogGraph>("Dialog");
            Assert.IsNotNull(dialogAsset);
            var store = new BlobAssetStore();
            var dialog = store.GetDialog(dialogAsset);
            var instance = Object.Instantiate(dialogUIPrefab);
            var uiController = new DialogController();
            var uiDocument = instance.GetComponent<UIDocument>();
            uiController.Init(uiDocument.rootVisualElement);
            uiController.ShowNode(dialog, dialog.Value.StartIndex);
            yield return new WaitForSeconds(10f);
        }


        [UnityTest]
        public IEnumerator DialogUISytem()
        {
            var uiModule = Resources.Load<GameObject>("UI Module");
            Object.Instantiate(uiModule);
            var gameManager = Resources.Load<GameObject>("Game Manager");
            var world = World.DefaultGameObjectInjectionWorld;
            var em = world.EntityManager;
            var convertToEntitySystem = world.GetOrCreateSystem<ConvertToEntitySystem>();
            var conversionSetting = GameObjectConversionSettings.FromWorld(world, convertToEntitySystem.BlobAssetStore);
            GameObjectConversionUtility.ConvertGameObjectHierarchy(gameManager, conversionSetting);
            yield return new WaitForSeconds(1f);
            Assert.IsNotNull(world.GetExistingSystem<DialogUISystem>());
            var dialogUIPrefabQuery = em.CreateEntityQuery(typeof(Prefab), typeof(DialogUI));
            Assert.IsTrue(dialogUIPrefabQuery.CalculateEntityCount() > 0);
            var dialogAuthoring = Resources.Load<GameObject>("DialogAuthoring");
            Assert.IsNotNull(dialogAuthoring);
            GameObjectConversionUtility.ConvertGameObjectHierarchy(dialogAuthoring, conversionSetting);
            yield return new WaitForEndOfFrame();
            var dialogUIQuery = em.CreateEntityQuery(typeof(DialogUI));
            Assert.IsTrue(dialogUIQuery.CalculateEntityCount() > 0);
            yield return new WaitForEndOfFrame();
            var renderDialogQuery = em.CreateEntityQuery(typeof(RenderDialog));
            Assert.IsTrue(renderDialogQuery.CalculateEntityCount() > 0);
            yield return new WaitForEndOfFrame();
            var dialogControllerQuery = em.CreateEntityQuery(typeof(DialogController));
            Assert.IsTrue(dialogControllerQuery.CalculateEntityCount() > 0);
        }
    }

}
