using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class experimentParameters : MonoBehaviour
{
    // Trial timing parameters
    [HideInInspector]
    public float preTrialsec, responseWindow, targDurationsec, targetAlpha, nTrials, minITI, maxITI, jittermax;
    public float[] targRange, prevCalibContrast, myCalibContrast;

    // Experiment Design parameters
    public int nTrialsperBlock, nBlocks, nPracticeTrials;
    [HideInInspector]
    public int[,] blockTypeArray;

    runExperiment runExperiment;
    DurationStaircase durationStaircase;

    [HideInInspector]
    public Color preTrialColor, probeColor, targetColor;

    // Stimulus type definitions - 12 total (6 per target letter)
    public enum StimulusType
    {
        // ============ TARGET E (6 types) ============
        // ACTIVE (E present - say YES)
        E_BigE_LittleE = 0,     // Congruent
        E_BigE_LittleI = 1,     // Global-only (incongruent)
        E_BigI_LittleE = 2,     // Local-only (incongruent)
        
        // INACTIVE (E absent - say NO)
        E_BigF_LittleF = 3,     // Congruent foil
        E_BigT_LittleF = 4,     // Incongruent foil
        E_BigF_LittleT = 5,     // Incongruent foil
        
        // ============ TARGET T (6 types) ============
        // ACTIVE (T present - say YES)
        T_BigT_LittleT = 6,     // Congruent
        T_BigT_LittleE = 7,     // Global-only (incongruent)
        T_BigE_LittleT = 8,     // Local-only (incongruent)
        
        // INACTIVE (T absent - say NO)
        T_BigF_LittleF = 9,     // Congruent foil
        T_BigI_LittleF = 10,    // Incongruent foil
        T_BigF_LittleI = 11     // Incongruent foil
    }

    // Detection task type
    public enum DetectionTask
    {
        DetectE = 0,  // Participant looking for E
        DetectT = 1   // Participant looking for T
    }

    [System.Serializable]
    public struct trialData
    {
        public int trialNumber, blockID, trialID, blockType, targCorrect;
        public float targOrien, targLocX, targLocY, targOnsetTime, clickOnsetTime, targResponse, targResponseTime;
        
        // Detection task and stimulus info
        public DetectionTask currentTask;  // Which letter is the target (E or T)?
        public StimulusType stimulusType;  // Which of the 12 stimulus types was shown?
        public char globalLetter;
        public char localLetter;
        public bool targetPresent;         // Is the target letter (E or T) present?
        public bool isCongruent;           // Are global and local the same letter?
        public string trialCategory;       // "Active" or "Inactive"
    }

    public trialData trialD;

    void Start()
    {
        runExperiment = GetComponent<runExperiment>();
        durationStaircase = GetComponent<DurationStaircase>();

        // All trials are standing (15 seconds)
        preTrialsec = 0.5f;
        responseWindow = 0.5f;  // 500ms response window
        targDurationsec = 0.6f;  // Initial value (600ms), controlled by staircase
        jittermax = 0.25f;
        minITI = 0.8f;  // 800ms ISI
        maxITI = 1.5f;  // Max 1.5s ISI

        nTrialsperBlock = 20;
        nBlocks = 6;  // 6 blocks × 20 trials = 120 trials
        nPracticeTrials = 4;  // First 4 trials are practice (2 for E, 2 for T)

        createTrialTypes();
        
        Debug.Log("=== DUAL DETECTION TASK: E and T ===");
        Debug.Log("4 letters used: E, T, F, I");
        Debug.Log("6 stimulus types per target letter:");
        Debug.Log("  - 3 ACTIVE (target present)");
        Debug.Log("  - 3 INACTIVE (target absent - foils)");
        Debug.Log($"{nBlocks} blocks × {nTrialsperBlock} trials = {nBlocks * nTrialsperBlock} trials total");
        Debug.Log("Detection task RANDOMIZED per trial (50% E, 50% T)");
        Debug.Log("All trials are STANDING (15 seconds each)");
    }

    public float GetTrialDuration()
    {
        return 15f;
    }

    public int GetMaxTargetsForTrial()
    {
        return 5;
    }

    public float GetStimulusDuration()
    {
        if (durationStaircase != null)
        {
            targDurationsec = durationStaircase.CurrentDuration;
        }
        return targDurationsec;
    }

    void createTrialTypes()
    {
        int nTrials = nTrialsperBlock * nBlocks;  // 20 × 6 = 120 trials
        
        // blockTypeArray structure:
        // Column 0: blockID (0-5 for 6 blocks)
        // Column 1: trialID (0-19 within each block)
        // Column 2: detectionTask (0 = Detect E, 1 = Detect T) - RANDOMIZED PER TRIAL
        // Column 3: (reserved for future use)

        blockTypeArray = new int[nTrials, 4];
        
        int icounter = 0;
        
        // Create 6 blocks with RANDOMIZED detection task per trial
        for (int iblock = 0; iblock < nBlocks; iblock++)
        {
            int detectECount = 0;
            int detectTCount = 0;
            
            for (int itrial = 0; itrial < nTrialsperBlock; itrial++)
            {
                blockTypeArray[icounter, 0] = iblock;        // Block ID
                blockTypeArray[icounter, 1] = itrial;        // Trial ID within block
                
                // RANDOMIZE detection task for each trial (50/50 E vs T)
                DetectionTask randomTask = (Random.Range(0f, 1f) < 0.5f) ? DetectionTask.DetectE : DetectionTask.DetectT;
                blockTypeArray[icounter, 2] = (int)randomTask;
                blockTypeArray[icounter, 3] = 0;             // Reserved
                
                if (randomTask == DetectionTask.DetectE)
                    detectECount++;
                else
                    detectTCount++;

                icounter++;
            }
            
            Debug.Log($"Block {iblock+1}: {detectECount} Detect E trials, {detectTCount} Detect T trials (randomized)");
        }
        
        // Print overall summary
        Debug.Log($"\n=== EXPERIMENT STRUCTURE ===");
        Debug.Log($"Total: {nTrials} trials across {nBlocks} blocks");
        Debug.Log($"Detection task RANDOMIZED per trial (50% E, 50% T)");
        Debug.Log($"Each trial: 50% active (target present) + 50% inactive (foils)");
        Debug.Log($"Response mapping: LEFT = NO (absent), RIGHT = YES (present)");
    }
}