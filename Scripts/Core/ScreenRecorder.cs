
using UnityEngine;
using System.IO;
using Sirenix.OdinInspector;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Screen Recorder will save individual images of active scene in any resolution and of a specific image format
 // including raw, jpg, png, and ppm.  Raw and PPM are the fastest image formats for saving.
 //
 // You can compile these images into a video using ffmpeg:
 // ffmpeg -i screen_3840x2160_%d.ppm -y test.avi
 
 public class ScreenRecorder : MonoBehaviour
 {
     public Camera cam;
     
     // 4k = 3840 x 2160   1080p = 1920 x 1080
     public int captureWidth = 1920;
     public int captureHeight = 1080;
 
     // optional game object to hide during screenshots (usually your scene canvas hud)
     public GameObject hideGameObject; 
 
     // optimize for many screenshots will not destroy any objects so future screenshots will be fast
     public bool optimizeForManyScreenshots = true;
 
     // configure with raw, jpg, png, or ppm (simple raw format)
     public enum Format { RAW, JPG, PNG, PPM };
     public Format format = Format.PPM;
 
     // folder to write output (defaults to data path)
     public string folder;
 
     // private vars for screenshot
     private Rect rect;
     private RenderTexture renderTexture;
     private Texture2D screenShot;
     private int counter = 0; // image #
 
     // commands
     private bool captureScreenshot = false;
     private bool captureVideo = false;
     
     // actions
     public InputActionReference captureScreenshotAction;

     private void Start()
     {
         if (captureScreenshotAction)
            captureScreenshotAction.action.performed += ctx => CaptureScreenshot();
     }

     // create a unique filename using a one-up variable
     private string UniqueFilename(int width, int height)
     {
         // if folder not specified by now use a good default
         if (folder == null || folder.Length == 0)
         {
             folder = Application.dataPath;
             if (Application.isEditor)
             {
                 // put screenshots in folder above asset path so unity doesn't index the files
                 var stringPath = folder + "/..";
                 folder = Path.GetFullPath(stringPath);
             }
             folder += "/screenshots";
 
             // make sure directoroy exists
             System.IO.Directory.CreateDirectory(folder);
 
             // count number of files of specified format in folder
             string mask = string.Format("screen_{0}x{1}*.{2}", width, height, format.ToString().ToLower());
             counter = Directory.GetFiles(folder, mask, SearchOption.TopDirectoryOnly).Length;
         }
 
         // use width, height, and counter for unique file name
         var filename = string.Format("{0}/screen_{1}x{2}_{3}.{4}", folder, width, height, counter, format.ToString().ToLower());
 
         // up counter for next call
         ++counter;
 
         // return unique filename
         return filename;
     }

     public GameObject targetImageObject;

     [Button]
     public void CreateTextureAndApplyToTargetImage()
     {
         var targetImage = targetImageObject.GetComponent<Image>();
         
         if (targetImage == null)
         {
             Debug.LogError("Target image is null. Please pass one as a param or assign to object.");
             return;
         }
         
         var texture = CreateTexture2D();
         texture.Apply();
         targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
     }
     
     public Texture2D CreateTexture2D()
     {
         // hide optional game object if set
         if (hideGameObject != null) hideGameObject.SetActive(false);

         // create screenshot objects if needed
         if (renderTexture == null)
         {
             // creates off-screen render texture that can rendered into
             renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
         }
         
         rect = new Rect(0, 0, captureWidth, captureHeight);
         screenShot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
     
         // get main camera and manually render scene into rt
         cam.targetTexture = renderTexture;
         cam.Render();

         // read pixels will read from the currently active render texture so make our offscreen 
         // render texture active and then read the pixels
         RenderTexture.active = renderTexture;
         screenShot.ReadPixels(rect, 0, 0);

         return screenShot;
     }
     
     [Button]
     public void CaptureScreenshot()
     {
         CreateTexture2D();

         // reset active camera texture and render texture
         cam.targetTexture = null;
         RenderTexture.active = null;

         // get our unique filename
         string filename = UniqueFilename((int) rect.width, (int) rect.height);

         // pull in our file header/data bytes for the specified image format (has to be done from main thread)
         byte[] fileHeader = null;
         byte[] fileData = null;
         if (format == Format.RAW)
         {
             fileData = screenShot.GetRawTextureData();
         }
         else if (format == Format.PNG)
         {
             fileData = screenShot.EncodeToPNG();
         }
         else if (format == Format.JPG)
         {
             fileData = screenShot.EncodeToJPG();
         }
         else // ppm
         {
             // create a file header for ppm formatted file
             string headerStr = string.Format("P6\n{0} {1}\n255\n", rect.width, rect.height);
             fileHeader = System.Text.Encoding.ASCII.GetBytes(headerStr);
             fileData = screenShot.GetRawTextureData();
         }

         // create new thread to save the image to file (only operation that can be done in background)
         new System.Threading.Thread(() =>
         {
             // create file and write optional header with image bytes
             var f = System.IO.File.Create(filename);
             if (fileHeader != null) f.Write(fileHeader, 0, fileHeader.Length);
             f.Write(fileData, 0, fileData.Length);
             f.Close();
             Debug.Log(string.Format("Wrote screenshot {0} of size {1}", filename, fileData.Length));
         }).Start();

         // unhide optional game object if set
         if (hideGameObject != null) hideGameObject.SetActive(true);

         // cleanup if needed
         if (optimizeForManyScreenshots == false)
         {
             bool editor = false;
#if UNITY_EDITOR
             editor = true;
#endif
             if (editor) DestroyImmediate(renderTexture);
             else Destroy(renderTexture);
             
             renderTexture = null;
             screenShot = null;
         }
     }
     
 }