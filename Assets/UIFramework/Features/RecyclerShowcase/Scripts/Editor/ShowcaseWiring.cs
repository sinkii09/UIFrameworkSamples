using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace RecyclerShowcase.Editor
{
    /// <summary>
    /// Writes private [SerializeField]s through SerializedObject. Every lookup throws naming the
    /// missing path: a renamed field otherwise writes nothing and the scene fails only on Play.
    /// </summary>
    internal static class ShowcaseWiring
    {
        internal static void Write(Object target, Action<SerializedObject> write)
        {
            var serialized = new SerializedObject(target);
            write(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static SerializedProperty Prop(SerializedObject serialized, string path)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
                throw new InvalidOperationException(
                    $"[RecyclerShowcase] No serialized field '{path}' on {serialized.targetObject.GetType().Name} " +
                    "— it was renamed; update the scene builder.");
            return property;
        }

        internal static void Ref(Object target, string path, Object value)
        {
            if (value == null)
                throw new InvalidOperationException($"[RecyclerShowcase] Refusing to wire null into '{path}'.");
            Write(target, s => Prop(s, path).objectReferenceValue = value);
        }

        internal static void Refs<T>(Object target, string path, IReadOnlyList<T> values) where T : Object
        {
            Write(target, s =>
            {
                SerializedProperty array = Prop(s, path);
                array.arraySize = values.Count;
                for (int i = 0; i < values.Count; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            });
        }
    }
}
