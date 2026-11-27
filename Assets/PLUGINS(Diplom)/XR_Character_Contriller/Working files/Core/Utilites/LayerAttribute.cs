using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Атрибут для отображения выпадающего списка слоев Unity в инспекторе.
/// Использование: [Layer] [SerializeField] private int _unitLayer;
/// </summary>
public class LayerAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(LayerAttribute))]
public class LayerDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif
