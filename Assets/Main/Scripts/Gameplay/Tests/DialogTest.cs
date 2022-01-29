using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using RPG.Gameplay;
using Unity.Entities;

public class DialogTest
{
    // A Test behaves as an ordinary method
    [Test]
    public void NewTestScriptSimplePasses()
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
        var world = World.DefaultGameObjectInjectionWorld;
        var e = world.EntityManager.CreateEntity();
        world.EntityManager.AddComponentData<CurrentDialog>(e, new CurrentDialog { NodeIndex = dialog.Value.StartIndex });
        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator NewTestScriptWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
