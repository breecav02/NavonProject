using UnityEngine;
using System;
using TMPro;
using System.Collections;

public class runExperiment : MonoBehaviour
{
    [Header("User Input")]
    public bool playinVR;
    public string participant;
    public bool skipWalkCalibration = true;

    [Header("Experiment State")]
    public string responseMapping = "L:NO R:YES";
    public int trialCount;
    public float trialTime;
    public float thisTrialDuration;
    public bool trialinProgress;

    [HideInInspector]
    public int detectIndex, targState;

    [HideInInspector]
    public bool collectTrialSummary, collectEventSummary, hasResponded;

    // Protected stimulus data
    [HideInInspector]
    public experimentParameters.DetectionTask currentDetectionTask;
    [HideInInspector]
    public experimentParameters.StimulusType currentStimulusType;
    [HideInInspector]
    public bool currentTargetPresent;
    [HideInInspector]
    public char currentGlobalLetter;
    [HideInInspector]
    public char currentLocalLetter;
    [HideInInspector]
    public bool currentIsCongruent;
    [HideInInspector]
    public string currentTrialCategory;

    private bool updateNextNavon;
    bool SetUpSession;
    
    CollectPlayerInput playerInput;
    experimentParameters expParams;
    controlWalkingGuide controlWalkingGuide;
    public DurationStaircase durationStaircase;
    ShowText ShowText;
    FeedbackText FeedbackText;
    targetAppearance targetAppearance;
    public RecordData RecordData;
    
    makeNavonStimulus makeNavonStimulus;

    [SerializeField] GameObject TextScreen;
    [SerializeField] GameObject TextFeedback;
    [SerializeField] GameObject StimulusScreen;

    void Start()
    {
        playerInput = GetComponent<CollectPlayerInput>();
        expParams = GetComponent<experimentParameters>();
        controlWalkingGuide = GetComponent<controlWalkingGuide>();
        durationStaircase = GetComponent<DurationStaircase>();
        RecordData = GetComponent<RecordData>();

        ShowText = TextScreen.GetComponent<ShowText>();
        FeedbackText = TextFeedback.GetComponent<FeedbackText>();

        targetAppearance = StimulusScreen.GetComponent<targetAppearance>();
        makeNavonStimulus = StimulusScreen.GetComponent<makeNavonStimulus>();
        
        togglePlayers();

        trialCount = 0;    
        trialinProgress = false;
        trialTime = 0f;
        collectEventSummary = false;
        hasResponded = false;
        updateNextNavon = false;
        
        SetUpSession = true;

        responseMapping = "L:NO R:YES";
        
        controlWalkingGuide.setGuidetoCentre();
        
        Debug.Log("=== DUAL DETECTION TASK: E and T ===");
        Debug.Log("PER-STIMULUS 2-up-2-down staircase");
        Debug.Log("Response: LEFT = NO (absent), RIGHT = YES (present)");
        Debug.Log("Detection task RANDOMIZED per trial (50% E, 50% T)");
        Debug.Log("4 letters: E, T, F, I");
    }

    void Update()
    {
        if (SetUpSession)
        {
            ShowText.UpdateText(ShowText.TextType.TrialStart);
            SetUpSession = false;
        }

        if (!trialinProgress && playerInput.botharePressed)
        {
            Debug.Log("Starting Trial (Standing)");
            startTrial();
        }

        if (trialinProgress)
        {
            trialTime += Time.deltaTime;

            if (trialTime > thisTrialDuration)
            {
                trialCount++;
                trialPackDown();
            }

            if (trialTime < 0.5f || hasResponded)
            {
                return;
            }

            if (playerInput.anyarePressed)
            {
                processPlayerResponse();
            }
        }
    }

    void togglePlayers()
    {
        if (playinVR)
        {
            GameObject.Find("VR_Player").SetActive(true);
            GameObject.Find("Kb_Player").SetActive(false);
        }
        else
        {
            GameObject.Find("VR_Player").SetActive(false);
            GameObject.Find("Kb_Player").SetActive(true);
        }
    }

    void processPlayerResponse()
    {
        expParams.trialD.clickOnsetTime = trialTime;

        if (hasResponded || detectIndex <= 0)
        {
            return;
        }

        // Use protected variables
        experimentParameters.DetectionTask taskType = currentDetectionTask;
        experimentParameters.StimulusType stimType = currentStimulusType;
        bool targetPresent = currentTargetPresent;
        char globalLetter = currentGlobalLetter;
        char localLetter = currentLocalLetter;
        bool isCongruent = currentIsCongruent;
        string trialCategory = currentTrialCategory;

        // Response mapping
        bool respondedYes = playerInput.rightisPressed;
        bool respondedNo = playerInput.leftisPressed;
        
        // Determine correctness based on target presence
        bool isCorrect = (respondedYes && targetPresent) || (respondedNo && !targetPresent);
        
        string taskLetter = (taskType == experimentParameters.DetectionTask.DetectE) ? "E" : "T";
        string congruencyStr = isCongruent ? "Congruent" : "Incongruent";
        
        if (isCorrect)
        {
            Debug.Log($"✓ {trialCategory} {congruencyStr}: {stimType}");
            Debug.Log($"  Response: {(respondedYes ? "YES" : "NO")} - CORRECT (target {taskLetter} {(targetPresent ? "present" : "absent")})");
        }
        else
        {
            Debug.Log($"✗ {trialCategory} {congruencyStr}: {stimType}");
            Debug.Log($"  Response: {(respondedYes ? "YES" : "NO")} - INCORRECT (target {taskLetter} {(targetPresent ? "present" : "absent")})");
        }

        // Store in trial data
        expParams.trialD.currentTask = taskType;
        expParams.trialD.stimulusType = stimType;
        expParams.trialD.targetPresent = targetPresent;
        expParams.trialD.globalLetter = globalLetter;
        expParams.trialD.localLetter = localLetter;
        expParams.trialD.isCongruent = isCongruent;
        expParams.trialD.trialCategory = trialCategory;
        expParams.trialD.targCorrect = isCorrect ? 1 : 0;
        expParams.trialD.targResponse = respondedYes ? 1 : 0;

        RecordData.extractEventSummary();
        hasResponded = true;

        // ✅ UPDATE STAIRCASE IMMEDIATELY after each stimulus
        if (durationStaircase != null)
        {
            float nextDuration = durationStaircase.ProcessResponse(isCorrect);
            Debug.Log($"[Staircase] Stimulus processed: {(isCorrect ? "✓" : "✗")} → Next duration: {nextDuration:F3}s");
        }

        // Show feedback during practice trials
        if (trialCount < expParams.nPracticeTrials)
        {
            if (expParams.trialD.targCorrect == 1)
            {
                FeedbackText.UpdateText(FeedbackText.TextType.Correct);
                Invoke(nameof(HideFeedbackText), 0.2f);
            }
            else
            {
                FeedbackText.UpdateText(FeedbackText.TextType.Incorrect);
                Invoke(nameof(HideFeedbackText), 0.2f);
            }
        }
        
        updateNextNavon = false;
    }

    private void HideFeedbackText()
    {
        FeedbackText.UpdateText(FeedbackText.TextType.Hide);
    }

    void startTrial()
    {
        controlWalkingGuide.updateScreenHeight();
        ShowText.UpdateText(ShowText.TextType.Hide);
        FeedbackText.UpdateText(FeedbackText.TextType.Hide);

        trialinProgress = true;
        ShowText.UpdateText(ShowText.TextType.Hide);
        trialTime = 0;
        targState = 0;

        thisTrialDuration = expParams.GetTrialDuration();

        expParams.trialD.trialNumber = trialCount;
        expParams.trialD.blockID = expParams.blockTypeArray[trialCount, 0];
        expParams.trialD.trialID = expParams.blockTypeArray[trialCount, 1];
        expParams.trialD.blockType = 0;
        
        // Get detection task for this trial
        int taskValue = expParams.blockTypeArray[trialCount, 2];
        expParams.trialD.currentTask = (experimentParameters.DetectionTask)taskValue;
        
        // Set the task in the stimulus generator
        makeNavonStimulus.navonP.currentTask = expParams.trialD.currentTask;

        string taskName = (expParams.trialD.currentTask == experimentParameters.DetectionTask.DetectE) ? "E" : "T";
        
        Debug.Log($"╔═══════════════════════════════════════╗");
        Debug.Log($"▶️ TRIAL {trialCount + 1} STARTED");
        Debug.Log($"   Block {expParams.trialD.blockID + 1}, Trial {expParams.trialD.trialID + 1}");
        Debug.Log($"   Task: DETECT '{taskName}'");
        Debug.Log($"   Current duration: {expParams.GetStimulusDuration():F3}s");
        Debug.Log($"╚═══════════════════════════════════════╝");

        RecordData.recordPhase = RecordData.phase.collectResponse;
        targetAppearance.startSequence();
    }

    void trialPackDown()
    {
        RecordData.recordPhase = RecordData.phase.stop;
        trialinProgress = false;
        trialTime = 0f;
        targetAppearance.hideNavon();
        
        // Print staircase status
        if (durationStaircase != null)
        {
            Debug.Log($"─────────────────────────────────────────");
            Debug.Log($"Trial {trialCount} complete");
            Debug.Log($"Current duration: {durationStaircase.CurrentDuration:F3}s");
            Debug.Log($"Reversals: {durationStaircase.ReversalCount}");
            Debug.Log($"Threshold estimate: {durationStaircase.EstimatedThreshold:F3}s");
            Debug.Log($"─────────────────────────────────────────");
        }
        
        ShowText.UpdateText(ShowText.TextType.TrialStart);
    }
}