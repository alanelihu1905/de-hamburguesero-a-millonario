using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Tycoon3D.Editor
{
    public static class PrototypeWinBuild
    {
        private const string ScenePath = "Assets/_Game/Scenes/BarrioResidencial.unity";
        private const string ExePath = "Builds/Windows/De Hamburguesero a Millonario.exe";

        public static void Build()
        {
            if (!File.Exists(ScenePath))
                throw new InvalidOperationException("Falta la escena del prototipo: " + ScenePath);

            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException(
                    "Falta soporte Windows Build Support para Unity " + UnityEngine.Application.unityVersion +
                    ". Instálalo desde Unity Hub > Installs > Add modules.");

            Directory.CreateDirectory(Path.GetDirectoryName(ExePath));
            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, ExePath,
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report == null || report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build Windows falló: " +
                    (report == null ? "sin reporte" : report.summary.result.ToString()));

            UnityEngine.Debug.Log("TYCOON_WIN_BUILD_OK: " + ExePath + ", " +
                report.summary.totalSize + " bytes.");
        }
    }
}
