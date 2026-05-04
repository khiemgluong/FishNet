using UnityEditor;
using UnityEngine;

namespace FishNet.Object.Editing
{
    [CustomPropertyDrawer(typeof(PrefabPair))]
    public class PrefabPairDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty regularProp = property.FindPropertyRelative("regular");
            SerializedProperty networkProp = property.FindPropertyRelative("network");

            if (regularProp == null || networkProp == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            const float spacing = 4f;
            float halfWidth = (contentRect.width - spacing) / 2f;

            Rect regularRect = new Rect(contentRect.x, contentRect.y, halfWidth, contentRect.height);
            Rect networkRect = new Rect(contentRect.x + halfWidth + spacing, contentRect.y, halfWidth, contentRect.height);

            EditorGUI.ObjectField(regularRect, regularProp, typeof(GameObject), GUIContent.none);
            EditorGUI.ObjectField(networkRect, networkProp, typeof(NetworkObject), GUIContent.none);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;
    }
}
