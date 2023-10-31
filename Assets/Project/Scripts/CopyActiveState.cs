// used to disable palette object when hands are not visible
using UnityEngine;

namespace Lynx
{
    public class CopyActiveState : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer smr;
        [SerializeField] private GameObject[] objToSet;
        void Update()
        {
            for (int i = 0; i < objToSet.Length; i++)
            {
                objToSet[i].SetActive(smr.enabled);
            }
        }
    }
}