using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.Rendering;
using UnityEngine;

// Inference is installed for editor tooling. Its Resources shaders otherwise enter
// every player build, even though this game has no inference models or workers.
public sealed class StripUnusedInferenceShaders : IPreprocessBuildWithReport,
    IPreprocessComputeShaders, IPreprocessShaders
{
    static bool stripInference;
    public int callbackOrder => -200;

    public void OnPreprocessBuild(BuildReport report)
    {
        bool hasModels = AssetDatabase.FindAssets("t:ModelAsset", new[] { "Assets" }).Length > 0;
        var assetRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
        var sources = CompilationPipeline.GetAssemblies(AssembliesType.Player)
            .SelectMany(a => a.sourceFiles).Distinct()
            .Where(p => Path.GetFullPath(p).StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase));
        bool usesInference = sources.Any(path =>
        {
            var source = File.ReadAllText(path);
            return source.Contains("Unity.InferenceEngine") || source.Contains("Unity.Sentis");
        });
        // Preserve inference automatically if game code or a model is added later.
        stripInference = !hasModels && !usesInference;
    }

    static bool Unused(UnityEngine.Object shader)
    {
        return stripInference && AssetDatabase.GetAssetPath(shader).Replace('\\', '/')
            .StartsWith("Packages/com.unity.ai.inference/", StringComparison.Ordinal);
    }

    public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
    {
        if (Unused(shader)) data.Clear();
    }

    public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
    {
        if (Unused(shader)) data.Clear();
    }
}
