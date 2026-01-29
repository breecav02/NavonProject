using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Linq;

public class SimpleRecordData : MonoBehaviour
{
    /// <summary>
    /// This script produces a simple output to test write/save features.
    /// upon Left click, the data stream is recorded (head pos - xyz pozition).
    /// after 1 sec duration, it is then written to disk at location *outputFolder*
    /// </summary>


    // updated 2025-05 MD to enable gaze origin and direction recordings.
    // preallocate output file, and folder
    string outputFile_pos, outputFolder;
    List<string> outputData_pos = new List<string>();
    private string startTime;

    //assign public GameObj for easey access to hmd and gaze daa (controllers too if needed.)
    public GameObject objHMD; // drag and drop
    public GameObject objGazeInteractor;

    //accessed trigger state:
    CollectPlayerInput CollectPlayerInput;
    GazeVisualizer GazeVisualizer;
    runExperiment runExperiment;
    private bool clickStateL, clickStateR; // to test if input is saved from triggers

    //access the gaze data
    float trialTime;
    //flow handler:
    private bool dataSaveinprogres;
    string projectName = "VIS2AFC_v2";
    // Start is called before the first frame update
    void Start()
    {
        
        //make sure we have access to all components. 
        CollectPlayerInput = GetComponent<CollectPlayerInput>(); // on same GameObj.
        runExperiment = GetComponent<runExperiment>();

        GazeVisualizer = objGazeInteractor.GetComponent<GazeVisualizer>();

        //set up output details:
        outputFolder = GetOutputFolder();
        Debug.Log("saving to location " + outputFolder);
        
        startTime = System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
        dataSaveinprogres = false; // 
        createPositionTextfile();


    }

    // Update is called once per frame
    void Update()
    {


        if (dataSaveinprogres)
        {
            // write each frame to our file:
            writePositionData(); // also increments trialTime for the datasave.
        }

        if (runExperiment.trialTime > 1) //
        {
            //write to disk.
            writeFiletoDisk();

            dataSaveinprogres = false;
        }

    }

    public void startdataSave() // called from other scripts (like runExperiment).
    {
        // if left button clicked, write data for 1 second,
        // write to disk only after that has elapsed,
        // printing status to console.

        dataSaveinprogres = true;

        Debug.Log("Data save beginning");

    }

    public void createPositionTextfile()
    {

        // blockID
        // is updated by the runExp.trialPackdown method, only on the last trial of each block (after data for that trial has been saved).

        outputFile_pos = outputFolder + "test" + "_" + startTime + ".csv";


        string columnNamesPos = "trialTime," +
            "clickstate_L," +
            "clickstate_R," +
            "head_X," +
            "head_Y," +
            "head_Z," +
            "gazeOrigin_X," +
            "gazeOrigin_Y," +
            "gazeOrigin_Z," +
            "gazeDirection_X," +
            "gazeDirection_Y," +
            "gazeDirection_Z," +
            "gazeHit_X," +
            "gazeHit_Y," +
            "gazeHit_Z," +
            "pupilDiameter" +
            "\r\n";

        File.WriteAllText(outputFile_pos, columnNamesPos);

    }

    public void writePositionData()
    {

        Vector3 currentHead = objHMD.transform.position;
        clickStateL = CollectPlayerInput.leftisPressed;
        clickStateR = CollectPlayerInput.rightisPressed;

        // NB that the position in the Transform of the GazeInteration object is the gaze origin. 
        //This is where the gaze ray starts from, calculated as the centre point between the eyes in the virtual space.
        //The Rotation (and forward vector derived from this rotation) represents the gaze direction/which way you are looking.

        Vector3 gazeDirection = objGazeInteractor.transform.forward;
        Vector3 gazeOrigin = objGazeInteractor.transform.position;

        //attempt to get the hitpoint in world space of the ray cast from eyes: 
        Vector3 gazeHit = GazeVisualizer.GazeHitPosition;
        float pupilDiameter = GazeVisualizer.AveragePupilDiameter;

        // DATA input must match the order in create_positionTextFile() above:
        // left, right, xyz.
        // per frame, append the relevant data to per column of our datastructure:
        string data =
                runExperiment.trialTime + "," +
                clickStateL + "," +//    "clickstateL," +
                clickStateR + "," +//"clickstateR," +
                currentHead.x + "," + //"headX," +
                currentHead.y + "," + //"headY," +
                currentHead.z + "," + //"headZ," +
                gazeOrigin.x + "," + //"gazeOX," +
                gazeOrigin.y + "," + //             
                gazeOrigin.z + "," + //             
                gazeDirection.x + "," + //gazeDirection (forward)
                gazeDirection.y + "," + //
                gazeDirection.z + "," + //
                gazeHit.x + "," + // hit in world space
                gazeHit.y + "," + //
                gazeHit.z + "," + //
                pupilDiameter; // average of left and right.


        outputData_pos.Add(data);


    }

    public void writeFiletoDisk()
    {
        saveRecordedDataList(outputFile_pos, outputData_pos);


        // clear cache
        outputData_pos = new List<string>();


    }

    static void saveRecordedDataList(string filePath, List<string> dataList)
    {
        // Robert Tobin Keys:
        // I wrote this with System.IO ----- this is super efficient

        using (StreamWriter writeText = File.AppendText(filePath))
        {
            foreach (var item in dataList)
                writeText.WriteLine(item);
        }
    }
    private string GetOutputFolder()
    {
         string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
        //  projectName defined above
        string parentDir = System.IO.Path.GetDirectoryName(projectRoot);
        string baseOutputPath;

        #if UNITY_EDITOR_WIN
            // Windows: C:/Users/[username]/Documents/Unity Projects/UnityOutputData/
            string userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            baseOutputPath = System.IO.Path.Combine(userProfile, "Documents", "Unity Projects", "UnityOutputData");
        #elif UNITY_EDITOR_OSX
            // Mac: /Users/[username]/Documents/Unity Projects/UnityOutputData/
            string userHome = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            baseOutputPath = System.IO.Path.Combine(userHome, "Documents", "Unity Projects", "UnityOutputData");
        #else
            // Linux or other: ~/Documents/Unity Projects/UnityOutputData/
            string userHome = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            baseOutputPath = System.IO.Path.Combine(userHome, "Documents", "Unity Projects", "UnityOutputData");
        #endif
    
    return System.IO.Path.Combine(baseOutputPath, projectName) + System.IO.Path.DirectorySeparatorChar;

        
    }

}