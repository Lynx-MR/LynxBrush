/**
 * @file VideoEncoderNatifMng.cs
 * 
 * @author Cédric Morel Francoz
 * 
 * @brief Manage encoder Video : all calls to native plug in are made here. 
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;


namespace Lynx
{
    public class VideoEncoderNatifMng : MonoBehaviour
    {
        // Plug ins imported fonction : 
        [DllImport("AndroidNativeVideoEncoder")]
        private static extern void InitEncoder(string videoFileName, int fps, int width, int height);

        [DllImport("AndroidNativeVideoEncoder")]
        private static extern void EncodeFrame(byte[] data, int frameIndex);

        [DllImport("AndroidNativeVideoEncoder")]
        private static extern void EncodeRGBAFrame(byte[] data, int frameIndex);

        [DllImport("AndroidNativeVideoEncoder")]
        private static extern void EndEncoding();

        [SerializeField]
        private Camera mCameraToCapture = null;

        // Public Properties
        [Header("Max duration in seconds of one video shot")]
        public int m_maxDuration = 300; // 5 minutes. 

        [Header("Frame rate of the video")]
        public int m_frameRate = 10; // number of frames to capture per second


        // important : size of one eye screen for lynx. 
        int mWidth = 1536;
        int mHeight = 1404;

        int mResizedWidth;
        int mResizedHeight;

        int mMaxFrames = 0;

        RenderTexture mRenderTexture = null;
        Texture2D mResizedTexture = null;
        Texture2D mThumbnailTexture = null;

        // Serialize frame part    
        private bool mProcessStarted = false;

        // Timing Data
        private float mCaptureFrameTime;
        private float mLastFrameTime;
        private int mFrameIndex;
        private int mFrameForThumbnailIndex = 15;

        private string mVideoFilePath;
        private string mThumbnailPath;
        private bool bThumbnailRecorded = false;


        byte[] mYuv420spArray;

        // Serialize frame part 
        private bool mThreadIsProcessing;
        private bool mTerminateThreadWhenDone;

        private List<byte[]> mFrameQueue;

        private int mSavingFrameNumber;


        // The Encoder Thread
        private Thread mEncoderThread;



        // just to test perf :
        private System.Diagnostics.Stopwatch m_sw = new System.Diagnostics.Stopwatch();

        void Awake()
        {
            mResizedWidth = mWidth / 2;    // 768  
            mResizedHeight = mHeight / 2;  // 702 

            mMaxFrames = m_maxDuration * m_frameRate;

            mYuv420spArray = new byte[mResizedWidth * mResizedHeight * 3 / 2];
            mFrameQueue = new List<byte[]>();
        }

        void Start()
        {
            if (mCameraToCapture == null) mCameraToCapture = Camera.main;
        }

        void CreateTextures()
        {
            mRenderTexture = new RenderTexture(mResizedWidth, mResizedHeight, 24);// 24 n'est pas RGB24 mais la profondeur. depth.     
            mResizedTexture = new Texture2D(mResizedWidth, mResizedHeight, TextureFormat.RGBA32, false); // ça marche.
            mThumbnailTexture = new Texture2D(mResizedWidth, mResizedHeight, TextureFormat.RGB24, false);
        }

        void DestroyTextures()
        {
            if (mRenderTexture != null)
            {
                Destroy(mRenderTexture);
                mRenderTexture = null;
            }

            if (mResizedTexture != null)
            {
                Destroy(mResizedTexture);
                mResizedTexture = null;
            }

            if (mThumbnailTexture != null)
            {
                Destroy(mThumbnailTexture);
                mThumbnailTexture = null;
            }
        }


        byte[] CaptureFrame(int width, int height)
        {
            // Render frame and divide /2 in one pass :
            mCameraToCapture.targetTexture = mRenderTexture;
            mCameraToCapture.Render();
            RenderTexture.active = mRenderTexture;

            VerticallyFlipRenderTexture(mRenderTexture);

            mResizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0); // it's here that is long !!! 

            mCameraToCapture.targetTexture = null;
            RenderTexture.active = null;

            byte[] bArray = mResizedTexture.GetRawTextureData();

            return bArray;
        }

        void recordThumbnail(int width, int height)
        {
            mCameraToCapture.targetTexture = mRenderTexture;
            mCameraToCapture.Render();
            RenderTexture.active = mRenderTexture;

            // Do not flip the texture here :

            mThumbnailTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            //mThumbnailTexture.Apply(); // cedric is it very useful ? 
            mCameraToCapture.targetTexture = null;
            RenderTexture.active = null;

            bThumbnailRecorded = true;
        }

        private void encodeRGBAFrame(byte[] rgbaBuffer, int indexFrame)
        {
            EncodeRGBAFrame(rgbaBuffer, indexFrame);
        }


        private void encodeYUVFrame(byte[] yuvBuffer, int indexFrame)
        {
            EncodeFrame(yuvBuffer, indexFrame);
        }


        void LateUpdate()
        {
            if (!mProcessStarted) return;

            if (mFrameIndex <= mMaxFrames)
            {
                // Calculate number of video frames to produce from this game frame
                // Generate 'padding' frames if desired framerate is higher than actual framerate
                float thisFrameTime = Time.time;
                int framesToCapture = ((int)(thisFrameTime / mCaptureFrameTime)) - ((int)(mLastFrameTime / mCaptureFrameTime));

                // Capture the frame :
                if (framesToCapture > 0)
                {
                    byte[] bArray = CaptureFrame(mResizedWidth, mResizedHeight);

                    mFrameQueue.Add(bArray);

                    if (mFrameIndex == mFrameForThumbnailIndex)
                    {
                        recordThumbnail(mResizedWidth, mResizedHeight);

                    }

                    mFrameIndex++;

                }

                mLastFrameTime = thisFrameTime;
            }
            else
            {
                Debug.Log("--------------------- End of capture because mFrameIndex > mMaxFrames");
                StopRecord();
            }
        }

        public void RecordVideo()
        {
            // compute video file path : it's in DCIM/Lynx/ScreenAndVideoShots
            string strVideoPath = ScreenshotAndVideoUtilities.ComputeVideoShotPath();

            Debug.Log("RecordVideo with video file Path : " + strVideoPath);

            mVideoFilePath = strVideoPath;
            bThumbnailRecorded = false;

            string videoThumbnailPath;

            ScreenshotAndVideoUtilities.ComputeVideoThumbnailPath(strVideoPath, out videoThumbnailPath);
            mThumbnailPath = videoThumbnailPath;

            CreateTextures();

            mFrameIndex = 0;

            mCaptureFrameTime = 1.0f / (float)m_frameRate;
            mLastFrameTime = Time.time;

            // first call to native plug in : 
            InitEncoder(strVideoPath,
                        m_frameRate,
                        mResizedWidth,
                        mResizedHeight);


            // Kill the encoder thread if running from a previous execution
            if (mEncoderThread != null && (mThreadIsProcessing || mEncoderThread.IsAlive))
            {
                mThreadIsProcessing = false;
                mEncoderThread.Join();
            }

            // Start a new encoder thread
            mThreadIsProcessing = true;
            mEncoderThread = new Thread(EncodeAndSave);
            mEncoderThread.Start();

            // Go !!!
            mProcessStarted = true;

            Debug.Log("Video capture and encoding process START");
        }

        public void StopRecord()
        {
            Debug.Log("Stop Video Record called");

            mProcessStarted = false;
            mTerminateThreadWhenDone = true;

            EndEncoding();

            saveVideoThumbnail();

            DestroyTextures();
        }

        private void saveVideoThumbnail()
        {
            var fileInfo = new System.IO.FileInfo(mVideoFilePath);
            Debug.Log("Video file final length : " + fileInfo.Length);

            if (mThumbnailPath.Length != 0 && bThumbnailRecorded && fileInfo.Length > 0)
            {
                byte[] bytes = mThumbnailTexture.EncodeToJPG(60);

                if (bytes.Length != 0)
                {
                    System.IO.File.WriteAllBytes(mThumbnailPath, bytes);
                    Debug.Log("Video Thumbnail saved ");
                }
                else
                {
                    Debug.LogError("Video Thumbnail NOT saved ");
                }
            }
        }

        private static void VerticallyFlipRenderTexture(RenderTexture target)
        {
            var temp = RenderTexture.GetTemporary(target.descriptor);
            Graphics.Blit(target, temp, new Vector2(1, -1), new Vector2(0, 1));
            Graphics.Blit(temp, target);
            RenderTexture.ReleaseTemporary(temp);
        }

        void EncodeYUV420SP_FromRGBA32BytesArray(byte[] yuv420sp, byte[] rgba, int width, int height) // adjusted by me
        {
            int yIndex = 0;
            int uvIndex = width * height;//mFrameSize; // attention ici

            int pixelIndex = 0;

            //int a;
            int R, G, B, Y, U, V;

            int index = 0;

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {

                    //a = (argb[index] & 0xff000000) >> 24; // a is not used obviously
                    // invert r and b
                    B = rgba[index] & 0xff;
                    G = rgba[index + 1] & 0xff;
                    R = rgba[index + 2] & 0xff;

                    // well known RGB to YUV algorithm
                    Y = ((66 * R + 129 * G + 25 * B + 128) >> 8) + 16;
                    U = ((-38 * R - 74 * G + 112 * B + 128) >> 8) + 128;
                    V = ((112 * R - 94 * G - 18 * B + 128) >> 8) + 128;

                    // NV21 has a plane of Y and interleaved planes of VU each sampled by a factor of 2
                    //    meaning for every 4 Y pixels there are 1 V and 1 U.  Note the sampling is every other
                    //    pixel AND every other scanline.
                    yuv420sp[yIndex++] = (byte)((Y < 0) ? 0 : ((Y > 255) ? 255 : Y));

                    if (j % 2 == 0 && pixelIndex % 2 == 0)
                    {
                        yuv420sp[uvIndex++] = (byte)((V < 0) ? 0 : ((V > 255) ? 255 : V));
                        yuv420sp[uvIndex++] = (byte)((U < 0) ? 0 : ((U > 255) ? 255 : U));
                    }

                    pixelIndex++;
                    index += 4; // rgba
                }
            }
        }

        private void EncodeAndSave()
        {
            while (mThreadIsProcessing)
            {
                if (mFrameQueue.Count > 0)
                {
                    // ici , il pourrai arrivé que le mYuv420spArray se remplisse en décalage par rapport           
                    EncodeYUV420SP_FromRGBA32BytesArray(mYuv420spArray, mFrameQueue[0], mResizedWidth, mResizedHeight);
                    encodeYUVFrame(mYuv420spArray, mFrameIndex);
                    mFrameQueue.RemoveAt(0);

                    GC.Collect();
                }
                else
                {

                    if (mTerminateThreadWhenDone)
                    {
                        break;
                    }

                    Thread.Sleep(1);

                }
            }

            mTerminateThreadWhenDone = false;
            mThreadIsProcessing = false;
            mFrameQueue.Clear();

            Debug.Log("FRAMES SAVER THREAD FINISHED");
        }
    }
}