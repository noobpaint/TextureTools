using UnityEngine;
using UnityEditor;

namespace TextureTools.Editor
{
    /// <summary>
    /// Texture web load window.
    /// </summary>
    public class TextureWebLoadWindow  : EditorWindow
    {
        enum WindowState
        {
            Normal = 0,
            TestLoad,
            Reset,
        }
        
        public Texture2D sourceTexture;
        public Texture2D destTexture;

        TextureWebLoad m_TextureWebLoad = new TextureWebLoad();

        Vector2 m_ScrollPos = Vector2.zero;

        int m_ImageRez = 128;
        
        WindowState m_CurrentState = WindowState.Normal;

        [MenuItem("Texture/TextureWebLoad")]
        static void CreateWizard()
        {
            GetWindow<TextureWebLoadWindow>("TextureWebLoad");
        }

        void OnGUI()
        {
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.grey;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Current State: {m_CurrentState.ToString()}");
            GUI.backgroundColor = Color.green;

            sourceTexture = EditorGUILayout.ObjectField("Texture", sourceTexture, 
            typeof(Texture2D), false) as Texture2D;
            if (sourceTexture != null)
            {
                EditorGUILayout.IntField("width", sourceTexture.width);
                EditorGUILayout.IntField("height", sourceTexture.height);
            }

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            if (sourceTexture != null)
            {
                GUILayout.Label(sourceTexture);
                m_ImageRez = EditorGUILayout.IntField(m_ImageRez);
                if(GUILayout.Button("Test Load resize") && WindowState.Normal == m_CurrentState)
                {
                    // do test load in update loop
                    m_CurrentState = WindowState.TestLoad;
                }
                
                if (destTexture != null)
                {
                    GUILayout.Label(destTexture);
                }
            }


            if (destTexture != null)
            {
                if (GUILayout.Button("Reset Load Texture"))
                {
                    // do reset in update loop
                    m_CurrentState = WindowState.Reset;
                }
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
                    if (null != m_TextureWebLoad.loadTexture)
                    {
                        destTexture = m_TextureWebLoad.loadTexture.Copy();
                        m_TextureWebLoad.reset();
                    }
                    
                    break;
                }
                case WindowState.TestLoad:
                {
                    if (!m_TextureWebLoad.locked && null != sourceTexture)
                    {
                        m_TextureWebLoad.EditorWebLoad(sourceTexture, m_ImageRez, m_ImageRez);
                    }
                    if (null != m_TextureWebLoad.loadTexture)
                    {
                        m_CurrentState = WindowState.Normal;
                    }
                    
                    break;
                }
                case WindowState.Reset:
                {
                    destTexture = null;
                    m_TextureWebLoad.reset();
                    m_CurrentState = WindowState.Normal;
                    break;
                }
            }
        }
    }
}
