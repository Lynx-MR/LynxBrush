//   ==============================================================================
//   | LynxInterfaces (2023)                                                      |
//   |======================================                                      |
//   | LynxUI Editor Script                                                       |
//   | Script that displays interface creation shortcuts.                         |
//   ==============================================================================

#if LYNX_XRI
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace Lynx.UI
{
    public class LynxUIEditor
    {
        private const string STR_LYNX_CANVAS = "LynxCanvas.prefab";
        private const string STR_SIMPLE_BUTTON = "LynxSimpleButton_Square_Image.prefab";
        private const string STR_TOGGLE_BUTTON = "LynxToggleButton_Square_Image.prefab";
        private const string STR_TIMER_BUTTON = "LynxTimerButton_Square_Image.prefab";
        private const string STR_SWITCH_BUTTON = "LynxSwitchButton.prefab";
        private const string STR_LABEL = "LynxLabel.prefab";
        private const string STR_SLIDER = "LynxSlider.prefab";

#if LYNX_XRI

        /// <summary>
        /// Add a Toggle Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Theme Manager", false, 210)]
        public static void AddThemeManager()
        {
            // 
            if (LynxBuildSettings.FindObjectsOfTypeAll<LynxThemeManager>().Count <= 0)
                LynxThemeManager.ResetInstance();

            if (!LynxThemeManager.Instance)
                LynxThemeManagerEditor.InstantiateThemeManager();
            else
                Debug.LogWarning("Theme Manager already exist in current scene.");
        }


        /// <summary>
        /// Add a Canvas, setup for the XRI.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Canvas", false, 211)]
        public static void AddHandtrackingCanvas()
        {
            if (GameObject.FindObjectOfType<EventSystem>() == null)
                InstantiateEventSystem();

            InstantiateCanvas();
        }

        /// <summary>
        /// Call this function to instiante an Event System.
        /// </summary>
        /// <returns>New Event System GameObject.</returns>
        private static GameObject InstantiateEventSystem()
        {
            // Create a new GameObject to hold the EventSystem component
            GameObject eventSystemObject = new GameObject("EventSystem");

            // Add the EventSystem component to the GameObject
            eventSystemObject.AddComponent<EventSystem>();

            // Add the StandaloneInputModule component to the GameObject
            eventSystemObject.AddComponent<StandaloneInputModule>();

            return eventSystemObject;
        }

        /// <summary>
        /// Call this function to instantiate a Lynx canvas.
        /// </summary>
        /// <returns>New Canvas GameObject.</returns>
        private static GameObject InstantiateCanvas()
        {
            Transform tParent = Selection.activeTransform;

            string str_gameObject = Directory.GetFiles(Application.dataPath, STR_LYNX_CANVAS, SearchOption.AllDirectories)[0].Replace(Application.dataPath, "Assets/");
            GameObject gameObject = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<Object>(str_gameObject), tParent) as GameObject;

            gameObject.transform.SetAsLastSibling();

            if (tParent)
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(tParent, true);

            Undo.RegisterCreatedObjectUndo(gameObject, "Instantiated UI");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            // Add TrackedDeviceGraphicRaycaster
            gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            //set envent camera to canvas
            gameObject.GetComponent<Canvas>().worldCamera = Camera.main;

            Undo.RegisterCreatedObjectUndo(gameObject, "Instantiated Canvas");

            return gameObject;
        }

        /// <summary>
        /// Call this function to instantiate a default XR canvas.
        /// </summary>
        /// <returns>New Canvas GameObject.</returns>
        private static GameObject InstantiateXRCanvas()
        {
            // Create empty GameObject
            GameObject canvasObject = new GameObject("Handtracking Canvas");
            canvasObject.transform.position = new Vector3(0f, Camera.main.transform.position.y, 0.4f);
            canvasObject.transform.rotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one;

            // Add Canvas and assign values
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;

            // Add CanvasScaler
            canvasObject.AddComponent<CanvasScaler>();

            // Add GraphicRaycaster
            canvasObject.AddComponent<GraphicRaycaster>();

            // Add TrackedDeviceGraphicRaycaster
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            Undo.RegisterCreatedObjectUndo(canvasObject, "Instantiated Canvas");

            return canvasObject;
        }

        



        /// <summary>
        /// Add a Toggle Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Label", false, 212)]
        public static void AddLabel()
        {
            InstantiatePrefab(STR_LABEL);
        }


        /// <summary>
        /// Add a Simple Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Simple Button", false, 213)]
        public static void AddSimpleButton()
        {
            InstantiatePrefab(STR_SIMPLE_BUTTON);
        }


        /// <summary>
        /// Add a Toggle Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Toggle Button", false, 214)]
        public static void AddToggleButton()
        {
            InstantiatePrefab(STR_TOGGLE_BUTTON);
        }

        /// <summary>
        /// Add a Timer Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Timer Button", false, 215)]
        public static void AddTimerButton()
        {
            InstantiatePrefab(STR_TIMER_BUTTON);
        }

        /// <summary>
        /// Add a Switch Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Switch Button", false, 216)]
        public static void AddSwitchButton()
        {
            InstantiatePrefab(STR_SWITCH_BUTTON);
        }

        /// <summary>
        /// Add a Sider Button in the scene.
        /// </summary>
        [MenuItem("GameObject/Lynx/UI/Slider Button", false, 217)]
        public static void AddSliderButton()
        {
            InstantiatePrefab(STR_SLIDER);
        }

        /// <summary>
        /// Call this function to instiantie a UI prefab.
        /// </summary>
        /// <param name="prefab">The UI prefab to instantiate.</param>
        private static void InstantiatePrefab(string prefab)
        {
            string str_gameObject = Directory.GetFiles(Application.dataPath, prefab, SearchOption.AllDirectories)[0].Replace(Application.dataPath, "Assets/");
            GameObject gameObject = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<Object>(str_gameObject), Selection.activeTransform ? Selection.activeTransform : InstantiateXRCanvas().transform) as GameObject;
            gameObject.transform.localPosition = Vector3.zero;

            gameObject.transform.SetAsLastSibling();

            if (Selection.activeTransform)
                UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(Selection.activeTransform, true);

            Undo.RegisterCreatedObjectUndo(gameObject, "Instantiated UI");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        }
#endif
    }
}
