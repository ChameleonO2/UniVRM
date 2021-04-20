using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniGLTF.Animation;
using UnityEditor;
using UnityEngine;
using VRMShaders;

namespace UniGLTF
{
    public class GltfExportWindow : ExportDialogBase
    {
        const string MENU_KEY = UniGLTFVersion.MENU + "/Export " + UniGLTFVersion.UNIGLTF_VERSION;

        [MenuItem(MENU_KEY, false, 0)]
        private static void ExportFromMenu()
        {
            var window = (GltfExportWindow)GetWindow(typeof(GltfExportWindow));
            window.titleContent = new GUIContent("Gltf Exporter");
            window.Show();
        }

        private static void Export(GameObject go, string path, MeshExportSettings settings, Axises inverseAxis)
        {
        }

        GltfExportSettings m_settings;
        Editor m_settingsInspector;

        protected override void Initialize()
        {
            m_settings = ScriptableObject.CreateInstance<GltfExportSettings>();
            m_settings.InverseAxis = UniGLTFPreference.GltfIOAxis;
            m_settingsInspector = Editor.CreateEditor(m_settings);
        }

        protected override void Clear()
        {
            // m_settingsInspector
            UnityEditor.Editor.DestroyImmediate(m_settingsInspector);
            m_settingsInspector = null;
        }

        protected override IEnumerable<Validator> ValidatorFactory()
        {
            yield return HierarchyValidator.ValidateRoot;
            yield return AnimationValidator.Validate;
            if (!State.ExportRoot)
            {
                yield break;
            }
        }
        protected override bool DoGUI()
        {
            m_settings.Root = State.ExportRoot;
            m_settingsInspector.OnInspectorGUI();
            return true;
        }

        protected override string SaveTitle => "Save gltf";
        protected override string SaveName => $"{State.ExportRoot.name}.glb";
        protected override string[] SaveExtensions => new string[] { "glb", "gltf" };

        protected override void ExportPath(string path)
        {
            var ext = Path.GetExtension(path).ToLower();
            var isGlb = false;
            switch (ext)
            {
                case ".glb": isGlb = true; break;
                case ".gltf": isGlb = false; break;
                default: throw new System.Exception();
            }

            var gltf = new glTF();
            using (var exporter = new gltfExporter(gltf, m_settings.InverseAxis))
            {
                exporter.Prepare(State.ExportRoot);
                var settings = new MeshExportSettings
                {
                    ExportOnlyBlendShapePosition = m_settings.DropNormal,
                    UseSparseAccessorForMorphTarget = m_settings.Sparse,
                    DivideVertexBuffer = m_settings.DivideVertexBuffer,
                };
                exporter.Export(settings, AssetTextureUtil.IsTextureEditorAsset, AssetTextureUtil.GetTextureBytesWithMime);
            }

            if (isGlb)
            {
                var bytes = gltf.ToGlbBytes();
                File.WriteAllBytes(path, bytes);
            }
            else
            {
                var (json, buffers) = gltf.ToGltf(path);
                // without BOM
                var encoding = new System.Text.UTF8Encoding(false);
                File.WriteAllText(path, json, encoding);
                // write to local folder
                var dir = Path.GetDirectoryName(path);
                foreach (var b in buffers)
                {
                    var bufferPath = Path.Combine(dir, b.uri);
                    File.WriteAllBytes(bufferPath, b.GetBytes().ToArray());
                }
            }

            if (path.StartsWithUnityAssetPath())
            {
                AssetDatabase.ImportAsset(path.ToUnityRelativePath());
                AssetDatabase.Refresh();
            }

        }
    }
}
