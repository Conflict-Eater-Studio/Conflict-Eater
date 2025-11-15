using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(PowerupSpawner))]
public class PowerupSpawnerEditor : Editor
{
    private VisualTreeAsset _visualTree;

    private void OnEnable()
    {
        _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Assets/_Editor/PowerupSpawnerEditorUXML.uxml"
        );
    }

    public override VisualElement CreateInspectorGUI()
    {
        if (_visualTree == null)
        {
            return new Label("Nie znaleziono pliku UXML!");
        }

        var root = new VisualElement();
        _visualTree.CloneTree(root);

        // Przykład przypięcia pól
        var spawner = (PowerupSpawner)target;

        /*
        var powerupField = root.Q<ObjectField>("powerup");
        if (powerupField != null)
        {
            powerupField.objectType = typeof(GameObject);
            powerupField.value = spawner.powerupPrefab;

            powerupField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(spawner, "Zmiana prefabu powerupa");
                spawner.powerupPrefab = evt.newValue as GameObject;
            });
        }
        */

        return root;
    }
}
