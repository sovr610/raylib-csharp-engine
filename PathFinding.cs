using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class GridNode
{
    public Vector3 Position { get; set; }
    public bool Walkable { get; set; }
    public float G { get; set; }
    public float H { get; set; }
    public float F => G + H;
    public GridNode Parent { get; set; }

    public GridNode(Vector3 position, bool walkable)
    {
        Position = position;
        Walkable = walkable;
    }
}

public class Pathfinder
{
    private GridNode[,,] grid;
    private Vector3 gridWorldSize;
    private float nodeRadius;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY, gridSizeZ;

    public Pathfinder(Vector3 worldSize, float nodeRadius)
    {
        this.gridWorldSize = worldSize;
        this.nodeRadius = nodeRadius;
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(worldSize.X / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(worldSize.Y / nodeDiameter);
        gridSizeZ = Mathf.RoundToInt(worldSize.Z / nodeDiameter);
        CreateGrid();
    }

    private void CreateGrid()
    {
        grid = new GridNode[gridSizeX, gridSizeY, gridSizeZ];
        Vector3 worldBottomLeft = Vector3.Zero - gridWorldSize / 2f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.One * (nodeDiameter * new Vector3(x, y, z) + Vector3.One * nodeRadius);
                    bool walkable = !Physics.Raycast(worldPoint, Vector3.Up, nodeRadius);
                    grid[x, y, z] = new GridNode(worldPoint, walkable);
                }
            }
        }
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        GridNode startNode = NodeFromWorldPoint(startPos);
        GridNode targetNode = NodeFromWorldPoint(targetPos);

        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            GridNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].F < currentNode.F || openSet[i].F == currentNode.F && openSet[i].H < currentNode.H)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            foreach (var neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.Walkable || closedSet.Contains(neighbor))
                {
                    continue;
                }

                float newMovementCostToNeighbor = currentNode.G + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.G || !openSet.Contains(neighbor))
                {
                    neighbor.G = newMovementCostToNeighbor;
                    neighbor.H = GetDistance(neighbor, targetNode);
                    neighbor.Parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private List<Vector3> RetracePath(GridNode startNode, GridNode endNode)
    {
        List<Vector3> path = new List<Vector3>();
        GridNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        path.Reverse();
        return path;
    }

    private List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && y == 0 && z == 0)
                        continue;

                    int checkX = node.GridX + x;
                    int checkY = node.GridY + y;
                    int checkZ = node.GridZ + z;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY && checkZ >= 0 && checkZ < gridSizeZ)
                    {
                        neighbors.Add(grid[checkX, checkY, checkZ]);
                    }
                }
            }
        }

        return neighbors;
    }

    private float GetDistance(GridNode nodeA, GridNode nodeB)
    {
        float dstX = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        float dstY = Mathf.Abs(nodeA.GridY - nodeB.GridY);
        float dstZ = Mathf.Abs(nodeA.GridZ - nodeB.GridZ);

        if (dstX > dstY && dstX > dstZ)
            return 14 * dstY + 14 * dstZ + 10 * (dstX - Mathf.Max(dstY, dstZ));
        if (dstY > dstZ)
            return 14 * dstX + 14 * dstZ + 10 * (dstY - Mathf.Max(dstX, dstZ));
        return 14 * dstX + 14 * dstY + 10 * (dstZ - Mathf.Max(dstX, dstY));
    }

    private GridNode NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.X + gridWorldSize.X / 2) / gridWorldSize.X;
        float percentY = (worldPosition.Y + gridWorldSize.Y / 2) / gridWorldSize.Y;
        float percentZ = (worldPosition.Z + gridWorldSize.Z / 2) / gridWorldSize.Z;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        percentZ = Mathf.Clamp01(percentZ);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        int z = Mathf.RoundToInt((gridSizeZ - 1) * percentZ);
        return grid[x, y, z];
    }
}

public class SteeringBehavior
{
    public Vector3 Seek(Vector3 position, Vector3 target, Vector3 velocity, float maxSpeed, float maxForce)
    {
        Vector3 desired = Vector3.Normalize(target - position) * maxSpeed;
        return Vector3.Clamp(desired - velocity, -maxForce, maxForce);
    }

    public Vector3 Arrive(Vector3 position, Vector3 target, Vector3 velocity, float maxSpeed, float maxForce, float slowingDistance)
    {
        Vector3 toTarget = target - position;
        float distance = toTarget.Length();

        if (distance > 0)
        {
            float rampedSpeed = maxSpeed * (distance / slowingDistance);
            float clippedSpeed = Math.Min(rampedSpeed, maxSpeed);
            Vector3 desired = toTarget * (clippedSpeed / distance);
            return Vector3.Clamp(desired - velocity, -maxForce, maxForce);
        }

        return Vector3.Zero;
    }

    public Vector3 FollowPath(List<Vector3> path, Vector3 position, Vector3 velocity, float maxSpeed, float maxForce, float pathRadius)
    {
        if (path == null || path.Count == 0)
            return Vector3.Zero;

        Vector3 target = path[0];
        if (Vector3.DistanceSquared(position, target) < pathRadius * pathRadius)
        {
            path.RemoveAt(0);
            if (path.Count == 0)
                return Arrive(position, target, velocity, maxSpeed, maxForce, pathRadius);
        }

        return Seek(position, target, velocity, maxSpeed, maxForce);
    }
}