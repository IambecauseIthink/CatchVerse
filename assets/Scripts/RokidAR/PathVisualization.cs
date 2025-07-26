using System.Collections.Generic;
using UnityEngine;

namespace RokidAR
{
    public class PathVisualization : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private LineRenderer pathLine;
        [SerializeField] private GameObject pathPointPrefab;
        [SerializeField] private Color pathColor = Color.cyan;
        [SerializeField] private float pathWidth = 0.02f;
        [SerializeField] private bool showPaths = true;
        
        [Header("Point Visualization")]
        [SerializeField] private GameObject[] pathPointObjects;
        [SerializeField] private float pointSize = 0.1f;
        [SerializeField] private bool showDirection = true;
        
        private List<Vector3> currentPath = new List<Vector3>();
        private ProceduralPathMovement pathMovement;
        
        private void Start()
        {
            pathMovement = GetComponent<ProceduralPathMovement>();
            SetupVisualization();
        }
        
        private void Update()
        {
            if (showPaths && pathMovement != null)
            {
                UpdatePathVisualization();
            }
        }
        
        private void SetupVisualization()
        {
            if (pathLine == null)
            {
                GameObject lineObj = new GameObject("PathLine");
                lineObj.transform.SetParent(transform);
                pathLine = lineObj.AddComponent<LineRenderer>();
            }
            
            pathLine.material = new Material(Shader.Find("Sprites/Default"));
            pathLine.startColor = pathColor;
            pathLine.endColor = pathColor;
            pathLine.startWidth = pathWidth;
            pathLine.endWidth = pathWidth;
            pathLine.useWorldSpace = true;
            pathLine.enabled = showPaths;
        }
        
        public void UpdatePathVisualization()
        {
            if (pathMovement == null) return;
            
            currentPath = pathMovement.GetCurrentPath();
            
            if (currentPath.Count > 1)
            {
                DrawPathLine();
                CreatePathPoints();
            }
        }
        
        private void DrawPathLine()
        {
            pathLine.positionCount = currentPath.Count;
            pathLine.SetPositions(currentPath.ToArray());
        }
        
        private void CreatePathPoints()
        {
            // Clean up old points
            if (pathPointObjects != null)
            {
                foreach (var point in pathPointObjects)
                {
                    if (point != null)
                        DestroyImmediate(point);
                }
            }
            
            pathPointObjects = new GameObject[currentPath.Count];
            
            for (int i = 0; i < currentPath.Count; i++)
            {
                GameObject pointObj = CreatePathPoint(currentPath[i], i);
                pathPointObjects[i] = pointObj;
            }
        }
        
        private GameObject CreatePathPoint(Vector3 position, int index)
        {
            GameObject point;
            
            if (pathPointPrefab != null)
            {
                point = Instantiate(pathPointPrefab, position, Quaternion.identity);
            }
            else
            {
                point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                point.transform.localScale = Vector3.one * pointSize;
                point.GetComponent<Renderer>().material.color = pathColor;
            }
            
            point.transform.SetParent(transform);
            point.name = $"PathPoint_{index}";
            
            return point;
        }
        
        public void SetPathColor(Color color)
        {
            pathColor = color;
            if (pathLine != null)
            {
                pathLine.startColor = color;
                pathLine.endColor = color;
            }
        }
        
        public void ToggleVisualization(bool visible)
        {
            showPaths = visible;
            if (pathLine != null)
                pathLine.enabled = visible;
            
            if (pathPointObjects != null)
            {
                foreach (var point in pathPointObjects)
                {
                    if (point != null)
                        point.SetActive(visible);
                }
            }
        }
        
        public void ClearVisualization()
        {
            if (pathLine != null)
            {
                pathLine.positionCount = 0;
            }
            
            if (pathPointObjects != null)
            {
                foreach (var point in pathPointObjects)
                {
                    if (point != null)
                        DestroyImmediate(point);
                }
                pathPointObjects = null;
            }
        }
    }
}