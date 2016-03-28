/*
---------------------- Terrain Water Erosion Editor ----------------------
-- TerrainWaterErosionEditor.cs
--
-- Code and algorithm by Dmitry Soldatenkov
-- Based on Terrain Toolkit by Sándor Moldán. 
--
-------------------------------------------------------------------
*/
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(TerrainWaterErosion))]
public class TerrainWaterErosionEditor : Editor {

    public override void OnInspectorGUI () {
        TerrainWaterErosion weTerrain = (TerrainWaterErosion)target as TerrainWaterErosion;
        if (!weTerrain.gameObject) {
            return;
        }
        Terrain terComponent = (Terrain)weTerrain.GetComponent (typeof(Terrain));
        if (terComponent == null) {
            EditorGUILayout.HelpBox("The GameObject that Terrain Water Erosion is attached to does not have a Terrain component. Please attach a Terrain component.", 
                                     MessageType.Warning);
            return;
        }

        if (GUI.changed) {
            EditorUtility.SetDirty (weTerrain);
        }
        GUI.changed = false;

        EditorGUILayout.BeginHorizontal ();
        EditorGUILayout.PrefixLabel ("Iterations");
        weTerrain.waterErosionIterations = (int)EditorGUILayout.Slider (weTerrain.waterErosionIterations, 1, 250);
        EditorGUILayout.EndHorizontal ();

        EditorGUILayout.BeginHorizontal ();
        EditorGUILayout.PrefixLabel ("Rainfall");
        weTerrain.waterErosionRainfall = EditorGUILayout.Slider (weTerrain.waterErosionRainfall, 0, 10);
        EditorGUILayout.EndHorizontal ();

        EditorGUILayout.BeginHorizontal ();
        EditorGUILayout.PrefixLabel ("Stream Turbulence");
        weTerrain.waterErosionConeThreshold = EditorGUILayout.Slider (weTerrain.waterErosionConeThreshold, 1, 5);
        EditorGUILayout.EndHorizontal ();

        EditorGUILayout.BeginHorizontal ();
        EditorGUILayout.PrefixLabel ("Water Flow Iterations");
        weTerrain.waterFlowIterations = (int)EditorGUILayout.Slider (weTerrain.waterFlowIterations, 1, 512);
        EditorGUILayout.EndHorizontal ();

        Rect buttonRect = EditorGUILayout.BeginHorizontal ();
        buttonRect.x = buttonRect.width / 2 - 100;
        buttonRect.width = 200;
        buttonRect.height = 28;
        if (GUI.Button (buttonRect, "Apply Water Erosion")) {
            DateTime startTime = DateTime.Now;
            weTerrain.processTerrain ();
            EditorUtility.ClearProgressBar ();
            TimeSpan processTime = DateTime.Now - startTime;
            Debug.Log ("Process complete in: " + processTime.ToString ());
            GUIUtility.ExitGUI ();
        }
        EditorGUILayout.EndHorizontal ();
        for (int i = 0; i < 6; i++)
            EditorGUILayout.Separator ();

        if (GUI.changed) {
            EditorUtility.SetDirty (weTerrain);
        }
    }

}

