using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGenPlus.Collections {
  
  [System.Serializable]
  public  class DunGenExtenderEvents {

    public ExtenderEvent<DunGenExtenderProperties> OnModifyDunGenExtenderProperties = new ExtenderEvent<DunGenExtenderProperties>();

  }
}
