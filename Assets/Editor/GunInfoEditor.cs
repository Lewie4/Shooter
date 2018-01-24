using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof (GunInfo))]
public class GunInfoEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		GunInfo gunInfo = (GunInfo)target;

		DrawDefaultInspector ();

		if (GUILayout.Button ("Update")) 
		{
			gunInfo.SetupItem ();
		}
	}
}
