using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using OpenCvSharp;







public class Hololens_sample : MonoBehaviour
{

    private byte[] bytes = null;//送信するバイト配列
    float dt = 0;
    //public static string endpoint = "localhost:5000/";
    public static string endpoint = "http://192.168.1.10:5000/";
    public Detections detections;
    public UnityWebRequest unityWebRequest;
    public int x1;
    public int x2;
    public int y1;
    public int y2;
    public int cx;
    public int cy;
    public string class_id;
    //public GameObject quad;
    Renderer quadRenderer;


    WebCamTexture webcamTexture;
    private float timeleft;
    public Color32[] color32;

    Texture2D cap_tex;
    Texture2D out_tex;


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
        quad.transform.localScale = new Vector3(0.2f, 0.2f,0.2f);
        //Renderer quadRenderer=quad.GetComponent<Renderer>() as Renderer;
        quadRenderer = quad.GetComponent<Renderer>() as Renderer;
        quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        quad.transform.parent = this.transform;
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, 0.3f);


        WebCamDevice[] devices = WebCamTexture.devices;
        //webcamTexture = new WebCamTexture(devices[0].name, cameraResolution.width, cameraResolution.height, 30);
        webcamTexture = new WebCamTexture(devices[0].name, cameraResolution.width/3, cameraResolution.height/3, 30);
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
                quadRenderer.material.mainTexture = webcamTexture;
                Debug.Log("sucesss");
                break;

            }
            yield return null;
        }
    }

    void Visualize()
    {
        String json_response_text = unityWebRequest.downloadHandler.text;
        detections = new Detections();
        detections = JsonUtility.FromJson<Detections>(json_response_text);
        if (detections.detections.Length >= 1)
        {

            //int x1 = int.Parse(detections.detections[0].x1);
            //int x2 = int.Parse(detections.detections[0].x2);
            //int y1 = int.Parse(detections.detections[0].y1);
            //int y2 = int.Parse(detections.detections[0].y2);
            //int cx = (x1 + x2) / 2;
            //int cy = (y1 + y2) / 2;


            x1 = int.Parse(detections.detections[0].x1);
            x2 = int.Parse(detections.detections[0].x2);
            y1 = int.Parse(detections.detections[0].y1);
            y2 = int.Parse(detections.detections[0].y2);
            cx = (x1 + x2) / 2;
            cy = (y1 + y2) / 2;
            class_id = detections.detections[0].class_id;


            //Mat mat = OpenCvSharp.Unity.TextureToMat(webcamTexture);
            //mat = OpenCvSharp.Unity.TextureToMat(webcamTexture);
            //Cv2.Rectangle(mat, new OpenCvSharp.Rect(x1, y1, x2, y2), new Scalar(0, 0, 255), 3, LineTypes.AntiAlias);
            //Cv2.PutText(mat, detections.detections[0].class_id, new OpenCvSharp.Point(cx, cy), HersheyFonts.HersheyComplexSmall, 2, new Scalar(255, 0, 0), 3, LineTypes.AntiAlias);
            //out_tex = OpenCvSharp.Unity.MatToTexture(mat);

            //quadRenderer.material.mainTexture = out_tex;
        }
    }



    IEnumerator Send()
    {
        Texture2D texture = new Texture2D(webcamTexture.width, webcamTexture.height);
        color32 = webcamTexture.GetPixels32();
        texture.SetPixels32(color32);
        //texture.Apply();
        //texture.Resize(webcamTexture.width / 2, webcamTexture.height / 2);
        texture.Apply();
        bytes = texture.EncodeToPNG();
        Destroy(texture);

        WWWForm webForm = new WWWForm();
        String s = Convert.ToBase64String(bytes);
        webForm.AddField("image", s);
            //webForm.AddField("image", "image");

            //using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(endpoint, webForm))
        using (unityWebRequest = UnityWebRequest.Post(endpoint, webForm))
            {
            yield return unityWebRequest.SendWebRequest();
            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                print(unityWebRequest.error);
            }
            else
            {
                Visualize();
                //String json_response_text = unityWebRequest.downloadHandler.text;
                //detections = new Detections();
                //detections = JsonUtility.FromJson<Detections>(json_response_text);
                //if (detections.detections.Length >= 1)
                //{

                //    int x1 = int.Parse(detections.detections[0].x1);
                //    int x2 = int.Parse(detections.detections[0].x2);
                //    int y1 = int.Parse(detections.detections[0].y1);
                //    int y2 = int.Parse(detections.detections[0].y2);
                //    int cx = (x1 + x2) / 2;
                //    int cy = (y1 + y2) / 2;


                //    Mat mat = OpenCvSharp.Unity.TextureToMat(webcamTexture);
                //    Cv2.Rectangle(mat, new OpenCvSharp.Rect(x1, y1, x2, y2), new Scalar(0, 0, 255), 3, LineTypes.AntiAlias);
                //    Cv2.PutText(mat, detections.detections[0].class_id, new OpenCvSharp.Point(cx, cy), HersheyFonts.HersheyComplexSmall, 2, new Scalar(255, 0, 0), 3, LineTypes.AntiAlias);
                //    out_tex =OpenCvSharp.Unity.MatToTexture(mat);

                //    quadRenderer.material.mainTexture = out_tex;


                }

        }

    }

    void Update()
    {
        //Task.Run(Send);
        Mat mat = OpenCvSharp.Unity.TextureToMat(webcamTexture);
        if (class_id.Length>=2)
        {
            Cv2.PutText(mat, class_id, new OpenCvSharp.Point(cx, cy), HersheyFonts.HersheyComplexSmall, 2, new Scalar(255, 0, 0), 3, LineTypes.AntiAlias);
        }
        Cv2.Rectangle(mat, new OpenCvSharp.Rect(x1, y1, x2, y2), new Scalar(0, 0, 255), 3, LineTypes.AntiAlias);
        //Cv2.PutText(mat,class_id, new OpenCvSharp.Point(cx, cy), HersheyFonts.HersheyComplexSmall, 2, new Scalar(255, 0, 0), 3, LineTypes.AntiAlias);
        out_tex = OpenCvSharp.Unity.MatToTexture(mat);
        quadRenderer.material.mainTexture = out_tex;
        //StartCoroutine(Send());
        dt += Time.deltaTime;
        if (dt > 3) 
        {
            dt = 0.0f;
            StartCoroutine(Send());
        }
    }

}
