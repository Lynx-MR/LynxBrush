using System.Collections;
using UnityEngine;
using System.IO;

namespace Lynx
{
    /// <summary>
    /// Lynx Utilities to create screenshots.
    /// </summary>
    public class ScreenshotAndVideoUtilities : MonoBehaviour
    {
        //INSPECTOR
        [Header("The camera object that takes screenshots")]
        public GameObject m_cameraGameObjectForScreenShot;

        //PRIVATE
        private static int m_scWidth = 1024;
        private static int m_scHeight = 1024;

        private static string m_keywordForVideoThumbnailName = "_lynxthumb";

#if !UNITY_EDITOR && UNITY_ANDROID
    // Path the lynx shared folder where screenshots and video are stored. 
    private static string mLYNX_GALLERY_PATH = "/sdcard/DCIM/Lynx/ScreenAndVideoShots";
#endif

        public static string GetVideoThumbnailKeyword()
        {
            return m_keywordForVideoThumbnailName;
        }

        public void TakeShot()
        {
            GetGalleryPath();
            StartCoroutine(TakeScreenShotCoroutine(m_scWidth,
                                                   m_scHeight));
        }

        public static string GetGalleryPath()
        {
            string galleryFullPath;

#if !UNITY_EDITOR && UNITY_ANDROID
        galleryFullPath = mLYNX_GALLERY_PATH; 
        //Debug.Log("galleryFullPath on Android : " + galleryFullPath);// typically /storage/emulated/0/Android/data/com.a.LynxAppStartTest/files/
#else
            //Application.dataPath; // typically on Windows the Assets folder
            galleryFullPath = Application.dataPath + "/ScreenAndVideoShots";
#endif

            if (!System.IO.Directory.Exists(galleryFullPath))
            {
                Debug.Log("***** ScreenshotAndVideoUtilities::GetGalleryPath : Create Lynx folder because it doesn't exist");
                System.IO.Directory.CreateDirectory(galleryFullPath);
            }

            return galleryFullPath;
        }


        IEnumerator TakeScreenShotCoroutine(int resWidth, int resHeight)
        {
            yield return new WaitForEndOfFrame();
            TakeScreenShot(resWidth, resHeight);
        }



        /// <summary>
        /// Create a screenshot with given dimensions and save it with the day time.
        /// </summary>
        /// <param name="width">Width for the screenshot.</param>
        /// <param name="height">Height for the screenshot.</param>
        /// <returns>Saved file name.</returns>
        public void TakeScreenShot(int resWidth, int resHeight)
        {
            RenderTexture renderTexture = new RenderTexture(resWidth, resHeight, 24);
            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                m_cameraGameObjectForScreenShot.SetActive(true);

                if (m_cameraGameObjectForScreenShot != null)
                {
                    //Debug.Log("goCamera find = " + cameraGameObjectForScreenShot.name);
                    Camera cameraForScreenshot = m_cameraGameObjectForScreenShot.GetComponent<Camera>();

                    if (cameraForScreenshot != null)
                    {
                        mainCamera = cameraForScreenshot;
                    }
                    else
                    {
                        Debug.LogWarning("No camera object found on Eye Left object");
                        return;
                    }
                }
            }

            mainCamera.targetTexture = renderTexture;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            mainCamera.Render();
            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            mainCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
            byte[] bytes = screenShot.EncodeToJPG(60);//.EncodeToPNG();

            string filename = ComputeScreenShotPath();

            System.IO.File.WriteAllBytes(filename, bytes);

            Destroy(screenShot);
            Destroy(renderTexture);

            // cedric : change 05 septembre 2022 for Open XR version
            // don't desactivate the camera. it's no more the mono camera like on SVR, it's now on Open XR version the main running camera. 
            //cameraGameObjectForScreenShot.SetActive(false);

            Debug.Log("******** ScreenshotAndVideoUtilities::New screenshot taken with path : " + filename);
        }

        /// <summary>
        /// Create a screenshot file name based "timestamp".
        /// </summary>
        /// <returns>Saved file name.</returns>
        public static string ComputeScreenShotPath()
        {
            /*
            string filename = string.Format("{0}/screen_{1}.png",
                                        GetGalleryPath(),
                                        System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            */

            string filename = string.Format("{0}/screen_{1}.jpg",
                                        GetGalleryPath(),
                                        System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            return filename;
        }

        /// <summary>
        /// Create a video file name based on "timestamp".
        /// </summary>
        /// <returns>Saved file name.</returns>
        public static string ComputeVideoShotPath()
        {
            string filename = string.Format("{0}/video_{1}.mp4",
                                        GetGalleryPath(),
                                        System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

            return filename;
        }


        public static bool ComputeVideoThumbnailPath(string videoShotPath, out string videoThumnailPath)
        {
            bool ret = false;
            videoThumnailPath = "";
            string strThumbnailName = "NotFound";

            int index = videoShotPath.IndexOf("video_2");

            if (index > 0)
            {
                strThumbnailName = videoShotPath.Substring(index, videoShotPath.Length - index - 4); // - 4 is to remove .mp4
                strThumbnailName = strThumbnailName + m_keywordForVideoThumbnailName + ".jpg";
                strThumbnailName = GetGalleryPath() + "/" + strThumbnailName;
                videoThumnailPath = strThumbnailName;
                ret = true;
            }

            return ret;
        }

        public static void DeleteVideoThumbnail(string videoShotPath)
        {
            string videoThumbnailPath;

            bool ret = ComputeVideoThumbnailPath(videoShotPath, out videoThumbnailPath);

            if (ret && videoThumbnailPath.Length != 0 && System.IO.File.Exists(videoThumbnailPath))
                File.Delete(videoThumbnailPath);
        }

        public static string GetThumbnailTitleName(string filePath)
        {
            string result;

            // screen_2021-11-02_18-03-04.jpg -> 
            string temp = Path.GetFileName(filePath);
            string[] stringArray = new string[5];
            stringArray = temp.Split('-');

            int indexScreen = stringArray[0].IndexOf("screen_20");
            int indexVideo = stringArray[0].IndexOf("video_20");

            if (indexScreen < 0 && indexVideo < 0) // the name is not correctly formatted, it's not a lynx screenshots or video -> so display only title name of the file.  
            {
                Debug.LogWarning("MEDIA name is not correctly formatted, it is not a lynx screenshots or video, so display only title name of the file");
                result = Path.GetFileNameWithoutExtension(filePath);
            }
            else
            {
                int index = stringArray[0].IndexOf("_");

                string year = stringArray[0].Substring(index + 1);
                string month = stringArray[1];
                string day = stringArray[2].Substring(0, 2);
                string hour = stringArray[2].Substring(3, 2);
                string min = stringArray[3];

                result = month + "." + day + "." + year + "  " + hour + ":" + min;
            }

            return result;
        }


    }
}