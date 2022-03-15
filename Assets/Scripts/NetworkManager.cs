using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManager : MonoBehaviour
{

    [Header("Server")]
    [SerializeField] private string serverUploadURL; //server URL
    [SerializeField] private string serverPoseEstimatorURL; //server URL
    
    //for testing in engine only
#if UNITY_EDITOR
    [Header("Debug")] 
    [SerializeField] private bool enableDebug; //if this is true we are in debug mode!
    [SerializeField] private string filePath; //for testing system
#endif

    private void Start()
    {
        //for testing in engine only
#if UNITY_EDITOR
        if (enableDebug)
        {
            UploadFile(filePath);
        } 
#endif
    }


    
    [Serializable] 
    public struct PoseRequest
    {
        public string fileName;
        public int index;
    }

    
    //starting coroutine for sending ASync to server
    public void UploadFile(string localFileName)
    {
        
        StartCoroutine(Upload(localFileName));
    }
    
    //Async file uploader
    IEnumerator UploadFileCo(string localFileName, string uploadURL)
    {
        WWW localFile = new WWW("file:///" + localFileName);
        yield return localFile;
        if (localFile.error == null)
            Debug.Log("Loaded file successfully");
        else
        {
            Debug.Log("Open file error: "+localFile.error);
            yield break; // stop the coroutine here
        }
        WWWForm postForm = new WWWForm();
        // version 1
        //postForm.AddBinaryData("theFile",localFile.bytes);
        // version 2
        postForm.AddBinaryData("file",localFile.bytes,localFileName,"text/plain");
        WWW upload = new WWW(uploadURL,postForm);        
        yield return upload;
        if (upload.error == null)
        {
            while (upload.MoveNext())
            {
                Debug.Log("upload done :" + upload.text);
                yield return upload;
            }
            Debug.Log(upload.isDone);
                Debug.Log("upload done :" + upload.text);
                Debug.Log(upload.bytes.Length);

        }
        else
            Debug.Log("Error during upload: " + upload.error);
    }

    
    IEnumerator Upload(string localFileName) {

        WWW localFile = new WWW("file:///" + localFileName);
        yield return localFile;
        if (localFile.error == null)
            Debug.Log("Loaded file successfully");
        else
        {
            Debug.Log("Open file error: "+localFile.error);
            yield break; // stop the coroutine here
        }
        WWWForm postForm = new WWWForm();

        postForm.AddBinaryData("file",localFile.bytes,localFileName,"text/plain");

        UnityWebRequest www = UnityWebRequest.Post(serverUploadURL, postForm);
        yield return www.SendWebRequest();
        
        if (www.result != UnityWebRequest.Result.Success) {
            Debug.Log(www.error);
        }
        else {
            byte[] results = www.downloadHandler.data;
            using (var stream = new MemoryStream(results))
            using (var binaryStream = new BinaryReader(stream))
            {             
                Debug.Log(results.Length);
            }
            
            Debug.Log(www.downloadHandler.text);
            StartCoroutine(GetPoseEstimates(www.downloadHandler.text));
            Debug.Log("Upload complete!");
        }
    }
    
    IEnumerator GetPoseEstimates(string poseVideoName) {
        PoseRequest poseRequest = new PoseRequest();
        poseRequest.index = 0;
        poseRequest.fileName = poseVideoName;
        
        while (true)
        {
            UnityWebRequest webRequest = new UnityWebRequest(serverPoseEstimatorURL, "POST");
            byte[] encodedPayload = new System.Text.UTF8Encoding().GetBytes(JsonUtility.ToJson(poseRequest));
            webRequest.uploadHandler = (UploadHandler) new UploadHandlerRaw(encodedPayload);
            webRequest.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("cache-control", "no-cache");
        
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result != UnityWebRequest.Result.Success) {
                Debug.Log(webRequest.error);
            }
            else {
                if(webRequest.downloadHandler.text.Equals("Done"))
                    break;
                Debug.Log(webRequest.downloadHandler.text);
                Debug.Log(JsonUtility.FromJson<PoseJson>(webRequest.downloadHandler.text).frame);
                poseRequest.index += 1;
            }

        }

    }
}
