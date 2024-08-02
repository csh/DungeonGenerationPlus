using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;

namespace DunGenPlus.Components.Scrap {
  public class RandomGuaranteedScrapSpawn : MonoBehaviour {


    public float spawnChance = 1f;
    public int minimumScrapValue = 0;

    public static Dictionary<int, IEnumerable<Item>> scrapItemCache;

    public static void ResetCache(){
      scrapItemCache = new Dictionary<int, IEnumerable<Item>>();
    }

    public static IEnumerable<Item> GetCachedItemList(List<SpawnableItemWithRarity> allMoonItems, int scrapValue) {
      if (!scrapItemCache.TryGetValue(scrapValue, out var list)){
        list = allMoonItems.Select(i => i.spawnableItem).Where(i => i.minValue >= scrapValue).ToArray();
        scrapItemCache.Add(scrapValue, list);
      }
      return list;
    }

    public (NetworkObject itemReference, int scrapValue) CreateItem(RoundManager roundManager, List<SpawnableItemWithRarity> allMoonItems){
      var anomalyRandom = roundManager.AnomalyRandom;
      if (anomalyRandom.NextDouble() >= spawnChance) return (null, 0);

      var itemList = GetCachedItemList(allMoonItems, minimumScrapValue);
      var itemListCount = itemList.Count();
      if (itemListCount == 0) return (null, 0);

      var randomItem = itemList.ElementAt(anomalyRandom.Next(itemListCount));
      var randomValue = (int)(anomalyRandom.Next(randomItem.minValue, randomItem.maxValue) * roundManager.scrapValueMultiplier);

      var gameObject = Instantiate(randomItem.spawnPrefab, transform.position, Quaternion.identity, roundManager.spawnedScrapContainer);
      var itemComp = gameObject.GetComponent<GrabbableObject>();
      
      gameObject.transform.rotation = Quaternion.Euler(randomItem.restingRotation);
      itemComp.fallTime = 0f;
      itemComp.scrapValue = randomValue;

      var networkComp = gameObject.GetComponent<NetworkObject>();
      networkComp.Spawn(false);
      return (networkComp, randomValue);
    }

  }
}
