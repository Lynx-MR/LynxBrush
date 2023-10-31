using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lynx
{
    public class StrokeGenerator : MonoBehaviour
    {
        [SerializeField] private GameObject camOffset;
        [SerializeField] private Material strokeMat; 
        [SerializeField] private PaintManager paintManager;

        public bool isPainting = false;

        #region Stroke generation parameters
        public float size = 0.1f;
        public Quaternion normal;
        public Vector3 pinchPos;
        public float drawDensity = 0.05f;
        public Color color = Color.white;
        public Vector2 atlasSize;

        private int m_idx = 0;
        public int atlasIndex
        {
            get
            {
                return m_idx;
            }
            set
            {
                m_idx = value;
                UVFromIndex(value);
            }
        }
        #endregion

        #region Mesh Generation Data
        private int strokeNbr = 0;
        private GameObject str;
        private MeshFilter currentStroke;
        private Mesh mesh;
        private List<Vector3> vert;
        private List<Vector2> uv;
        private List<int> tris;
        private List<Color> col;

        private Vector2[] uvCoord;
        #endregion


        private List<GameObject> strokes = new List<GameObject>();

        private void Start()
        {
            UVFromIndex(m_idx);
        }

        /// <summary>
        /// create a new stroke object and init needed data for generation
        /// start mesh generation coroutine
        /// </summary>
        public void NewStroke()
        {
            str = new GameObject();
            str.transform.position = camOffset.transform.position;
            str.transform.rotation = camOffset.transform.rotation;
            str.name = "Stroke_" + strokeNbr.ToString();
            currentStroke = str.AddComponent<MeshFilter>();
            MeshRenderer mshR = str.AddComponent<MeshRenderer>();
            mshR.name = "Stroke_" + strokeNbr.ToString();
            mshR.material = strokeMat;


            ++strokeNbr;
            isPainting = true;
            StartCoroutine(GenerateMesh());

        }

        /// <summary>
        /// Generate a new mesh while right hand is pinching
        /// add new stroke to strokes list at the end 
        /// </summary>
        /// <returns></returns>
        IEnumerator GenerateMesh()
        {

            Vector3 SC = pinchPos;
            mesh = new Mesh();
            vert = new List<Vector3>();
            uv = new List<Vector2>();
            tris = new List<int>();
            col = new List<Color>();

            while (isPainting)
            {
                //create new plane to fill distance betwen last pinch and new one
                while (Vector3.Distance(SC, pinchPos) > drawDensity)
                {
                    MakeQuad(SC, normal, size, out Vector3 vec1, out Vector3 vec2, out Vector3 vec3, out Vector3 vec4);
                    int tmpvertcount = vert.Count;
                    vert.AddRange(new List<Vector3> { vec1, vec2, vec3, vec4 });

                    uv.AddRange(new List<Vector2> { uvCoord[0], uvCoord[1], uvCoord[2], uvCoord[3] });

                    col.AddRange(new List<Color> { color, color, color, color });

                    tris.AddRange(new List<int> { 0 + tmpvertcount, 2 + tmpvertcount, 1 + tmpvertcount, 0 + tmpvertcount, 3 + tmpvertcount, 2 + tmpvertcount });

                    SC += (pinchPos - SC).normalized * (drawDensity);
                }
                //apply new data to mesh
                mesh.vertices = vert.ToArray();
                mesh.uv = uv.ToArray();
                mesh.triangles = tris.ToArray();
                mesh.colors = col.ToArray();
                currentStroke.mesh = mesh;

                yield return new WaitForEndOfFrame();
            }

            strokes.Add(str);
        }

        #region PRIVATE METHODES
        /// <summary>
        /// generate new vertex position to make a plane
        /// </summary>
        /// <param name="pos">Center of the plane</param>
        /// <param name="nrm">Normal of the plane</param>
        /// <param name="radius">Half size of the plane</param>
        /// <param name="vec1">position of first vertice</param>
        /// <param name="vec2">position of second vertice</param>
        /// <param name="vec3">position of third vertice</param>
        /// <param name="vec4">position of fourth vertice</param>
        private void MakeQuad(in Vector3 pos, in Quaternion nrm, in float radius, out Vector3 vec1, out Vector3 vec2, out Vector3 vec3, out Vector3 vec4)
        {
            float angle = Random.Range(0, Mathf.PI * 2);
            float s = Mathf.Sin(angle) * radius;
            float c = Mathf.Cos(angle) * radius;

            vec1 = (nrm * new Vector3(-c + s, -c - s, 0.0f)) + pos; // BL
            vec2 = (nrm * new Vector3(c + s, -c + s, 0.0f)) + pos; // BR
            vec3 = (nrm * new Vector3(c - s, c + s, 0.0f)) + pos; // TR
            vec4 = (nrm * new Vector3(-c - s, c - s, 0.0f)) + pos; // TL
        }

        /// <summary>
        /// Generate array of UV coordinate for new generated mesh depending on atlas index
        /// </summary>
        /// <param name="idx">index of alpha in atlas</param>
        private void UVFromIndex(int idx)
        {
            float xSplit = 1f / atlasSize.x;
            float ySplit = 1f / atlasSize.y;

            float yOffset = Mathf.Floor(idx / atlasSize.x) * ySplit;
            float xOffset = Mathf.Floor(idx % atlasSize.y) * xSplit;



            //{ Vector2.zero, Vector2.right, Vector2.one, Vector2.up}
            uvCoord = new Vector2[4] {  new Vector2(xOffset,yOffset),
                                    new Vector2(xOffset,yOffset) + new Vector2(xSplit, 0),
                                    new Vector2(xOffset,yOffset) + new Vector2(xSplit, ySplit),
                                    new Vector2(xOffset,yOffset) + new Vector2(0, ySplit)
                                 };
        }
        #endregion

        /// <summary>
        /// Delete last stroke in strokes list
        /// </summary>
        public void UndoStroke()
        {
            Destroy(strokes[strokes.Count - 1]);
            strokes.RemoveAt(strokes.Count - 1);
        }

    }
}
