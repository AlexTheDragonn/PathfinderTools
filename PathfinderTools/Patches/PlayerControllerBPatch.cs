using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameNetcodeStuff;
using HarmonyLib;

namespace PathfinderTools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    class PlayerControllerBPatch
    {
        private static LineRenderer exitLine = null;

        private static List<EntranceTeleport> exitTeleports;


        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch()
        {
            GameObject exitLineGO = new GameObject("ExitFinderLine", typeof(LineRenderer));
            exitLine = exitLineGO.GetComponent<LineRenderer>();
            exitLine.startColor = new Color(255, 0, 0);
            exitLine.endColor = new Color(0, 255, 0);
            exitLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

            GetAllValidExitTeleports();
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        static void UpdatePatch()
        {
            //DEBUG: REMOVE BEFORE UPLOAD
            try
            {
                GameNetworkManager.Instance.localPlayerController.sprintMeter = 10f;
            }
            catch(Exception e)
            {                
            }
            
            try
            {
                Vector3 playerPosition = GameNetworkManager.Instance.localPlayerController.transform.position;
                ExitFinder(playerPosition);
            }
            catch(Exception e)
            {
                Plugin.logger.LogWarning($"Can't get GameNetworkManager.Instance.localPlayerController. This is fine during startup, but not mid-game.\n{e}");
            }
        }

        static void ExitFinder(Vector3 playerPosition)
        {
            if (GameNetworkManager.Instance.localPlayerController.isInElevator || GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                return;

            GetAllValidExitTeleports();
            List<Transform> transforms = exitTeleports.Select(entranceTeleport => entranceTeleport.entrancePoint).ToList();
            EntranceTeleport closest = exitTeleports[Pathfinding.GetClosestTransform(transforms, playerPosition)];

            //Plugin.logger.LogInfo($"Player position: {playerPosition} Closest Object: {closest.transform.position} [{closest.isEntranceToBuilding}, {closest.entranceId}]");
            Pathfinding.DrawPathToTransform(closest.entrancePoint, playerPosition, exitLine);
        }

        public static void GetAllValidExitTeleports()
        {
            exitTeleports = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>().ToList();
            RemoveInvalidExit(exitTeleports);
        }

        public static void RemoveInvalidExit(List<EntranceTeleport> entranceTeleports)
        {
            for (int i = entranceTeleports.Count - 1; i >= 0; i--)
            {
                EntranceTeleport eT = entranceTeleports[i];
                // Check if the entrance is relevant (only want isEntranceToBuilding when we're NOT inside)
                if (eT.isEntranceToBuilding == GameNetworkManager.Instance.localPlayerController.isInsideFactory)
                {
                    entranceTeleports.RemoveAt(i);
                }
            }
        }

    }
}
