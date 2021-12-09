using UnityEngine;
using System;
using System.Linq;
using UnityEngine.XR;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;
using UnityEngine.XR.WSA.Input;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;



public class ImageCapture : MonoBehaviour
{

    private string endpoint = "http://192.168.1.10:5000/";// AI推論サーバーURL
    private UnityWebRequest unityWebRequest;
    private byte[] bytes = null;//送信するバイト配列

    [System.Serializable]

    public class Detection //物体検出結果class
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







    private PhotoCapture _photoCapture = null;
    private Texture2D targetTexture = null;
    private Resolution cameraResolution;
    private Renderer quadRenderer;
    private Camera cam;
    private GameObject quad; //キャプチャ画像描画用
    //public GameObject bbox;

    private int resize_rate = 8;
    private int img_width;
    private int img_height;

    private int w;
    private int h;
    private int cx;
    private int cy;
    private string class_id;
    private Matrix4x4 projectionMat;
    private Matrix4x4 cameraToWorldMat;


    [SerializeField]
    private GameObject Label;
    private ToolTip tooltip;

    [SerializeField]
    private GameObject bbox;






    // Start is called before the first frame update
    void Start()
    {
        
        cam = Camera.main;

        cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        // キャプチャ画像描画用のquadを生成
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        quad.transform.parent = transform;
        quad.transform.localPosition = new Vector3(0.0f, 0.0f, 0.5f);


        quadRenderer = quad.GetComponent<Renderer>();
        //quadRenderer.material = new Material(Shader.Find("Unlit/Texture"));

        // quadに貼るテクスチャ

        //targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
        img_width = (int)(cameraResolution.width / resize_rate);
        img_height = (int)(cameraResolution.height / resize_rate);

        targetTexture = new Texture2D(img_width, img_height);
        quadRenderer.material.mainTexture = targetTexture;

        // 検出ラベルを描画するtooltip
        tooltip = Label.GetComponent<ToolTip>();



        cx = (int)((float)cameraResolution.width / 2);
        cy = (int)((float)cameraResolution.height / 2);


    }



    // Update is called once per frame
    void Update() //Unityデバッグ用
    {
        if (Input.GetKeyDown("space"))
        {
            // the space key in Unity run mode will do the same thing
            // as a finger tap with the hololens
            //print("space key was pressed");

            //HandleClickEvent();
        }
    }

    //private void TapHandler(TappedEventArgs obj) //タップイベントが発生したら
    //{
    //    print("tap event");
    //    HandleClickEvent();
    //}


    public void HandleClickEvent()//ボタンイベントと紐つけるするためpublic
    {
        StartCapture();



    }


    private async void StartCapture()
    {
        if (_photoCapture != null)
        {
            _photoCapture.StopPhotoModeAsync(OnStoppedPhotoMode);
        }
        PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
    }

    private void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {

        


        _photoCapture = captureObject;
        var cameraParameters = new CameraParameters
            {
                hologramOpacity = 0.0f,
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = CapturePixelFormat.BGRA32
            };

        _photoCapture.StartPhotoModeAsync(cameraParameters, OnPhotoCaptureStarted);
        

    }

    private void OnPhotoCaptureStarted(PhotoCapture.PhotoCaptureResult result)
    {
        StartCoroutine(TakePhoto());
    }

    IEnumerator TakePhoto()
    {
        yield return null;
        _photoCapture.TakePhotoAsync(OnCapturedPhotoToMemory);

    }

    private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {

        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        photoCaptureFrame.TryGetProjectionMatrix(out projectionMat);
        photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMat);


        StartCoroutine(Send());

    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
           // photo capture のリソースをシャットダウンします
           _photoCapture.Dispose();
           _photoCapture = null;
    }





    public IEnumerator Send()
    {
        bytes = targetTexture.EncodeToPNG();

        WWWForm webForm = new WWWForm();
        String s = Convert.ToBase64String(bytes);
        webForm.AddField("image", s);

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

            }

        }
    }

    public void Visualize()
    {
        String json_response_text = unityWebRequest.downloadHandler.text;
        Detections detections = new Detections();
        detections = JsonUtility.FromJson<Detections>(json_response_text);
        if (detections.detections.Length >= 1)
        {


            int x1 = int.Parse(detections.detections[0].x1);
            int x2 = int.Parse(detections.detections[0].x2);
            int y1 = int.Parse(detections.detections[0].y1);
            int y2 = int.Parse(detections.detections[0].y2);
            cx = (x1 + x2) / 2;
            cx = (int)(cx * resize_rate);
            cy = (y1 + y2) / 2;
            cy = (int)(cy * resize_rate);

            w = x2 - x1;
            h = y2 - y1;


            class_id = detections.detections[0].class_id;


            var pixelPos = new Vector2(cx, cy);
            var imagePosZeroToOne = new Vector2(pixelPos.x / cameraResolution.width, 1 - (pixelPos.y / cameraResolution.height));
            var imagePosProjected = (imagePosZeroToOne * 2) - new Vector2(1, 1);    // -1 to 1 space
            var cameraSpacePos = UnProjectVector(projectionMat, new Vector3(imagePosProjected.x, imagePosProjected.y, 1));

            var worldSpaceRayPoint1 = cameraToWorldMat.MultiplyPoint(Vector3.zero);     // camera location in world space
            var worldSpaceRayPoint2 = cameraToWorldMat.MultiplyPoint(cameraSpacePos);




            if (class_id != null)
            {
                tooltip.ToolTipText = class_id;
            }
            RaycastHit hit;
            if (Physics.Raycast(worldSpaceRayPoint1, worldSpaceRayPoint2 - worldSpaceRayPoint1, out hit))
            {
                bbox.transform.position = hit.point;

            }


        }

    }

    public static Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
    {
        Vector3 from = new Vector3(0, 0, 0);
        var axsX = proj.GetRow(0);
        var axsY = proj.GetRow(1);
        var axsZ = proj.GetRow(2);
        from.z = to.z / axsZ.z;
        from.y = (to.y - (from.z * axsY.z)) / axsY.y;
        from.x = (to.x - (from.z * axsX.z)) / axsX.x;
        return from;
    }




}
