using System.Collections;
using UnityEngine;

namespace Lynx
{

    [System.Serializable]
    public class brush
    {
        public Transform trs;
        public Transform offPos;
        public Transform onPos;
    }

    public class PaletteManager : MonoBehaviour
    {
        #region SCRIPT PARAMETERS
        [SerializeField] private PaintManager manager;

        [SerializeField] private brush[] brushes;

        [SerializeField] private MeshRenderer paintSpots;

        [SerializeField] private float animTime = 0.33f;
        [SerializeField] private LynxMath.easingType easing;

        [SerializeField] private Color[] paintColors;
        #endregion

        private void Start()
        {
            //set colors on palette to corespond input parameters
            for (int i = 0; i < paintSpots.materials.Length; i++)
            {
                paintSpots.materials[i].SetColor("_Color", paintColors[i]);
            }
            SetBrush(0);
            SetColor(0);
            manager.paintCol = paintColors[0];
            manager.paintAlpha = 0;
        }

        /// <summary>
        /// set alpha index on stroke generator
        /// set index on palette materials
        /// start animation on selected brush obj
        /// </summary>
        /// <param name="idx">index of the selected brush</param>
        public void SetBrush(int idx)
        {
            StartCoroutine(ActivateBrush(idx));
            for (int i = 0; i < paintSpots.materials.Length; i++)
            {
                paintSpots.materials[i].SetFloat("_idx", idx);
            }
            manager.paintAlpha = idx;
        }

        /// <summary>
        /// activate the corect alpha index to paint manager
        /// change brush objects color
        /// </summary>
        /// <param name="idx">index of the selected color</param>
        public void SetColor(int idx)
        {
            for (int i = 0; i < brushes.Length; i++)
            {
                brushes[i].trs.gameObject.GetComponent<MeshRenderer>().materials[1].SetColor("_Color", paintColors[idx]);
            }
            manager.paintCol = paintColors[idx];
        }

        /// <summary>
        /// Animate all brush to push forward the selected one and backward the others
        /// </summary>
        /// <param name="idx">index of selected brush</param>
        /// <returns></returns>
        IEnumerator ActivateBrush(int idx)
        {
            // save pos at start of coroutine
            Vector3[] pos = new Vector3[4];
            for (int i = 0; i < brushes.Length; i++)
            {
                pos[i] = brushes[i].trs.position;
            }

            for (float t = 0; t < 1; t += Time.deltaTime / animTime)
            {
                float l = LynxMath.Ease(t, easing);
                for (int i = 0; i < brushes.Length; i++)
                {
                    if (i == idx)
                    {
                        brushes[i].trs.position = Vector3.Lerp(pos[i], brushes[i].onPos.position, l);
                    }
                    else
                    {
                        brushes[i].trs.position = Vector3.Lerp(pos[i], brushes[i].offPos.position, l);
                    }
                }
                yield return new WaitForEndOfFrame();
            }
            for (int i = 0; i < brushes.Length; i++)
            {
                if (i == idx)
                {
                    brushes[i].trs.position = brushes[i].onPos.position;
                }
                else
                {
                    brushes[i].trs.position = brushes[i].offPos.position;
                }
            }
        }
    }
}
