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
        PlayerSettings.WebGL.memorySize = 46275232;
        PlayerSettings.WebGL.emscriptenArgs = "-s TOTAL_MEMORY=46275232 -s WASM_MEM_MAX=512MB";

    }
    public override void RegisterAdditionalFilesToDeploy(Action<string, string> registerAdditionalFileToDeploy)
    {

    }
}