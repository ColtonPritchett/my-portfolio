using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WFC : MonoBehaviour
{
    [SerializeField] 
    public Tilemap floorMap;
    
    [SerializeField]
    public TileBase[] grass;
    [SerializeField]
    public TileBase[] path;
    [SerializeField] 
    public TileBase[] leftPath;
    [SerializeField]
    public TileBase[] rightPath;
    [SerializeField]
    public TileBase[] upPath;
    [SerializeField]
    public TileBase[] downPath;
    
    [SerializeField]
    public TileBase[] upLeftPath;
    [SerializeField]
    public TileBase[] upRightPath;
    [SerializeField]
    public TileBase[] downLeftPath;
    [SerializeField]
    public TileBase[] downRightPath;
    
    [SerializeField]
    public TileBase[] upLeftCorner;
    [SerializeField]
    public TileBase[] upRightCorner;
    [SerializeField]
    public TileBase[] downLeftCorner;
    [SerializeField]
    public TileBase[] downRightCorner;
    
    public void Start()
    {
        TileBase[][] masterTiles = {grass, path, leftPath, rightPath, upPath, downPath, upLeftPath, upRightPath, downLeftPath, downRightPath, upLeftCorner, upRightCorner, downLeftCorner, downRightCorner};
        
        //left, right, up, down
        //0 = grassy, 1 = pathy, 2 = left pathy, 3 = right pathy, 4 = up pathy, 5 = down pathy
        Dictionary<TileBase[], (int, int)[][]> rules = new Dictionary<TileBase[], (int, int)[][]>{ 
            {grass, new[] {new[] {(0, 10), (1, 2)}, new[] {(0, 10), (1, 2)}, new[] {(0, 10), (1, 2)}, new[] {(0, 10), (1, 2)}}},
            {path, new[] {new[] {(1, 10), (0, 2)}, new[] {(1, 10), (0, 2)}, new[] {(1, 10), (0, 2)}, new[] {(1, 10), (0, 2)}}},
            {leftPath, new[] {new[] {(1, 10)}, new[] {(0, 10)}, new[] {(2, 10)}, new[] {(2, 10)}}},
            {rightPath, new[] {new[] {(0, 10)}, new[] {(1, 10)}, new[] {(3, 10)}, new[] {(3, 10)}}},
            {upPath, new[] {new[] {(4, 10)}, new[] {(4, 10)}, new[] {(1, 10)}, new[] {(0, 10)}}},
            {downPath, new[] {new[] {(5, 10)}, new[] {(5, 10)}, new[] {(0, 10)}, new[] {(1, 10)}}},
            {upLeftPath, new[] {new[] {(4, 10)}, new[] {(0, 10)}, new[] {(2, 10)}, new[] {(0, 10)}}},
            {upRightPath, new[] {new[] {(0, 10)}, new[] {(4, 10)}, new[] {(3, 10)}, new[] {(0, 10)}}},
            {downLeftPath, new[] {new[] {(5, 10)}, new[] {(0, 10)}, new[] {(0, 10)}, new[] {(2, 10)}}},
            {downRightPath, new[] {new[] {(0, 10)}, new[] {(5, 10)}, new[] {(0, 10)}, new[] {(3, 10)}}},
            {upLeftCorner, new[] {new[] {(1, 10)}, new[] {(4, 10)}, new[] {(1, 10)}, new[] {(2, 10)}}},
            {upRightCorner, new[] {new[] {(4, 10)}, new[] {(1, 10)}, new[] {(1, 10)}, new[] {(3, 10)}}},
            {downLeftCorner, new[] {new[] {(1, 10)}, new[] {(5, 10)}, new[] {(2, 10)}, new[] {(1, 10)}}},
            {downRightCorner, new[] {new[] {(5, 10)}, new[] {(1, 10)}, new[] {(3, 10)}, new[] {(1, 10)}}}
        };

        StartCoroutine(test(masterTiles, rules));
    }

    private IEnumerator test(TileBase[][] masterTiles, Dictionary<TileBase[], (int, int)[][]> rules)
    {
        EntropyField entropyField = new EntropyField(-20, -10, 20, 10, masterTiles, rules);

        int temp = 0;
        floorMap.SetTile(new Vector3Int(0, 0), entropyField.entropy[new Vector3(0, 0)].Collapse());
        entropyField.UpdateEntropy(new Vector3(0, 0), 2);
        while (!entropyField.IsDone())
        {
            yield return new WaitForSeconds(1f/600);
            Vector3 min = entropyField.MinEntropy();
            Vector3Int location = new Vector3Int(Mathf.FloorToInt(min.x), Mathf.FloorToInt(min.y));
            floorMap.SetTile(location, entropyField.entropy[location].Collapse());
            entropyField.UpdateEntropy(location, 1);
            temp++;
        }
    }
    
}

public class Outcome
{
    public int weight;
    public TileBase[] tiles;

    public Outcome(int weight, TileBase[] tiles)
    {
        this.weight = weight;
        this.tiles = tiles;
    }
}

public class Entropy
{
    public List<Outcome> outcomes = new();
    private float totalWeight;
    public bool collapsed;
    private Dictionary<TileBase[], (int, int)[][]> rules;
    private TileBase[][] masterTiles;
    private TileBase[] tiles;
    private TileBase tile;

    public Entropy(TileBase[][] masterTiles, Dictionary<TileBase[], (int, int)[][]> rules)
    {
        this.masterTiles = masterTiles;
        this.rules = rules;
        
        foreach(TileBase[] tiles in masterTiles)
        {
            outcomes.Add(new Outcome(5, tiles));
        }
    }

    public float GetEntropy()
    {
        if (collapsed)
            return 0f;
        
        GetTotalWeight();
        
        float total = 0;
        for (int i = 0; i < outcomes.Count; i++)
        {
            float probability = outcomes[i].weight / totalWeight;
            total -= (float)(probability * Math.Log(probability) / Math.Log(2));
        }
        return total;
    }

    public TileBase Collapse()
    {
        if (outcomes.Count == 0)
        {
            outcomes = new List<Outcome>{new Outcome(5, masterTiles[0])};
        }

        int index = 0;
        GetTotalWeight();
        int observation = Random.Range(1, (int)totalWeight);

        for (int i = 0; i < outcomes.Count; i++)
        {
            index = i;
            observation -= outcomes[i].weight;
            if (observation < 0) break;
        }

        collapsed = true;
        tiles = outcomes[index].tiles;
        outcomes = new List<Outcome>{new Outcome(5, tiles)};
        tile = tiles[Random.Range(0, tiles.Length)];
        return tile;
    }

    public void Uncollapse()
    {
        collapsed = false;
        outcomes = new List<Outcome>();
        foreach(TileBase[] tilesTemp in masterTiles) outcomes.Add(new Outcome(5, tilesTemp));
        tiles = null;
        tile = null;
    }

    private void GetTotalWeight()
    {
        totalWeight = 0;
        for (int i = 0; i < outcomes.Count; i++) totalWeight += outcomes[i].weight;
    }
}

public class EntropyField
{
    public Dictionary<Vector3, Entropy> entropy = new();
    private TileBase[][] masterTiles;
    private Dictionary<TileBase[], (int, int)[][]> rules;
    
    public EntropyField(int startX, int startY, int endX, int  endY, TileBase[][] masterTiles, Dictionary<TileBase[], (int, int)[][]> rules)
    {
        this.masterTiles = masterTiles;
        this.rules = rules;

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                entropy.Add(new Vector3(x, y), new Entropy(masterTiles, rules));
            }
        }
    }

    private void UpdateEntropy(Vector3 location)
    {
        if (!entropy[location].collapsed)
        {
            var outcomes = entropy[location].outcomes;
            outcomes = new List<Outcome>();
            foreach (TileBase[] tiles in masterTiles) outcomes.Add(new Outcome(5, tiles));
            
            //what can be to the right of the left square
            int[] temp = { -1, -1, -1, -1, -1, -1 };
            int[] weights = { 0, 0, 0, 0, 0, 0 };
            int removed = 0;

            if (entropy.Keys.Contains(new Vector3(location.x - 1, location.y)))
            {
                foreach (Outcome leftOutcome in entropy[new Vector3(location.x - 1, location.y)].outcomes)
                {
                    foreach ((int, int) i in rules[leftOutcome.tiles][1])
                    {
                        temp[i.Item1] = i.Item1;
                        weights[i.Item1] += i.Item2;
                    }
                }

                for(int i = 0; i < outcomes.Count - removed; i++)
                    if (!temp.Contains(rules[outcomes[i].tiles][0][0].Item1))
                    {
                        outcomes.Remove(outcomes[i--]);
                        removed++;
                    }
                    else
                    {
                        outcomes[i].weight += weights[rules[entropy[location].outcomes[i].tiles][1][0].Item1];
                    }
            }

            //what can be to the left of the right square
            temp = new [] { -1, -1, -1, -1, -1, -1 };
            weights = new [] { 0, 0, 0, 0, 0, 0 };
            removed = 0;
            
            if (entropy.Keys.Contains(new Vector3(location.x + 1, location.y)))
            {
                foreach (Outcome rightOutcome in entropy[new Vector3(location.x + 1, location.y)].outcomes)
                    foreach ((int, int) i in rules[rightOutcome.tiles][0])
                    {
                        temp[i.Item1] = i.Item1;
                        weights[i.Item1] += i.Item2;
                    }

                for(int i = 0; i < outcomes.Count - removed; i++)
                    if (!temp.Contains(rules[outcomes[i].tiles][1][0].Item1))
                    {
                        outcomes.Remove(outcomes[i--]);
                        removed++;
                    }
                    else
                    {
                        outcomes[i].weight += weights[rules[entropy[location].outcomes[i].tiles][1][0].Item1];
                    }
            }

            //what can be to below the above square
            temp = new [] { -1, -1, -1, -1, -1, -1 };
            weights = new [] { 0, 0, 0, 0, 0, 0 };
            removed = 0;

            if (entropy.Keys.Contains(new Vector3(location.x, location.y + 1)))
            {
                foreach (Outcome upOutcome in entropy[new Vector3(location.x, location.y + 1)].outcomes)
                    foreach ((int, int) i in rules[upOutcome.tiles][3])
                    {
                        temp[i.Item1] = i.Item1;
                        weights[i.Item1] += i.Item2;
                    }

                for(int i = 0; i < outcomes.Count - removed; i++)
                    if (!temp.Contains(rules[outcomes[i].tiles][2][0].Item1))
                    {
                        outcomes.Remove(outcomes[i--]);
                        removed++;
                    }
                    else
                    {
                        outcomes[i].weight += weights[rules[entropy[location].outcomes[i].tiles][1][0].Item1];
                    }
            }

            //what can be to above the below square
            temp = new [] { -1, -1, -1, -1, -1, -1 };
            weights = new [] { 0, 0, 0, 0, 0, 0 };
            removed = 0;

            if (entropy.Keys.Contains(new Vector3(location.x, location.y - 1)))
            {
                foreach (Outcome leftOutcome in entropy[new Vector3(location.x, location.y - 1)].outcomes)
                    foreach ((int, int) i in rules[leftOutcome.tiles][2])
                    {
                        temp[i.Item1] = i.Item1;
                        weights[i.Item1] += i.Item2;
                    }

                for(int i = 0; i < outcomes.Count - removed; i++)
                    if (!temp.Contains(rules[outcomes[i].tiles][3][0].Item1))
                    {
                        outcomes.Remove(outcomes[i--]);
                        removed++;
                    }
                    else
                    {
                        outcomes[i].weight += weights[rules[entropy[location].outcomes[i].tiles][1][0].Item1];
                    }
            }

            if (outcomes.Count == 0)
            {
                outcomes = new List<Outcome>();
                outcomes.Add(new Outcome(5, masterTiles[1]));
            }

        }
    }

    public void UpdateEntropy(Vector3 location, int range)
    {
        var locations = new List<Vector3>();
        for (float x = 0; x <= range; x++)
        {
            for (float y = 0; y <= range; y++)
            {
                Vector3 temp = new Vector3(location.x + x, location.y + y);
                if(!locations.Contains(temp) && entropy.Keys.Contains(temp))
                    locations.Add(temp);
                temp = new Vector3(location.x - x, location.y + y);
                if(!locations.Contains(temp) && entropy.Keys.Contains(temp))
                    locations.Add(temp);
                temp = new Vector3(location.x + x, location.y - y);
                if(!locations.Contains(temp) && entropy.Keys.Contains(temp))
                    locations.Add(temp);
                temp = new Vector3(location.x - x, location.y - y);
                if(!locations.Contains(temp) && entropy.Keys.Contains(temp))
                    locations.Add(temp);
            }
        }
        
        foreach(Vector3 loc in locations)
            UpdateEntropy(loc);
    }

    public Vector3 MinEntropy()
    {
        float minEntropy = 1.1f;
        foreach (Vector3 location in entropy.Keys)
        {
            if (!entropy[location].collapsed)
            {
                var temp = entropy[location].GetEntropy();
                if (temp <= 0.001f)
                {
                    if(entropy.Keys.Contains(new Vector3(location.x - 1, location.y)))
                        entropy[new Vector3(location.x - 1, location.y)].Uncollapse();
                    if(entropy.Keys.Contains(new Vector3(location.x + 1, location.y)))
                        entropy[new Vector3(location.x + 1, location.y)].Uncollapse();
                    if(entropy.Keys.Contains(new Vector3(location.x, location.y - 1)))
                        entropy[new Vector3(location.x, location.y - 1)].Uncollapse();
                    if(entropy.Keys.Contains(new Vector3(location.x, location.y + 1)))
                        entropy[new Vector3(location.x, location.y + 1)].Uncollapse();
                    
                    UpdateEntropy(location, 1);
                    
                    temp = entropy[location].GetEntropy();
                    if (temp > 0) minEntropy = Math.Min(temp, minEntropy);
                }

                else minEntropy = Math.Min(temp, minEntropy);
            }
        }

        List<Vector3> index = new List<Vector3>();
        
        foreach (Vector3 location in entropy.Keys)
        {
            if ($"{entropy[location].GetEntropy():0.000}" == $"{minEntropy:0.000}" && !entropy[location].collapsed)
            {
                index.Add(location);
            }
        }

        if (index.Count == 0) index = entropy.Keys.ToList();

        return index[Random.Range(0, index.Count)];
    }

    public bool IsDone()
    {
        foreach (Vector3 location in entropy.Keys)
            if (!entropy[location].collapsed)
                return false;

        return true;
    }
}