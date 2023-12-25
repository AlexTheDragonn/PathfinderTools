using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine.AI;

namespace PathfinderTools.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    class PlayerControllerBPatch
    {
        private static LineRenderer scrapLine = null;
        private static LineRenderer exitLine = null;


        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        static void StartPatch()
        {
            GameObject scrapLineGO = new GameObject("ScrapFinderLine", typeof(LineRenderer));
            scrapLine = scrapLineGO.GetComponent<LineRenderer>();
            scrapLine.startColor = new Color(0, 0, 255);
            scrapLine.endColor = new Color(0, 0, 255);
            scrapLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));


            GameObject exitLineGO = new GameObject("ExitFinderLine", typeof(LineRenderer));
            exitLine = exitLineGO.GetComponent<LineRenderer>();
            exitLine.startColor = new Color(255, 0, 0);
            exitLine.endColor = new Color(0, 255, 0);
            exitLine.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
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
                ScrapFinder(playerPosition);
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

            List<EntranceTeleport> exitTeleports = UnityEngine.Object.FindObjectsOfType<EntranceTeleport>().ToList();
            RemoveInvalidExit(exitTeleports);
            List<Transform> transforms = exitTeleports.Select(entranceTeleport => entranceTeleport.entrancePoint).ToList();
            EntranceTeleport closest = exitTeleports[GetClosestTransform(transforms, playerPosition)];

            //Plugin.logger.LogInfo($"Player position: {playerPosition} Closest Object: {closest.transform.position} [{closest.isEntranceToBuilding}, {closest.entranceId}]");
            DrawPathToTransform(closest.entrancePoint, playerPosition, exitLine);
        }

        public static void ScrapFinder(Vector3 playerPosition)
        {
            if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory || GameNetworkManager.Instance.localPlayerController.isPlayerDead)
                return;


            List<GrabbableObject> scraps = UnityEngine.Object.FindObjectsOfType<GrabbableObject>().ToList();
            GrabbableObject closestValidScrap = GetClosestValidScrap(scraps, playerPosition);
            if (closestValidScrap == null)
                return;

            DrawPathToTransform(closestValidScrap.transform, playerPosition, scrapLine);
        }
        
        public static GrabbableObject GetClosestValidScrap(List<GrabbableObject> scraps, Vector3 pos)
        {
            if (scraps == null)
                return null;

            RemoveInvalidScrap(scraps);
            List<Transform> transforms = scraps.Select(grabbableObject => grabbableObject.transform).ToList();
            return scraps[GetClosestTransform(transforms, pos)];
        }

        public static int GetClosestTransform<T>(List<T> list, Vector3 pos) where T : Transform
        {
            float minDist = Mathf.Infinity;
            int i = 0;
            int index = 0;
            foreach (Transform item in list)
            {
                //Pathfinding distance
                NavMeshPath path = new NavMeshPath();

                if (NavMesh.CalculatePath(pos, item.position, -1, path))
                {
                    float dist = Vector3.Distance(pos, path.corners[0]);

                    for (int j = 1; j < path.corners.Length; j++)
                    {
                        dist += Vector3.Distance(path.corners[j - 1], path.corners[j]);
                    }

                    if (dist < minDist)
                    {
                        minDist = dist;
                        index = i;
                    }
                }
                i++;
            }
            return index;
        }

        public static void RemoveInvalidScrap(List<GrabbableObject> grabbableObjects)
        {
            for (int i = grabbableObjects.Count - 1; i >= 0; i--)
            {
                GrabbableObject scrap = grabbableObjects[i];
                if (scrap.isHeld || !scrap.isInFactory || !scrap.itemProperties.isScrap || scrap.itemProperties.itemName == "Hive")
                {
                    grabbableObjects.RemoveAt(i);
                }
            }
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

        public static void DrawPathToTransform<T>(T transform, Vector3 pos, LineRenderer lineRenderer) where T : Transform
        {
            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(pos, transform.position, NavMesh.AllAreas, path))
            {
                lineRenderer.positionCount = path.corners.Length;
                lineRenderer.SetPositions(path.corners);
            }
            else
            {
                //Plugin.logger.LogError($"Can't draw path between {pos} and {transform.position}");
            }

        }

    }
}
