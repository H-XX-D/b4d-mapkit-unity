using System.IO;
using UnityEditor;

namespace B4D
{
    /// Where the kit puts what it makes.
    ///
    /// Baked meshes and exported maps go to fixed folders rather than wherever
    /// the last save dialog happened to be pointing, so a project stays tidy on
    /// its own and a map file can find its meshes in a predictable place.
    ///
    ///     Assets/B4D/Meshes/    baked .glb files
    ///     Assets/B4D/Maps/      exported campaign json
    ///
    /// Nothing forces you to use them. The save dialogs simply start there.
    public static class B4DProjectPaths
    {
        public const string Root = "Assets/B4D";
        public const string Meshes = Root + "/Meshes";
        public const string Maps = Root + "/Maps";

        /// Creates the folder if it is missing and returns it, ready for a dialog.
        public static string Ensure(string projectRelativePath)
        {
            if (!Directory.Exists(projectRelativePath))
            {
                Directory.CreateDirectory(projectRelativePath);
                AssetDatabase.Refresh();
            }
            return projectRelativePath;
        }

        public static string EnsureMeshes() => Ensure(Meshes);
        public static string EnsureMaps() => Ensure(Maps);
    }
}
