using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class RecordData : MonoBehaviour
{
    string outputFile_pos, outputFile_posEye, outputFile_summary, outputFolder;
    List<string> outputData_pos = new List<string>();
    List<string> outputData_summary = new List<string>();
    private string startTime;
    float data_trialTime;
    
    public GameObject objHMD;
    public GameObject objGazeInteractor;

    CollectPlayerInput CollectPlayerInput;
    GazeVisualizer GazeVisualizer;
    runExperiment runExperiment;
    experimentParameters experimentParameters;
    
    private bool clickStateL, clickStateR;
    float trialTime;
    private bool dataSaveinprogres;
    string projectName = "DualDetection_ET_Standing";

    public enum phase
    {
        idle,
        collectResponse,
        collectTrialSummary,
        stop
    };

    public phase recordPhase = phase.idle;

    void Start()
    {
        CollectPlayerInput = GetComponent<CollectPlayerInput>();
        runExperiment = GetComponent<runExperiment>();
        experimentParameters = GetComponent<experimentParameters>();
        GazeVisualizer = GetComponent<GazeVisualizer>();

        outputFolder = GetOutputFolder();
        Debug.Log("saving to location " + outputFolder);

        startTime = System.DateTime.Now.ToString("yyyy-MM-dd-hh-mm");
        dataSaveinprogres = false;

        if (runExperiment.playinVR)
        {
            createPositionTextfile();
        }
        createSummaryTextfile();
    }

    void Update()
    {
        if (recordPhase == phase.idle)
        {
            if (data_trialTime > 0)
            {
                data_trialTime = 0;
            }
        }

        if (recordPhase == phase.collectResponse)
        {
            if (runExperiment.playinVR)
            {
                writePositionData();
            }
        }

        if (recordPhase == phase.stop)
        {
            if (runExperiment.playinVR)
            {
                writeFiletoDisk();
            }
            dataSaveinprogres = false;
        }
    }

    public void createPositionTextfile()
    {
        outputFile_pos = outputFolder + runExperiment.participant + "_" + startTime + ".csv";

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

        Vector3 gazeDirection = objGazeInteractor.transform.forward;
        Vector3 gazeOrigin = objGazeInteractor.transform.position;
        Vector3 gazeHit = GazeVisualizer.GazeHitPosition;
        float pupilDiameter = GazeVisualizer.AveragePupilDiameter;

        string data =
                runExperiment.trialTime + "," +
                clickStateL + "," +
                clickStateR + "," +
                currentHead.x + "," +
                currentHead.y + "," +
                currentHead.z + "," +
                gazeOrigin.x + "," +
                gazeOrigin.y + "," +
                gazeOrigin.z + "," +
                gazeDirection.x + "," +
                gazeDirection.y + "," +
                gazeDirection.z + "," +
                gazeHit.x + "," +
                gazeHit.y + "," +
                gazeHit.z + "," +
                pupilDiameter;

        outputData_pos.Add(data);
    }

    private void createSummaryTextfile()
    {
        outputFile_summary = outputFolder + runExperiment.participant + "_" + startTime + "_trialsummary.csv";

        string columnNamesSumm = "date," +
           "participant," +
           "respmap," +
           "trial," +
           "block," +
           "trialID," +
           "detectionTask," +
           "stimulusType," +
           "globalLetter," +
           "localLetter," +
           "targetPresent," +
           "isCongruent," +
           "trialCategory," +
           "stimulusDuration," +
           "targOnset," +
           "clickOnset," +
           "reactionTime," +
           "targResponse," +
           "correctResponse," +
           "targLocX," +
           "targLocY";

        columnNamesSumm += "\r\n";

        File.WriteAllText(outputFile_summary, columnNamesSumm);
    }

    public void extractEventSummary()
    {
        // Calculate reaction time (RT - Onset)
        float reactionTime = experimentParameters.trialD.clickOnsetTime - experimentParameters.trialD.targOnsetTime;
        
        string data =
                  System.DateTime.Now.ToString("yyyy-MM-dd") + "," +
                  runExperiment.participant + "," +
                  runExperiment.responseMapping + "," +
                  runExperiment.trialCount + "," +
                  experimentParameters.trialD.blockID + "," +
                  experimentParameters.trialD.trialID + "," +
                  experimentParameters.trialD.currentTask + "," +
                  experimentParameters.trialD.stimulusType + "," +
                  experimentParameters.trialD.globalLetter + "," +
                  experimentParameters.trialD.localLetter + "," +
                  experimentParameters.trialD.targetPresent + "," +
                  experimentParameters.trialD.isCongruent + "," +
                  experimentParameters.trialD.trialCategory + "," +
                  experimentParameters.targDurationsec + "," +
                  experimentParameters.trialD.targOnsetTime + "," +
                  experimentParameters.trialD.clickOnsetTime + "," +
                  reactionTime + "," +
                  experimentParameters.trialD.targResponse + "," +
                  experimentParameters.trialD.targCorrect + "," +
                  experimentParameters.trialD.targLocX + "," +
                  experimentParameters.trialD.targLocY;

        outputData_summary.Add(data);
        runExperiment.collectEventSummary = false;
    }

    public void saveonBlockEnd()
    {
        saveRecordedDataList(outputFile_pos, outputData_pos);
        saveRecordedDataList(outputFile_summary, outputData_summary);

        outputData_pos = new List<string>();
        outputData_summary = new List<string>();
    }

    public void writeFiletoDisk()
    {
        if (runExperiment.playinVR)
        {
            saveRecordedDataList(outputFile_pos, outputData_pos);
            saveRecordedDataList(outputFile_summary, outputData_summary);
        }
        else
        {
            saveRecordedDataList(outputFile_summary, outputData_summary);
        }

        outputData_pos = new List<string>();
        outputData_summary = new List<string>();
    }

    private void OnApplicationQuit()
    {
        if (runExperiment.playinVR)
        {
            saveRecordedDataList(outputFile_pos, outputData_pos);
            saveRecordedDataList(outputFile_summary, outputData_summary);
        }
        else
        {
            saveRecordedDataList(outputFile_summary, outputData_summary);
        }
    }

    static void saveRecordedDataList(string filePath, List<string> dataList)
    {
        using (StreamWriter writeText = File.AppendText(filePath))
        {
            foreach (var item in dataList)
                writeText.WriteLine(item);
        }
    }

    private string GetOutputFolder()
    {
        string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
        string parentDir = System.IO.Path.GetDirectoryName(projectRoot);
        string baseOutputPath;

#if UNITY_EDITOR_WIN
        string userProfile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        baseOutputPath = System.IO.Path.Combine(userProfile, "Documents", "Unity Projects", "UnityOutputData");
#elif UNITY_EDITOR_OSX
        string userHome = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        baseOutputPath = System.IO.Path.Combine(userHome, "Documents", "Unity Projects", "UnityOutputData");
#else
        string userHome = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
        baseOutputPath = System.IO.Path.Combine(userHome, "Documents", "Unity Projects", "UnityOutputData");
#endif

        return System.IO.Path.Combine(baseOutputPath, projectName) + System.IO.Path.DirectorySeparatorChar;
    }
}