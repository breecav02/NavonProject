using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DurationStaircase : MonoBehaviour
{
    /// <summary>
    /// CORRECTED VERSION: Per-stimulus 2-up-2-down staircase
    /// - 2 consecutive CORRECT stimuli → DECREASE duration (harder)
    /// - 2 consecutive INCORRECT stimuli → INCREASE duration (easier)
    /// Updates IMMEDIATELY after each stimulus response
    /// </summary>

    [Header("Duration Parameters")]
    [Tooltip("Starting stimulus duration in seconds")]
    public float initialDuration = 0.600f;  // 600ms starting point
    
    [Tooltip("Minimum duration (hardest)")]
    public float minDuration = 0.01667f;  // 16.67ms minimum
    
    [Tooltip("Maximum duration (easiest)")]
    public float maxDuration = 1.2f;  // 1200ms max
    
    [Header("Step Sizes")]
    [Tooltip("Initial step size (seconds to add/subtract)")]
    public float initialStepSize = 0.030f;  // 30ms steps
    
    [Tooltip("Final step size for fine adjustments")]
    public float finalStepSize = 0.030f;  // Keep constant at 30ms
    
    [Tooltip("Reduce step size after this many reversals")]
    public int trialsToReduceStep = 4;
    
    [Header("Stopping Criteria")]
    public int maxTrials = 120;
    public int minReversals = 8;
    public int trialsAfterMinReversals = 10;
    
    // Current state
    [Header("Current State (Read Only)")]
    [SerializeField] private float currentDuration;
    [SerializeField] private float currentStepSize;
    [SerializeField] private int stimulusCount = 0;  // Count STIMULI, not trials
    [SerializeField] private int reversalCount = 0;
    [SerializeField] private int consecutiveCorrect = 0;
    [SerializeField] private int consecutiveIncorrect = 0;
    [SerializeField] private bool isComplete = false;
    
    // History tracking
    private List<float> durationHistory = new List<float>();
    private List<bool> responseHistory = new List<bool>();
    private List<float> reversalDurations = new List<float>();
    private List<int> reversalStimuli = new List<int>();
    private bool lastDirectionWasUp = false;  // Up = INCREASE duration (easier)
    private bool hasHadFirstReversal = false;
    
    // Properties for external access
    public float CurrentDuration => currentDuration;
    public bool IsComplete => isComplete;
    public int StimulusCount => stimulusCount;
    public int ReversalCount => reversalCount;
    public float EstimatedThreshold => CalculateThreshold();
    
    void Start()
    {
        InitializeStaircase();
    }
    
    public void InitializeStaircase()
    {
        currentDuration = initialDuration;
        currentStepSize = initialStepSize;
        stimulusCount = 0;
        reversalCount = 0;
        consecutiveCorrect = 0;
        consecutiveIncorrect = 0;
        isComplete = false;
        hasHadFirstReversal = false;
        
        durationHistory.Clear();
        responseHistory.Clear();
        reversalDurations.Clear();
        reversalStimuli.Clear();
        
        Debug.Log($"<color=cyan>Duration Staircase initialized: PER-STIMULUS 2-up-2-down</color>");
        Debug.Log($"<color=cyan>Initial duration: {initialDuration:F3}s, Step: {initialStepSize:F3}s</color>");
    }
    
    /// <summary>
    /// Process a single stimulus response - updates staircase IMMEDIATELY if needed
    /// </summary>
    /// <param name="correct">Was this stimulus response correct?</param>
    /// <returns>The duration for the NEXT stimulus</returns>
    public float ProcessResponse(bool correct)
    {
        if (isComplete)
        {
            Debug.LogWarning("Staircase is already complete!");
            return currentDuration;
        }
        
        // Record the response
        stimulusCount++;
        responseHistory.Add(correct);
        durationHistory.Add(currentDuration);
        
        // Update consecutive counters
        if (correct)
        {
            consecutiveCorrect++;
            consecutiveIncorrect = 0;
            Debug.Log($"<color=green>Stimulus {stimulusCount}: CORRECT (consecutive: {consecutiveCorrect})</color>");
        }
        else
        {
            consecutiveIncorrect++;
            consecutiveCorrect = 0;
            Debug.Log($"<color=red>Stimulus {stimulusCount}: INCORRECT (consecutive: {consecutiveIncorrect})</color>");
        }
        
        // Determine if we should change duration
        bool shouldGoUp = ShouldIncreaseDuration();    // Make EASIER (longer)
        bool shouldGoDown = ShouldDecreaseDuration();  // Make HARDER (shorter)
        
        // Check for reversal BEFORE updating duration
        bool isReversal = CheckForReversal(shouldGoUp, shouldGoDown);
        
        // Update duration based on 2-up-2-down rule
        if (shouldGoUp)
        {
            IncreaseDuration(); // Make EASIER (longer duration)
            lastDirectionWasUp = true;
            ResetConsecutiveCounters();
        }
        else if (shouldGoDown)
        {
            DecreaseDuration(); // Make HARDER (shorter duration)
            lastDirectionWasUp = false;
            ResetConsecutiveCounters();
        }
        
        // Record reversal if it occurred
        if (isReversal)
        {
            reversalCount++;
            reversalDurations.Add(currentDuration);
            reversalStimuli.Add(stimulusCount);
            hasHadFirstReversal = true;
            
            Debug.Log($"<color=yellow>⭐ Reversal #{reversalCount} at stimulus {stimulusCount}, duration {currentDuration:F3}s</color>");
            
            // Reduce step size after certain number of reversals
            if (reversalCount > 0 && reversalCount % trialsToReduceStep == 0)
            {
                ReduceStepSize();
            }
        }
        
        // Check completion criteria (based on STIMULI, not trials)
        CheckCompletionCriteria();
        
        // Ensure duration stays within bounds
        currentDuration = Mathf.Clamp(currentDuration, minDuration, maxDuration);
        
        Debug.Log($"<color=cyan>Next stimulus duration: {currentDuration:F3}s (Step: {currentStepSize:F3}s, Reversals: {reversalCount})</color>");
        
        return currentDuration;
    }
    
    private bool ShouldIncreaseDuration()
    {
        // When 2 INCORRECT: increase duration (make EASIER)
        // This happens IMMEDIATELY after 2nd consecutive incorrect
        return consecutiveIncorrect >= 2;
    }
    
    private bool ShouldDecreaseDuration()
    {
        // When 2 CORRECT: decrease duration (make HARDER)
        // This happens IMMEDIATELY after 2nd consecutive correct
        return consecutiveCorrect >= 2;
    }
    
    private bool CheckForReversal(bool shouldGoUp, bool shouldGoDown)
    {
        if (!hasHadFirstReversal)
        {
            if (shouldGoUp || shouldGoDown)
            {
                return true;
            }
        }
        else
        {
            if ((shouldGoUp && !lastDirectionWasUp) || (shouldGoDown && lastDirectionWasUp))
            {
                return true;
            }
        }
        return false;
    }
    
    private void IncreaseDuration()
    {
        float oldDuration = currentDuration;
        currentDuration += currentStepSize;
        Debug.Log($"<color=orange>→ Making EASIER (longer duration): {oldDuration:F3}s → {currentDuration:F3}s (+{currentStepSize:F3}s)</color>");
    }
    
    private void DecreaseDuration()
    {
        float oldDuration = currentDuration;
        currentDuration -= currentStepSize;
        Debug.Log($"<color=blue>→ Making HARDER (shorter duration): {oldDuration:F3}s → {currentDuration:F3}s (-{currentStepSize:F3}s)</color>");
    }
    
    private void ResetConsecutiveCounters()
    {
        consecutiveCorrect = 0;
        consecutiveIncorrect = 0;
    }
    
    private void ReduceStepSize()
    {
        float oldStepSize = currentStepSize;
        currentStepSize = Mathf.Max(finalStepSize, currentStepSize * 0.5f);
        Debug.Log($"<color=magenta>Step size reduced: {oldStepSize:F3}s → {currentStepSize:F3}s</color>");
    }
    
    private void CheckCompletionCriteria()
    {
        // Complete ONLY after all trials are done (120 trials × 5 stimuli = 600 stimuli)
        // This ensures the staircase continues adapting throughout the entire experiment
        bool maxStimuliReached = stimulusCount >= (maxTrials * 5);
        
        if (maxStimuliReached)
        {
            isComplete = true;
            Debug.Log($"<color=green>🎯 Staircase complete! Stimuli: {stimulusCount}, Reversals: {reversalCount}, Threshold: {EstimatedThreshold:F3}s</color>");
        }
    }
    
    private float CalculateThreshold()
    {
        if (reversalDurations.Count < 4)
        {
            return currentDuration;
        }
        
        // Use last 6 reversals or all if fewer than 6
        int reversalsToUse = Mathf.Min(6, reversalDurations.Count);
        int startIndex = reversalDurations.Count - reversalsToUse;
        
        float sum = 0f;
        for (int i = startIndex; i < reversalDurations.Count; i++)
        {
            sum += reversalDurations[i];
        }
        
        return sum / reversalsToUse;
    }
    
    public void PrintSummary()
    {
        Debug.Log("=== DURATION STAIRCASE SUMMARY ===");
        Debug.Log($"Procedure: PER-STIMULUS 2-up-2-down (70.7% threshold)");
        Debug.Log($"Stimuli completed: {stimulusCount}");
        Debug.Log($"Reversals: {reversalCount}");
        Debug.Log($"Final duration: {currentDuration:F3}s");
        Debug.Log($"Final step size: {currentStepSize:F3}s");
        Debug.Log($"Estimated threshold: {EstimatedThreshold:F3}s");
        
        if (responseHistory.Count > 0)
        {
            float accuracy = responseHistory.Count(r => r) / (float)responseHistory.Count * 100f;
            Debug.Log($"Overall accuracy: {accuracy:F1}%");
        }
        
        Debug.Log("Reversal durations: " + string.Join(", ", reversalDurations.Select(r => r.ToString("F3"))));
    }
    
    public void Reset()
    {
        InitializeStaircase();
    }
    
    public List<float> GetDurationHistory()
    {
        return new List<float>(durationHistory);
    }
    
    public List<bool> GetResponseHistory()
    {
        return new List<bool>(responseHistory);
    }
}