using UnityEngine;
using UnityEditor;

namespace TextureTools.Editor
{
    /// <summary>
    /// Demo window to test texture edge bleeding.
    /// </summary>
    public class TextureEdgeBleedingWindow : EditorWindow
    {
        enum WindowState
        {
            Normal = 0,
            GenerateUV,
            BleedTexture,
            Reset,
        }

        public Texture2D sourceTexture;
        public Mesh sourceMesh;
        public Texture2D destTexture;
        public Color[] destColors;

        Texture2D m_UVIslands;
        TextureEdgeBleeding m_TextureEdgeBleed = new TextureEdgeBleeding();

        Vector2 m_ScrollPos = Vector2.zero;
        int m_ImageRez = 128;
        WindowState m_CurrentState = WindowState.Normal;

        [MenuItem("Texture/TextureEdgeBleeding")]
        static void CreateWizard()
        {
            GetWindow<TextureEdgeBleedingWindow>("TextureEdgeBleeding");
        }

        void OnGUI()
        {
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Current State: " + m_CurrentState.ToString());
            GUI.backgroundColor = Color.green;

            sourceTexture = EditorGUILayout.ObjectField("Texture", sourceTexture, 
            typeof(Texture2D), false) as Texture2D;
            m_ImageRez =EditorGUILayout.IntField("Load at Image Resolution", m_ImageRez);
            sourceMesh = EditorGUILayout.ObjectField("Mesh", sourceMesh, 
            typeof(Mesh), false) as Mesh;

            EditorGUILayout.BeginHorizontal();
            TextureEdgeBleeding.debugIsland = EditorGUILayout.Toggle(
            "Show Pending Edge", TextureEdgeBleeding.debugIsland);
            TextureEdgeBleeding.debugFill = EditorGUILayout.Toggle(
            "Show Fill Steps", TextureEdgeBleeding.debugFill);
            EditorGUILayout.EndHorizontal();

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            if (sourceMesh != null && sourceTexture != null)
            {
                if (GUILayout.Button("Generate UV Islands") && WindowState.Normal == m_CurrentState)
                {
                    // do generate UV islands in update loop
                    m_CurrentState = WindowState.GenerateUV;
                }
            } 
            else 
            {
                if (m_UVIslands != null)
                {
                    DestroyImmediate(m_UVIslands);
                }
            }

            if (m_UVIslands != null)
            {
                GUILayout.Label(m_UVIslands);
            }

            if (destTexture != null)
            {
                if (GUILayout.Button("Reset Bleed Texture"))
                {
                    // do reset in update loop
                    m_CurrentState = WindowState.Reset;
                }
            }
            else
            {
                if (m_UVIslands != null && sourceTexture != null)
                {
                    if (GUILayout.Button("Generate Bleed Image") && WindowState.Normal == m_CurrentState)
                    {
                        // do web load and bleed on texture in update
                        m_CurrentState = WindowState.BleedTexture;
                    }
                }
            }

            if (destTexture != null)
            {
                GUILayout.Label(destTexture);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = previousColor;
        }

        void Update()
        {
            switch(m_CurrentState)
            {
                case WindowState.Normal:
                {
                    if (null != m_TextureEdgeBleed.loadTexture)
                    {
                        destTexture = m_TextureEdgeBleed.loadTexture.Copy();
                        m_TextureEdgeBleed.reset();
                    }
                    break;
                }
                case WindowState.BleedTexture:
                {
                    if (null == sourceTexture)
                    {
                        Debug.LogError ("Source Texture is NULL!");
                        m_CurrentState = WindowState.Normal;
                        break;
                    }
                    
                    if (null == sourceMesh)
                    {
                        Debug.LogError ("Source Mesh is NULL!");
                        m_CurrentState = WindowState.Normal;
                        break;
                    }
                    
                    if (!m_TextureEdgeBleed.locked )
                    {
                        m_TextureEdgeBleed.EditorBleedTexture(sourceMesh, sourceTexture, m_ImageRez, m_ImageRez);
                    }
                    
                    if (null != m_TextureEdgeBleed.loadTexture)
                    {
                        m_CurrentState = WindowState.Normal;
                    }
                    
                    break;
                }
                case WindowState.GenerateUV:
                {
                    m_UVIslands = new Texture2D(m_ImageRez, m_ImageRez);
                    m_UVIslands = sourceMesh.GetUVMask(0, m_ImageRez);
                    m_CurrentState = WindowState.Normal;
                    break;
                }
                case WindowState.Reset:
                {
                    destTexture = null;
                    m_TextureEdgeBleed.reset();
                    m_CurrentState = WindowState.Normal;
                    break;
                }
            }
        }
    }
}
