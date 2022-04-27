using System;
using Unity.Build.Classic;
using UnityEditor;

namespace RPG.Test
{
    class TestBundlesBuildCustomizer : ClassicBuildPipelineCustomizer
    {
        public override Type[] UsedComponents => base.UsedComponents;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string[] ModifyEmbeddedScenes(string[] scenes)
        {
            return base.ModifyEmbeddedScenes(scenes);
        }

        public override void OnBeforeBuild()
        {
            base.OnBeforeBuild();
            // AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
            // buildMap[0].assetBundleName = "world";
            // buildMap[0].assetNames = new string[]{
            //     "Assets/world.asset"
            // };
            // CompatibilityBuildPipeline.BuildAssetBundles ("Assets/ABs", buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneLinux64);
        }

        public override void OnBeforeRegisterAdditionalFilesToDeploy()
        {
            base.OnBeforeRegisterAdditionalFilesToDeploy();

        }

        public override BuildOptions ProvideBuildOptions()
        {
            return base.ProvideBuildOptions();
        }

        public override string[] ProvidePlayerScriptingDefines()
        {
            return base.ProvidePlayerScriptingDefines();
        }

        public override void RegisterAdditionalFilesToDeploy(Action<string, string> registerAdditionalFileToDeploy)
        {
           
            base.RegisterAdditionalFilesToDeploy(registerAdditionalFileToDeploy);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}