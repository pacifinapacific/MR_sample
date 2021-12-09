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



public class Simple_Capture : MonoBehaviour
{



    private PhotoCapture _photoCapture = null;
    private Texture2D targetTexture = null;
    private Resolution cameraResolution;
    private Renderer quadRenderer;
    private Camera cam;
    private GameObject quad; //キャプチャ画像描画用
    //public GameObject bbox;

    private int w;
    private int h;
    private int cx;
    private int cy;
    private string class_id;

    [SerializeField]
    private GameObject Label;
    private ToolTip tooltip;

    [SerializeField]
    private GameObject bbox;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("!!! Test Debug Log Message !!!");
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
        targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
        quadRenderer.material.mainTexture = targetTexture;



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




    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // photo capture のリソースをシャットダウンします
        _photoCapture.Dispose();
        _photoCapture = null;
    }

    public void HandleClickEvent()
    {
        StartCapture();
    }


        // Update is called once per frame
        void Update()
    {
        
    }
}
