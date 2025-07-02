using UnityEditor;
using UnityEngine;

namespace Nakatani
{
    [CustomEditor(typeof(SpectateManager))]
    public class SpectateManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのインスペクターを描画
            DrawDefaultInspector();

            // SpectateManagerのインスタンスを取得
            SpectateManager manager = (SpectateManager)target;

            // スペースを追加
            EditorGUILayout.Space();

            // タイトルラベル
            EditorGUILayout.LabelField("Spectate Controls", EditorStyles.boldLabel);

            // 水平レイアウトでボタンを配置
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Previous"))
            {
                // 再生中のみ動作
                if (Application.isPlaying)
                {
                    manager.SpectatePreviousPlayer();
                }
                else
                {
                    Debug.LogWarning("Spectate controls only work in Play Mode.");
                }
            }

            if (GUILayout.Button("Random"))
            {
                // 再生中のみ動作
                if (Application.isPlaying)
                {
                    manager.StartSpectatingRandomPlayer();
                }
                else
                {
                    Debug.LogWarning("Spectate controls only work in Play Mode.");
                }
            }

            if (GUILayout.Button("Next"))
            {
                // 再生中のみ動作
                if (Application.isPlaying)
                {
                    manager.SpectateNextPlayer();
                }
                else
                {
                    Debug.LogWarning("Spectate controls only work in Play Mode.");
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
