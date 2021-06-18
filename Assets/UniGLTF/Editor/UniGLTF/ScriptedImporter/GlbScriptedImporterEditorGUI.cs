using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif


namespace UniGLTF
{
    [CustomEditor(typeof(GlbScriptedImporter))]
    public class GlbScriptedImporterEditorGUI : ScriptedImporterEditor
    {
        GlbScriptedImporter m_importer;
        GltfParser m_parser;

        RemapEditorMaterial m_materialEditor;
        RemapEditorAnimation m_animationEditor;

        public override void OnEnable()
        {
            base.OnEnable();

            m_importer = target as GlbScriptedImporter;
            m_parser = new GltfParser();
            m_parser.ParsePath(m_importer.assetPath);

            var externalObjectMap = m_importer.GetExternalObjectMap();
            var materialGenerator = new GltfMaterialDescriptorGenerator();
            var materialKeys = m_parser.GLTF.materials.Select((_, i) => materialGenerator.Get(m_parser, i).SubAssetKey);
            var textureKeys = new GltfTextureDescriptorGenerator(m_parser).Get().GetEnumerable().Select(x => x.SubAssetKey);
            m_materialEditor = new RemapEditorMaterial(materialKeys.Concat(textureKeys), externalObjectMap);
            m_animationEditor = new RemapEditorAnimation(AnimationImporterUtil.EnumerateSubAssetKeys(m_parser.GLTF), externalObjectMap);
        }

        enum Tabs
        {
            Model,
            Animation,
            Materials,
        }
        static Tabs s_currentTab;

        public override void OnInspectorGUI()
        {
            s_currentTab = MeshUtility.TabBar.OnGUI(s_currentTab);
            GUILayout.Space(10);

            switch (s_currentTab)
            {
                case Tabs.Model:
                    base.OnInspectorGUI();
                    break;

                case Tabs.Animation:
                    m_animationEditor.OnGUI(m_importer, m_parser);
                    break;

                case Tabs.Materials:
                    m_materialEditor.OnGUI(m_importer, m_parser, new GltfTextureDescriptorGenerator(m_parser),
                    assetPath => $"{Path.GetFileNameWithoutExtension(assetPath)}.Textures",
                    assetPath => $"{Path.GetFileNameWithoutExtension(assetPath)}.Materials");
                    break;
            }
        }
    }
}
