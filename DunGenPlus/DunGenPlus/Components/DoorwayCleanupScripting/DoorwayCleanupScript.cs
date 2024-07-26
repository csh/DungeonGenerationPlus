using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DunGenPlus.Components.DoorwayCleanupScripting {
  public abstract class DoorwayCleanupScript : MonoBehaviour {

    public abstract void Cleanup(DoorwayCleanup parent);

  }
}
