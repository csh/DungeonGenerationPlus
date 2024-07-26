using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DunGenPlus.Collections {
  public class ExtenderEvent<T> {

    internal event ParameterEvent onParameterEvent;

    public void Invoke(T param) {
      onParameterEvent?.Invoke(param);
    }

    public void AddListener(ParameterEvent listener) {
      onParameterEvent += listener;
    }

    public void RemoveListener(ParameterEvent listener) {
      onParameterEvent -= listener;
    }

    public delegate void ParameterEvent(T param);
  }
}
