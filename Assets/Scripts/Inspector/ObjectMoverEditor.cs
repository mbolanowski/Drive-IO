using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectMover))]
public class ObjectMoverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default Inspector UI
        DrawDefaultInspector();

        // Get a reference to the ObjectMover script
        ObjectMover mover = (ObjectMover)target;

        // Add buttons for movement
        if (GUILayout.Button("Move Left"))
        {
            mover.MoveLeft();
        }

        if (GUILayout.Button("Move Right"))
        {
            mover.MoveRight();
        }

        if (GUILayout.Button("Move Up"))
        {
            mover.MoveUp();
        }

        if (GUILayout.Button("Move Down"))
        {
            mover.MoveDown();
        }
    }
}
