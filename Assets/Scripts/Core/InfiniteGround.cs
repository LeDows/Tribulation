using System;
using Tribulation.Config;
using UnityEngine;

namespace Tribulation.Core
{
    /// <summary>
    /// Keeps a fixed grid of ground tiles around the player and repositions the
    /// grid when the player crosses a tile boundary. The object count therefore
    /// stays constant no matter how far the player travels.
    /// </summary>
    public sealed class InfiniteGround : MonoBehaviour
    {
        private const int GridSize = 5;
        private const int MinimumGridOffset = -(GridSize / 2);
        private const int MaximumGridOffset = MinimumGridOffset + GridSize - 1;
        public const int TileCount = GridSize * GridSize;

        private Transform target;
        private Transform[] tiles = Array.Empty<Transform>();
        private Material groundMaterial;
        private Vector2 gridOrigin;
        private Vector2 tileSize;
        private Vector2Int currentCell = new(int.MinValue, int.MinValue);

        public void Configure(Transform followTarget, MapConfig map)
        {
            target = followTarget;

            var groundPrefab = RuntimePrefabCatalog.Ground;
            if (target == null || map == null || groundPrefab == null)
            {
                Debug.LogError("Infinite ground could not be configured because its target, map, or ground prefab is missing.");
                enabled = false;
                return;
            }

            gridOrigin = new Vector2(target.position.x, target.position.z);

            var prefabRenderer = groundPrefab.GetComponent<Renderer>();
            var prefabFilter = groundPrefab.GetComponent<MeshFilter>();
            if (prefabRenderer == null || prefabFilter == null || prefabFilter.sharedMesh == null)
            {
                Debug.LogError("The ground prefab must contain a Renderer and a MeshFilter with a mesh.");
                enabled = false;
                return;
            }

            tileSize = new Vector2(
                Mathf.Abs(prefabFilter.sharedMesh.bounds.size.x * map.groundScale.x),
                Mathf.Abs(prefabFilter.sharedMesh.bounds.size.z * map.groundScale.z));

            if (tileSize.x < 0.01f || tileSize.y < 0.01f)
            {
                Debug.LogError("Ground scale produces a zero-sized tile.");
                enabled = false;
                return;
            }

            groundMaterial = new Material(prefabRenderer.sharedMaterial)
            {
                color = map.groundColor,
                name = ConfigCenter.Text("runtime.ground.material_name", ConfigCenter.Text(map.displayNameKey))
            };

            tiles = new Transform[TileCount];
            var index = 0;
            for (var z = MinimumGridOffset; z <= MaximumGridOffset; z++)
            {
                for (var x = MinimumGridOffset; x <= MaximumGridOffset; x++)
                {
                    var tile = RuntimePrefabCatalog.Instantiate(groundPrefab, transform);
                    tile.name = $"Ground Tile ({x}, {z})";
                    tile.transform.localScale = map.groundScale;
                    tile.GetComponent<Renderer>().sharedMaterial = groundMaterial;
                    tiles[index++] = tile.transform;
                }
            }

            RefreshTiles(force: true);
        }

        private void LateUpdate()
        {
            if (!GameManager.IsSimulationRunning)
            {
                return;
            }

            RefreshTiles(force: false);
        }

        private void RefreshTiles(bool force)
        {
            if (target == null || tiles.Length == 0)
            {
                return;
            }

            var nextCell = new Vector2Int(
                Mathf.FloorToInt((target.position.x - gridOrigin.x + tileSize.x * 0.5f) / tileSize.x),
                Mathf.FloorToInt((target.position.z - gridOrigin.y + tileSize.y * 0.5f) / tileSize.y));

            if (!force && nextCell == currentCell)
            {
                return;
            }

            currentCell = nextCell;
            var index = 0;
            for (var z = MinimumGridOffset; z <= MaximumGridOffset; z++)
            {
                for (var x = MinimumGridOffset; x <= MaximumGridOffset; x++)
                {
                    var cellX = currentCell.x + x;
                    var cellZ = currentCell.y + z;
                    var tile = tiles[index++];
                    tile.position = new Vector3(
                        gridOrigin.x + cellX * tileSize.x,
                        0f,
                        gridOrigin.y + cellZ * tileSize.y);
                    tile.name = $"Ground Tile ({cellX}, {cellZ})";
                }
            }
        }

        private void OnDestroy()
        {
            if (groundMaterial != null)
            {
                Destroy(groundMaterial);
            }
        }
    }
}
