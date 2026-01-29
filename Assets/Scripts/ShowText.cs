using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ShowText : MonoBehaviour
{
    public enum TextType
    {
        Hide = 0,
        Welcome = 1,
        TrialStart = 3,
        ExperimentComplete = 4
    }

    private TextMeshProUGUI textMesh;
    private Dictionary<TextType, string> textStrings;

    [SerializeField]
    GameObject scriptHolder;
    experimentParameters expParams;
    runExperiment runExperiment;
    
    [SerializeField]
    GameObject TextBG;
    
    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        
        // Automatically find scriptHolder if not assigned
        if (scriptHolder == null)
        {
            scriptHolder = GameObject.Find("scriptHolder");
            
            if (scriptHolder == null)
            {
                Debug.LogError("ShowText: Could not find 'scriptHolder' GameObject! Please check the hierarchy.");
                return;
            }
            else
            {
                Debug.Log("ShowText: Successfully found scriptHolder GameObject.");
            }
        }
        
        // Automatically find TextBG if not assigned
        if (TextBG == null)
        {
            TextBG = GameObject.Find("TextBG");
            
            if (TextBG == null)
            {
                Debug.LogError("ShowText: Could not find 'TextBG' GameObject! Please check the hierarchy or assign it in the Inspector.");
            }
            else
            {
                Debug.Log("ShowText: Successfully found TextBG GameObject.");
            }
        }
        
        expParams = scriptHolder.GetComponent<experimentParameters>();
        runExperiment = scriptHolder.GetComponent<runExperiment>();

        textStrings = new Dictionary<TextType, string>
        {
            [TextType.Hide] = "",
            [TextType.Welcome] = "",  // Empty - redirects to TrialStart anyway
            [TextType.TrialStart] = ""
        };
    }

    public void UpdateText(TextType textType)
    {
        if (textStrings == null)
        {
            Debug.LogWarning($"ShowText dictionary not yet initialized. Attempting to show: {textType}");
            return;
        }

        // REDIRECT Welcome to TrialStart - NO WELCOME SCREEN!
        if (textType == TextType.Welcome)
        {
            textType = TextType.TrialStart;
        }

        if (textStrings.ContainsKey(textType))
        {
            if (textType == TextType.TrialStart)
            {
                textMesh.color = Color.black;
                
                // Get detection task for next trial
                int nextTrialIndex = runExperiment.trialCount;
                string taskText = "";
                string targetLetter = "";
                
                if (nextTrialIndex < expParams.blockTypeArray.GetLength(0))
                {
                    int taskValue = expParams.blockTypeArray[nextTrialIndex, 2];
                    experimentParameters.DetectionTask task = (experimentParameters.DetectionTask)taskValue;
                    
                    if (task == experimentParameters.DetectionTask.DetectE)
                    {
                        targetLetter = "E";
                        taskText = "<color=#FF1493><size=140%>Detect 'E'</size></color>";
                    }
                    else  // DetectT
                    {
                        targetLetter = "T";
                        taskText = "<color=#1E90FF><size=140%>Detect 'T'</size></color>";
                    }
                }
                
                int blockNum = expParams.blockTypeArray[nextTrialIndex, 0] + 1;
                int trialInBlock = expParams.blockTypeArray[nextTrialIndex, 1] + 1;
                
                // Build the message
                textMesh.text = "<b>Dual Detection Task</b>" +
                    $"\n\n{taskText}" +
                    "\n\n<b><color=#FF0000>L: NO  |  R: YES</color></b>" +
                    "\n\nPull both triggers to begin" +
                    $"\n\nTrial {trialInBlock} / 20" +
                    $"\n(Block {blockNum} of 6)";
                    
                if (TextBG != null) TextBG.SetActive(true);
                return;
            }
            else
            {
                textMesh.color = Color.black;
                textMesh.text = textStrings[textType];
                if (TextBG != null)
                {
                    if (textType == TextType.Hide)
                    {
                        TextBG.SetActive(false);
                    }
                    else
                    {
                        TextBG.SetActive(true);
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning($"TextType {textType} not found in dictionary");
        }
    }
}