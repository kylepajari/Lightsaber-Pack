using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using ThunderRoad;

namespace SaberMod
{
    public class Configuration : LevelModule
    {
        //set up global variables
        public static bool RecallAllowed { get; private set; }
        public static bool RecallTurnSaberOff { get; private set; }
        public static float RecallMaxDistance { get; private set; }
        public static float RecallStrength { get; private set; }
        public static float IgnitionSpeed { get; private set; }
        public static float IgnitionDelay { get; private set; }

        //default values
        public bool recallAllowed = true;
        public float recallMaxDistance = 4.0f;
        public bool recallTurnSaberOff = true;
        public float recallStrength = 15.0f;
        public float ignitionSpeed = 0.2f;
        public float ignitionDelay = 1.0f;

        public override System.Collections.IEnumerator OnLoadCoroutine(Level levelDefinition)
        {
            //assign values
            RecallAllowed = recallAllowed;
            RecallTurnSaberOff = recallTurnSaberOff;
            RecallMaxDistance = recallMaxDistance;
            RecallStrength = recallStrength;
            IgnitionSpeed = ignitionSpeed;
            IgnitionDelay = ignitionDelay;
            //hook up new scene load event
            SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(OnNewSceneLoaded);

            return base.OnLoadCoroutine(levelDefinition);
        }

        private void OnNewSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RecallAllowed = recallAllowed;
            RecallTurnSaberOff = recallTurnSaberOff;
            RecallMaxDistance = recallMaxDistance;
            RecallStrength = recallStrength;
            IgnitionSpeed = ignitionSpeed;
            IgnitionDelay = ignitionDelay;
        }

    }
}
