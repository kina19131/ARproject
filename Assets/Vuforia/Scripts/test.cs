using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;
using UnityEngine.Networking;
using System;
using System.Net; 
using System.IO;
using UnityEngine.Video;
using System.Linq;
using UnityEngine.SceneManagement;

public class test : MonoBehaviour, ICloudRecoEventHandler
{
    public float BaseScreenHeight = 600f;
    private CloudRecoBehaviour mCloudRecoBehaviour;
    public ImageTargetBehaviour ImageTargetTemplate;
    private bool misScanning = false;
    private string mTargetMetadata = "";
    public VideoClip videoClip;
    private AudioClip _audio;


    // Use this for initialization
    void Start()
    {
        mCloudRecoBehaviour = GetComponent<CloudRecoBehaviour>();

        if (mCloudRecoBehaviour)
        {
            mCloudRecoBehaviour.RegisterEventHandler(this);
        }
    }

    public void OnInitialized()
    {
        System.Diagnostics.Debug.WriteLine("Cloud Reco intitialized");
    }

    public void OnInitError(TargetFinder.InitState initError)
    {
        System.Diagnostics.Debug.WriteLine("Cloud Reco init error" + initError.ToString());
    }

    public void OnUpdateError(TargetFinder.UpdateState updateError)
    {
        System.Diagnostics.Debug.WriteLine("Cloud Reco update error" + updateError.ToString());
    }

    //when scanning
    public void OnStateChanged(bool scanning)
    {
        //misScanning = false defined before as a private bool 
        misScanning = scanning;

        if (scanning)
        {
            var tracker = TrackerManager.Instance.GetTracker<ObjectTracker>();
            tracker.TargetFinder.ClearTrackables(false);
        }
    }


#region filtering out different types of metadata and do something according to it - this is for mp4 file
    //main function that downloads and play the video 
    private IEnumerator downloadAndPlayVideo(string videoUrl, string saveFileName, bool overwriteVideo)
    {
        //Where to Save the Video
        string saveDir = Path.Combine(Application.persistentDataPath, saveFileName);

        //Play back Directory
        string playbackDir = saveDir;

        #if UNITY_IPHONE 
        playbackDir = "file://" + saveDir;
        #endif

        bool downloadSuccess = false;
        byte[] vidData = null;

        //Check if the video file exist before downloading it again using System.Linq
        //finding out the status
        string[] persistantData = Directory.GetFiles(Application.persistentDataPath);
        if (persistantData.Contains(playbackDir) && !overwriteVideo)
            //video already exists
        {
            Debug.Log("Video already exist. Playing it now");
            //Play Video
            playVideo(playbackDir);
            //EXIT
            yield break;
        }
        else if (persistantData.Contains(playbackDir) && overwriteVideo)
            //video already exists but override 
        {
            Debug.Log("Video already exist but we are Re-downloading it");
            yield return downloadData(videoUrl, (status, dowloadData) =>
            {
                downloadSuccess = status;
                vidData = dowloadData;
            });
        }

        else
            //video never existed
        {
            Debug.Log("Video Does not exist. Downloading video");
            yield return downloadData(videoUrl, (status, dowloadData) =>
            {
                downloadSuccess = status;
                vidData = dowloadData;
            });
        }

        //Save then play if there was no download error
        if (downloadSuccess)
        {
            //Save Video
            saveVideoFile(saveDir, vidData);
            //Play Video
            playVideo(playbackDir);
        }
    }


    //sub function 1
    //Actually downloads the Video 
    public IEnumerator downloadData(string videoUrl, Action<bool, byte[]> result)
    {
        //Download Video
        UnityWebRequest webRequest = UnityWebRequest.Get(videoUrl);
        webRequest.Send();
        //giving a warning that it's obsolete but it can be ignored...

        //Wait until download is done
        while (!webRequest.isDone)
        {
            //shows the percentage of downloadng
            Debug.Log("Downloading: " + webRequest.downloadProgress);
            yield return null;
        }

        //Exit if we encountered error
        if (webRequest.isNetworkError)
        {
            Debug.Log("Error while downloading Video: " + webRequest.error);
            yield break; //EXIT
        }

        Debug.Log("Video Downloaded");
        //Retrieve downloaded Data
        result(!webRequest.isNetworkError, webRequest.downloadHandler.data);
    }

    //sub function 2
    //Saves the downloaded video into a folder
    public bool saveVideoFile(string saveDir, byte[] vidData)
    {
        try
        {
            FileStream stream = new FileStream(saveDir, FileMode.Create);
            stream.Write(vidData, 0, vidData.Length);
            stream.Close();
            Debug.Log("Video Downloaded to: " + saveDir.Replace("/", "\\"));
            return true;
        }

        //if any error appears
        catch (Exception e)
        {
            Debug.Log("Error while saving Video File: " + e.Message);
        }
        return false;
    }

    //sub function 3
    //Plays the video
    void playVideo(string path)
    {
        Debug.Log("Video will be played soon ");
        
        Handheld.PlayFullScreenMovie(path, Color.black,
            FullScreenMovieControlMode.Full, FullScreenMovieScalingMode.AspectFit);
    }

#endregion

    //based on the scan, bring the metadata & descriminating depends on types of metadata
    public void OnNewSearchResult(TargetFinder.TargetSearchResult targetSearchResult)
    {
        mTargetMetadata = targetSearchResult.MetaData;

        //since got the data because the object has been successfully scanned, to not let it be scanned again, disable scanning
        mCloudRecoBehaviour.CloudRecoEnabled = false;

        //if metadata to bring is a video 
        if (mTargetMetadata.Contains(".mp4"))
        {
            string url = mTargetMetadata;
            StartCoroutine(downloadAndPlayVideo(url, "MetaVideo.mp4", true));
            mTargetMetadata = "The video is downloading and will be played soon. Please hold on.";
        }

        //if metadata to bring is an audio
        else if (mTargetMetadata.Contains(".mp3"))
        {
            //just to make sure mp4 is working, just told it to return text for now
            return;
        }

        //if it's just a text file
        else
        {
            return;
        }

    }


    //display the metadata brought from server onto the screen using GUI 
    void OnGUI()
    {

        GUIStyle boxStyle = new GUIStyle();
        boxStyle = "box";
        boxStyle.wordWrap = true;
        boxStyle.fontSize = 25;
        boxStyle.alignment = TextAnchor.MiddleCenter;

        //either says scanning/ scanned sucessfully based on whether the image has    been scanned or not (true or false)
        GUI.Box(new Rect(Screen.width * (0.35f / 6.55f), Screen.height * (0.52f / 6.3f), Screen.width * (1.2f / 6.55f), Screen.height * (0.35f / 6.3f)), misScanning ? "Scanning" : "Scanned Successfully", boxStyle);

        if (GUI.Button(new Rect(Screen.width * (5f / 6.55f), Screen.height * (5.78f / 6.3f), Screen.width * (1.23f / 6.55f), Screen.height * (0.26f / 6.3f)), "Back To Home", boxStyle))
        {
            Debug.Log("moved back to the mainmenu");
            SceneManager.LoadScene("MainMenuuuuu");
        }

        if (!misScanning)
            //once it has been sucessfully scanned
        {
            GUI.Box(new Rect(Screen.width * (0.35f / 6.55f), Screen.height * (1.31f / 6.3f), Screen.width * (2.8f / 6.55f), Screen.height * (3.2f / 6.3f)), "Information:" + "\r\n" + mTargetMetadata);

            //button that the user can click on to restart the scanning process
            if (GUI.Button(new Rect(Screen.width * (5f / 6.55f), Screen.height * (5.04f / 6.3f), Screen.width * (1.23f / 6.55f), Screen.height * (0.26f / 6.3f)), "Re-scan", boxStyle))
            {
                mCloudRecoBehaviour.CloudRecoEnabled = true;
            }

            //button to exit 
            if (GUI.Button(new Rect(Screen.width * (5f / 6.55f), Screen.height * (5.41f / 6.3f), Screen.width * (1.23f / 6.55f), Screen.height * (0.26f / 6.3f)), "Quit", boxStyle))
            {
                Debug.Log("has quit game");
                Application.Quit();
            }
        }

    }
}