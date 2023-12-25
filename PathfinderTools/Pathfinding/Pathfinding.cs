using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;


namespace PathfinderTools
{
    static class Pathfinding
    {
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
