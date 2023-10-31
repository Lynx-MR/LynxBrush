//Used to start an event on trigger entry
using UnityEngine;
using UnityEngine.Events;

namespace Lynx
{
    [System.Serializable]
    public class EnterEvent : UnityEvent<int>
    {
    }

    public class TriggerEvent : MonoBehaviour
    {
        public EnterEvent OnTriggerEntry;


        public void OnTriggerEnter(Collider other)
        {
            if (OnTriggerEntry != null)
                OnTriggerEntry.Invoke(0);
        }
    }
}

