﻿using System;
using UnityEditor;
using UnityEngine;

namespace SpaceShooter {
	[CustomEditor(typeof(Asteroid))]
	public class AsteroidInspector : Editor {
		private Asteroid myTarget;
		
		private void Awake() {
			myTarget = (Asteroid) target;
		}

		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			if (GUILayout.Button("Generate Unique ID")) {
				myTarget.id = Guid.NewGuid().ToString();
			}
		}
	}
}