using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class targetAppearance : MonoBehaviour
{
    
    public bool processNoResponse;
    private float waitTime;
    private float trialDuration;
    private float[] targRange, preTargISI, gapsare;
    runExperiment runExperiment;
    Renderer rend;
    makeNavonStimulus makeNavonStimulus;
    experimentParameters expParams;

    [SerializeField]
    GameObject scriptHolder;

    private Color targColor;
    bool includeForwardMask = true;  // ENABLED: Show hash mask BEFORE stimuli (300ms)

    private void Start()
    {
        runExperiment = scriptHolder.GetComponent<runExperiment>();
        expParams = scriptHolder.GetComponent<experimentParameters>();
        makeNavonStimulus = GetComponent<makeNavonStimulus>();
        processNoResponse = false;
        targColor = new Color(1f, 1f, 1f);
    }

    public void startSequence()
    {
        trialDuration = expParams.GetTrialDuration();

        targRange = new float[2];
        targRange[0] = expParams.preTrialsec;
        targRange[1] = trialDuration - expParams.responseWindow - expParams.preTrialsec;

        makeNavonStimulus.hideNavon();

        int maxTargetsThisTrial = expParams.GetMaxTargetsForTrial();

        Debug.Log($"Trial {runExperiment.trialCount+1}: {maxTargetsThisTrial} stimuli, Duration: {trialDuration:F2}s");

        gapsare = new float[maxTargetsThisTrial];
        preTargISI = new float[maxTargetsThisTrial];

        // Evenly space stimuli
        float availableTime = targRange[1] - targRange[0];
        float timePerStimulus = availableTime / maxTargetsThisTrial;
        
        for (int itargindx = 0; itargindx < gapsare.Length; itargindx++)
        {
            float centreTargTime = targRange[0] + (itargindx * timePerStimulus) + Random.Range(-0.1f, 0.1f);
            centreTargTime = Mathf.Clamp(centreTargTime, targRange[0], targRange[1] - 0.1f);
            preTargISI[itargindx] = centreTargTime;
        }

        StartCoroutine("trialProgress");
    }

    IEnumerator trialProgress()
    {
        while (runExperiment.trialinProgress)
        {
            runExperiment.detectIndex = 0;

            yield return new WaitForSecondsRealtime(expParams.preTrialsec);

            for (int itargindx = 0; itargindx < gapsare.Length; itargindx++)
            {
                if (itargindx == 0)
                {
                    waitTime = preTargISI[0];
                }
                else
                {
                    waitTime = preTargISI[itargindx] - runExperiment.trialTime;
                }

                if (waitTime < 0.1f)
                {
                    Debug.LogWarning($"Skipping target {itargindx + 1} - insufficient time");
                    continue;
                }

                yield return new WaitForSecondsRealtime(waitTime);

                // FORWARD MASK: Show mask BEFORE stimulus for 300ms
                if (includeForwardMask)
                {
                    makeNavonStimulus.backwardMask();  // Shows the hash grid
                    yield return new WaitForSecondsRealtime(0.3f);  // 300ms mask
                    makeNavonStimulus.hideNavon();  // Back to fixation
                }

                // Generate stimulus - task is already set in runExperiment
                makeNavonStimulus.GenerateNavon();
                makeNavonStimulus.showNavon();
                runExperiment.targState = 1;
                runExperiment.detectIndex = itargindx + 1;
                runExperiment.hasResponded = false;
                
                // Store in trial data
                expParams.trialD.targOnsetTime = runExperiment.trialTime;
                expParams.trialD.stimulusType = makeNavonStimulus.navonP.stimulusType;
                expParams.trialD.targetPresent = makeNavonStimulus.navonP.targetPresent;
                expParams.trialD.targLocX = makeNavonStimulus.navonP.px;
                expParams.trialD.targLocY = makeNavonStimulus.navonP.py;
                expParams.trialD.globalLetter = makeNavonStimulus.navonP.globalLetter;
                expParams.trialD.localLetter = makeNavonStimulus.navonP.localLetter;
                expParams.trialD.isCongruent = makeNavonStimulus.navonP.isCongruent;
                expParams.trialD.trialCategory = makeNavonStimulus.navonP.trialCategory;
                
                // Store in protected variables
                runExperiment.currentDetectionTask = makeNavonStimulus.navonP.currentTask;
                runExperiment.currentStimulusType = makeNavonStimulus.navonP.stimulusType;
                runExperiment.currentTargetPresent = makeNavonStimulus.navonP.targetPresent;
                runExperiment.currentGlobalLetter = makeNavonStimulus.navonP.globalLetter;
                runExperiment.currentLocalLetter = makeNavonStimulus.navonP.localLetter;
                runExperiment.currentIsCongruent = makeNavonStimulus.navonP.isCongruent;
                runExperiment.currentTrialCategory = makeNavonStimulus.navonP.trialCategory;

                // Use adaptive stimulus duration
                float currentStimulusDuration = expParams.GetStimulusDuration();
                yield return new WaitForSecondsRealtime(currentStimulusDuration);
                
                makeNavonStimulus.hideNavon();
                runExperiment.targState = 0;

                // Wait for response window (no mask after stimulus)
                yield return new WaitForSecondsRealtime(expParams.responseWindow);

                // Handle no response - ALWAYS INCORRECT
                if (!runExperiment.hasResponded)
                {
                    processNoResponse = true;
                    
                    // NO RESPONSE IS ALWAYS WRONG - participants must respond
                    bool noResponseCorrect = false;
                    
                    string stimCategory = makeNavonStimulus.navonP.trialCategory;
                    Debug.Log($"<color=red>No response to {stimCategory} stimulus ({makeNavonStimulus.navonP.stimulusType}) - INCORRECT (must respond)</color>");
                    
                    expParams.trialD.targCorrect = 0;  // Always incorrect
                    expParams.trialD.targResponse = -1;  // -1 indicates no response
                    expParams.trialD.clickOnsetTime = runExperiment.trialTime;
                    
                    runExperiment.RecordData.extractEventSummary();
                    
                    // ✅ UPDATE STAIRCASE - no response treated as incorrect
                    if (runExperiment.durationStaircase != null)
                    {
                        float nextDuration = runExperiment.durationStaircase.ProcessResponse(false);  // Always false
                        Debug.Log($"<color=orange>[Staircase] No-response: ✗ → Next duration: {nextDuration:F3}s (making easier)</color>");
                    }
                }
                
                runExperiment.detectIndex = 0;
            }

            while (runExperiment.trialTime < runExperiment.thisTrialDuration)
            {
                yield return null;
            }

            break;
        }
    }

    public void hideNavon()
    {
        makeNavonStimulus.hideNavon();
        runExperiment.targState = 0;
        runExperiment.detectIndex = 0;
    }
}