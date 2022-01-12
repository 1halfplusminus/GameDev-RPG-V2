using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Entities;

public class AudioTest
{


    // A Test behaves as an ordinary method
    [Test]
    public void AudioTestSimplePasses()
    {
        World world = World.DefaultGameObjectInjectionWorld;
        Assert.IsTrue(world != null, "World cannot be null");
        var entity = world.EntityManager.CreateEntity();

        // Use the Assert class to test conditions
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator AudioTestWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}