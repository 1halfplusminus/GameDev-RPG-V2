using System.Collections;
using System.Collections.Generic;
using Unity.Build;
using UnityEditor;
using System;
using Unity.Build.Classic;

public class TestSettings : IBuildComponent, IBuildComponentInitialize
{

    public void Initialize(HierarchicalComponentContainer<BuildConfiguration, IBuildComponent>.ReadOnly container)
    {


    }

}

public class TestBuildStep : ClassicBuildPipelineCustomizer
{
    public override Type[] UsedComponents { get; } =
    {
        typeof(TestSettings)
    };
    public override void OnBeforeBuild()
    {
        PlayerSettings.WebGL.memorySize = 512;
        PlayerSettings.WebGL.emscriptenArgs = "-s TOTAL_MEMORY=2147483648 -s INITIAL_MEMORY=2147483648 -s WASM_MEM_MAX=512MB -s ERROR_ON_UNDEFINED_SYMBOLS=0";

    }
    public override void RegisterAdditionalFilesToDeploy(Action<string, string> registerAdditionalFileToDeploy)
    {

    }
}


[InitializeOnLoad]
class EnableThreads
{
    static EnableThreads()
    {
        PlayerSettings.WebGL.emscriptenArgs = "-s TOTAL_MEMORY=2147483648 -s INITIAL_MEMORY=2147483648 -s WASM_MEM_MAX=512MB -s ERROR_ON_UNDEFINED_SYMBOLS=0";
    }
}