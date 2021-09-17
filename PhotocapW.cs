using UnityEngine;
using System;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.Windows.WebCam;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;

public class PhotocapW : MonoBehaviour
{
    PhotoCapture PhotoCapture = null;
    Texture2D targetTexture = null;
    Resolution cameraResolution;
    Renderer quadRenderer;
    float dt = 0;
    UnityWebRequest unityWebRequest;
    string endpoint = "http://192.168.1.10:5000/";

    int x1;
    int x2;
    int y1;
    int y2;
    int cx;
    int cy;
    string class_id;
    string s;
    byte[] bytes = null;//送信するバイト配列
    Detections detections;

    [System.Serializable]

    public class Detection
    {
        public string class_id;
        public string x1;
        public string x2;
        public string y1;
        public string y2;
    }

    public class Detections
    {
        public Detection[] detections;

    }
        //初期化

        void Start()
        {
            cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            quadRenderer = quad.GetComponent<Renderer>() as Renderer;
            quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));


            quad.transform.parent = this.transform;
            quad.transform.localPosition = new Vector3(0.0f, 0.0f, 0.3f);

        }


        async void StartCapture()
        {


            PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
            {
                PhotoCapture = captureObject;
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;


                PhotoCapture.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
                {

                    PhotoCapture.TakePhotoAsync(OnCapturedPhotoToMemory);
                });
            });
        }

        void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            Destroy(targetTexture);
            targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);

            photoCaptureFrame.UploadImageDataToTexture(targetTexture);


            Destroy(quadRenderer.material.mainTexture);
            quadRenderer.material.mainTexture=targetTexture;
            PhotoCapture.StopPhotoModeAsync(OnStoppedPhotoMode);
            StartCoroutine(Send());

 

        }

        void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {

            PhotoCapture.Dispose();
            PhotoCapture = null;
        }
        void Update()
        {
            dt += Time.deltaTime;

            if (dt > 3)
            {
                dt = 0.0f;
                StartCapture();
            }

        }
    


    IEnumerator Send()
    {

        bytes = targetTexture.EncodeToPNG();

        WWWForm webForm = new WWWForm();
        s = Convert.ToBase64String(bytes);
        webForm.AddField("image", s);


        using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(endpoint, webForm))
        {
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                print(unityWebRequest.error);
            }
            else
            {
                Visualize();


            }

        }
        

    }


    void Visualize()
    {
        String json_response_text = unityWebRequest.downloadHandler.text;
        detections = new Detections();
        detections = JsonUtility.FromJson<Detections>(json_response_text);
        if (detections.detections.Length >= 1)
        {


            x1 = int.Parse(detections.detections[0].x1);
            x2 = int.Parse(detections.detections[0].x2);
            y1 = int.Parse(detections.detections[0].y1);
            y2 = int.Parse(detections.detections[0].y2);
            cx = (x1 + x2) / 2;
            cy = (y1 + y2) / 2;
            class_id = detections.detections[0].class_id;

        }
    }

}