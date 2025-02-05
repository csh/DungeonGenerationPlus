using DunGen;
using DunGenPlus.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Collections {
  internal class TileExtenderProxy {
    public TileProxy TileProxy { get; internal set; }
    public TileExtender PrefabTileExtender { get; internal set; }
    public List<DoorwayProxy> Entrances { get; internal set; }
    public List<DoorwayProxy> Exits { get; internal set; }

    public TileExtenderProxy(TileExtenderProxy existingTileExtenderProxy) {
      TileProxy = existingTileExtenderProxy.TileProxy;
      PrefabTileExtender = existingTileExtenderProxy.PrefabTileExtender;
      Entrances = new List<DoorwayProxy>();
      Exits = new List<DoorwayProxy>();

      foreach(var existingDoorway in TileProxy.doorways) {
        if (existingTileExtenderProxy.Entrances.Contains(existingDoorway)) Entrances.Add(existingDoorway);
        if (existingTileExtenderProxy.Exits.Contains(existingDoorway)) Exits.Add(existingDoorway);
      }
    }

    public TileExtenderProxy(TileProxy tileProxy) {
      TileProxy = tileProxy;
      PrefabTileExtender = tileProxy.Prefab.GetComponent<TileExtender>();
      Entrances = new List<DoorwayProxy>();
      Exits = new List<DoorwayProxy>();

      if (PrefabTileExtender == null) {
        if (tileProxy.Entrance != null) Entrances.Add(tileProxy.Entrance);
        if (tileProxy.Exit != null) Exits.Add(tileProxy.Exit);
        return;
      }

      foreach(var proxyDoorway in tileProxy.doorways) {
        if (PrefabTileExtender.entrances.Contains(proxyDoorway.DoorwayComponent)) Entrances.Add(proxyDoorway);
        if (PrefabTileExtender.exits.Contains(proxyDoorway.DoorwayComponent)) Exits.Add(proxyDoorway);
      }
    }
  }
}
