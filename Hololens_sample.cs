using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;






public class Hololens_sample : MonoBehaviour
{

    private byte[] bytes = null;//送信するバイト配列
    float dt = 0;
    public static string endpoint = "localhost:5000/";
    public Detections detections;


    WebCamTexture webcamTexture;
    private float timeleft;
    public Color32[] color32;

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

    void Start()
    {
        Resolution cameraResolution = UnityEngine.Windows.WebCam.PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Renderer quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        quad.transform.parent = this.transform;
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, 0.3f);


        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(devices[0].name, cameraResolution.width, cameraResolution.height, 30);
        // Webカメラ開始
        webcamTexture.Play();

        // Webカメラの準備が出来るまでラグがあるため、コルーチンで準備出来るまで待つ
        StartCoroutine(WebCamTextureInitialize(quadRenderer));



    }








    IEnumerator WebCamTextureInitialize(Renderer quadRenderer)
    {
        while (true)
        {
            if (webcamTexture.width > 16 && webcamTexture.height > 16)
            {


                //GetComponent<Renderer>().material.mainTexture = webcamTexture;
                //quadRenderer.material.SetTexture("_MainTex", webcamTexture);
                quadRenderer.material.mainTexture = webcamTexture;
                //colorArray.colors = new Color32[webcamTexture.width * webcamTexture.height];
                //webcamTexture.GetPixels32(colorArray.colors);


                Debug.Log("sucesss");
                //StartCoroutine(Send());
                break;

            }
            yield return null;
        }
    }



    IEnumerator Send()
    {
        Texture2D texture = new Texture2D(webcamTexture.width, webcamTexture.height);
        color32 = webcamTexture.GetPixels32();
        texture.SetPixels32(color32);
        texture.Apply();
        bytes = texture.EncodeToPNG();
        Destroy(texture);

        WWWForm webForm = new WWWForm();
        String s = Convert.ToBase64String(bytes);
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
                //int("Finished Uploading Screenshot");
                String json_response_text = unityWebRequest.downloadHandler.text;
                detections = new Detections();
                detections = JsonUtility.FromJson<Detections>(json_response_text);
                //string current_objectid = detections.detection[0].class_id;
                if (detections.detections.Length >= 1)
                {
                    string current_objectid = detections.detections[0].class_id;
                    Debug.Log(current_objectid);

                }
               

                Debug.Log(detections);
            }
        }










    }

    void Update()
    {
        dt += Time.deltaTime;
        if (dt > 3)
        {
            dt = 0.0f;
            StartCoroutine(Send());
        }
    }

}
