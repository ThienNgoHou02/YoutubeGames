using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR

public static class CopyPasteTransformComponent
{
    struct TransformData
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public TransformData(Transform t)
        {
            localPosition = t.localPosition;
            localRotation = t.localRotation;
            localScale = t.localScale;
        }
    }

    private static readonly List<TransformData> transformDatas = new();

    #region COPY

    [MenuItem("Auto/Transform/Copy Transform Values &c", false, -101)]
    public static void CopyTransformValues()
    {
        if (Selection.gameObjects.Length == 0) return;

        transformDatas.Clear();

        var sorted = GetSortedSelection();

        foreach (var go in sorted)
        {
            transformDatas.Add(new TransformData(go.transform));
        }
    }

    #endregion

    #region PASTE

    [MenuItem("Auto/Transform/Paste Transform Values &v", false, -101)]
    public static void PasteTransformValues()
    {
        if (Selection.gameObjects.Length == 0 || transformDatas.Count == 0) return;

        var sorted = GetSortedSelection();
        int amount = Mathf.Min(sorted.Length, transformDatas.Count);

        Undo.SetCurrentGroupName("Paste Transform Values");
        int group = Undo.GetCurrentGroup();

        for (int i = 0; i < amount; i++)
        {
            var t = sorted[i].transform;

            Undo.RecordObject(t, "Paste Transform Values");

            t.localPosition = transformDatas[i].localPosition;
            t.localRotation = transformDatas[i].localRotation;
            t.localScale = transformDatas[i].localScale;
        }

        Undo.CollapseUndoOperations(group);
    }

    #endregion

    #region INVERT

    [MenuItem("Auto/Transform/Invert Position X &t", false, -101)]
    public static void InvertPositionX()
    {
        if (Selection.gameObjects.Length == 0) return;

        Undo.SetCurrentGroupName("Invert Position X");
        int group = Undo.GetCurrentGroup();

        int invert = Selection.gameObjects[0].transform.position.x > 0 ? -1 : 1;

        foreach (var go in Selection.gameObjects)
        {
            var t = go.transform;

            Undo.RecordObject(t, "Invert Position X");

            var pos = t.position;
            pos.x = invert * Mathf.Abs(pos.x);
            t.position = pos;
        }

        Undo.CollapseUndoOperations(group);
    }

    [MenuItem("Auto/Transform/Invert Position Y &y", false, -101)]
    public static void InvertPositionY()
    {
        if (Selection.gameObjects.Length == 0) return;

        Undo.SetCurrentGroupName("Invert Position Y");
        int group = Undo.GetCurrentGroup();

        int invert = Selection.gameObjects[0].transform.position.y > 0 ? -1 : 1;

        foreach (var go in Selection.gameObjects)
        {
            var t = go.transform;

            Undo.RecordObject(t, "Invert Position Y");

            var pos = t.position;
            pos.y = invert * Mathf.Abs(pos.y);
            t.position = pos;
        }

        Undo.CollapseUndoOperations(group);
    }

    #endregion

    #region ROTATE

    [MenuItem("Auto/Transform/Rotate Y &r", false, -101)]
    public static void RotateY()
    {
        if (Selection.gameObjects.Length == 0) return;

        Undo.SetCurrentGroupName("Rotate Y");
        int group = Undo.GetCurrentGroup();

        foreach (var go in Selection.gameObjects)
        {
            var t = go.transform;

            Undo.RecordObject(t, "Rotate Y");

            var angle = t.eulerAngles;
            angle.y += 180;
            t.eulerAngles = angle;
        }

        Undo.CollapseUndoOperations(group);
    }

    [MenuItem("Auto/Transform/Rotate Random &%r", false, -101)]
    public static void RotateRandom()
    {
        if (Selection.gameObjects.Length == 0) return;

        Undo.SetCurrentGroupName("Rotate Random");
        int group = Undo.GetCurrentGroup();

        foreach (var go in Selection.gameObjects)
        {
            var t = go.transform;

            Undo.RecordObject(t, "Rotate Random");

            var angle = t.eulerAngles;
            angle.z += Random.Range(0, 360f);
            t.eulerAngles = angle;
        }

        Undo.CollapseUndoOperations(group);
    }

    #endregion

    #region BAKE SCALE (FIX ALL DESCENDANTS)

    [MenuItem("Auto/Transform/Bake Scale To Children &e", false, -101)]
    public static void BakeScaleToChildren()
    {
        if (Selection.gameObjects.Length == 0) return;

        Undo.SetCurrentGroupName("Bake Scale To Children");
        int group = Undo.GetCurrentGroup();

        foreach (var root in Selection.gameObjects)
        {
            Transform rootTransform = root.transform;
            Vector3 parentScale = rootTransform.localScale;

            if (parentScale == Vector3.one) continue;

            var allChildren = rootTransform.GetComponentsInChildren<Transform>(true);

            Dictionary<Transform, (Vector3 pos, Quaternion rot)> cache =
                new Dictionary<Transform, (Vector3, Quaternion)>();

            foreach (var t in allChildren)
            {
                if (t == rootTransform) continue;
                cache[t] = (t.position, t.rotation);
            }

            foreach (Transform child in rootTransform)
            {
                Undo.RecordObject(child, "Bake Scale");

                child.localScale = Vector3.Scale(child.localScale, parentScale);
            }

            Undo.RecordObject(rootTransform, "Reset Scale");
            rootTransform.localScale = Vector3.one;

            foreach (var kvp in cache)
            {
                Undo.RecordObject(kvp.Key, "Restore Transform");

                kvp.Key.position = kvp.Value.pos;
                kvp.Key.rotation = kvp.Value.rot;
            }
        }

        Undo.CollapseUndoOperations(group);
    }

    #endregion

    #region ADD SQUARE

    [MenuItem("Auto/Transform/Add Square &s", false, -101)]
    public static void AddSquare()
    {
        if (Selection.gameObjects.Length == 0) return;

        Undo.SetCurrentGroupName("Add Square");
        int group = Undo.GetCurrentGroup();

        foreach (var item in Selection.gameObjects)
        {
            if (item.transform.childCount > 0 && item.transform.GetChild(0).name == "Square")
                continue;

            var sr = item.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            Vector3 originalScale = item.transform.localScale;
            Quaternion originalRot = item.transform.localRotation;

            GameObject square = new GameObject("Square");
            Undo.RegisterCreatedObjectUndo(square, "Create Square");

            GameObject go = new GameObject(item.name);
            Undo.RegisterCreatedObjectUndo(go, "Create Child");

            var newSR = go.AddComponent<SpriteRenderer>();
            newSR.sprite = sr.sprite;

            Undo.DestroyObjectImmediate(sr);

            Undo.RecordObject(item.transform, "Reset Root");
            item.transform.localRotation = Quaternion.identity;
            item.transform.localScale = Vector3.one;
            Undo.SetTransformParent(square.transform, item.transform, "Set Parent");
            Undo.SetTransformParent(go.transform, square.transform, "Set Parent");

            square.transform.localPosition = Vector3.zero;
            square.transform.localScale = Vector3.one;

            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = originalRot;
            go.transform.localScale = originalScale;
        }

        Undo.CollapseUndoOperations(group);
    }

    #endregion

    #region UTILS

    private static GameObject[] GetSortedSelection()
    {
        var arr = Selection.gameObjects;
        System.Array.Sort(arr, (a, b) => string.Compare(a.name, b.name));
        return arr;
    }

    #endregion
}

#endif