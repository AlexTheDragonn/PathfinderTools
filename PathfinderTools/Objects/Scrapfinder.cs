using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameNetcodeStuff;

namespace PathfinderTools.Objects
{
    class Scrapfinder : GrabbableObject
    {
        public LineRenderer scrapLine;

        public Material onMaterial;
        public Material offMaterial;

        public AudioClip switchScrapFinderPoweringOn;
        public AudioClip switchScrapFinderPoweringOff;
        public AudioClip scanningScrapFinder;

        public Light scrapFinderLight;

        private bool isTurningOn;
        private bool isTurnedOn;

        private List<GrabbableObject> scraps;
        private Coroutine turningOnCoroutine = null;
        private float updateInterval;

        public override void Update()
        {
            base.Update();
            if (updateInterval >= 0f)
            {
                updateInterval -= Time.deltaTime;
                return;
            }
            updateInterval = 1f;
            if (isTurnedOn)
            {
                ScrapFinder(playerHeldBy.transform.position);
            }
        }

        public override void ItemInteractLeftRight(bool right)
        {
            base.ItemInteractLeftRight(right);
            if (right)
            {
                return;
            }
            //Plugin.logger.LogInfo("Toggling Scrap Finder");
            SwitchScrapFinderOn(!(isTurnedOn || isTurningOn));
        }

        public void SwitchScrapFinderOn(bool on)
        {
            isTurningOn = on;
            if (on)
            {
                turningOnCoroutine = StartCoroutine(BootUpScrapFinder());
                return;
            }
            if(turningOnCoroutine != null)
            {
                StopCoroutine(turningOnCoroutine);
            }
            //play switchScrapFinderPoweringOff
            scrapLine.enabled = false;
            isTurnedOn = false;
        }

        public IEnumerator BootUpScrapFinder()
        {
            //Plugin.logger.LogInfo("Booting up scrap finder.");
            //Start playing switchScrapFinderPoweringOn
            yield return new WaitForSeconds(3);

            if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory)
            {
                //Plugin.logger.LogInfo("Not inside building");
                //Display: "ERR: NOT INSIDE BUILDING"
                isTurningOn = false;
                yield break;
            }

            isTurningOn = false;
            isTurnedOn = true;
            yield break;
        }

        public void GetAllValidScrap()
        {
            scraps = FindObjectsOfType<GrabbableObject>().ToList();
            for (int i = scraps.Count - 1; i >= 0; i--)
            {
                GrabbableObject scrap = scraps[i];
                if (scrap.isHeld || !scrap.isInFactory || !scrap.itemProperties.isScrap || scrap.itemProperties.itemName == "Hive")
                {
                    scraps.RemoveAt(i);
                }
            }
        }

        public GrabbableObject GetClosestValidScrap(List<GrabbableObject> scraps, Vector3 pos)
        {
            if (scraps == null)
                return null;

            List<Transform> transforms = scraps.Select(grabbableObject => grabbableObject.transform).ToList();
            return scraps[Pathfinding.GetClosestTransform(transforms, pos)];
        }

        public void ScrapFinder(Vector3 playerPosition)
        {
            //Plugin.logger.LogInfo("Scrap finder");
            if (!playerHeldBy.isInsideFactory || playerHeldBy.isPlayerDead)
                return;

            //Play scanningScrapFinder
            GetAllValidScrap();
            GrabbableObject closestValidScrap = GetClosestValidScrap(scraps, playerPosition);
            if (closestValidScrap == null)
            {
                //Plugin.logger.LogInfo("Can't find scrap, turning off");
                SwitchScrapFinderOn(false);
                return;
            }

            //Plugin.logger.LogInfo("Drawing path");
            Pathfinding.DrawPathToTransform(closestValidScrap.transform, playerPosition, scrapLine);
        }



    }

}
