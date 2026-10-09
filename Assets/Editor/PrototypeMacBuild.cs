using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Tycoon3D.Editor
{
    public static class PrototypeMacBuild
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string AppPath = "Builds/Mac/De Hamburguesero a Millonario.app";

        public static void Build()
        {
            if (!File.Exists(ScenePath)) throw new InvalidOperationException("Falta la escena del prototipo.");
            Directory.CreateDirectory("Builds/Mac");
            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, AppPath,
                BuildTarget.StandaloneOSX, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build macOS falló: " + report.summary.result);
            UnityEngine.Debug.Log("TYCOON_MAC_BUILD_OK: " + AppPath + ", " + report.summary.totalSize + " bytes.");
        }
    }
}
