using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class AdaptiveStaircaseSlow : MonoBehaviour
{

    [System.Serializable]
    public enum StaircaseType
    {
        SimpleUpDown,           // 1-up, 1-down (50% threshold)
        TwoUpOneDown,          // 2-up, 1-down (70.7% threshold)
        ThreeUpOneDown,        // 3-up, 1-down (79.4% threshold)
        OneUpTwoDown,          // 1-up, 2-down (70.7% threshold)
        OneUpThreeDown         // 1-up, 3-down (79.4% threshold)
    }
    
    [Header("Staircase Configuration")]
    public StaircaseType staircaseType = StaircaseType.TwoUpOneDown;
    
    [Header("Combined Spacing + Eccentricity Parameters")]
    [Tooltip("Starting intensity: controls both spacing and eccentricity (0.1 = tight+center/easy)")]
    public float initialIntensity = 0.1f;
    
    [Tooltip("Minimum intensity (tightest spacing + center = easiest)")]
    public float minIntensity = 0.05f;
    
    [Tooltip("Maximum intensity (widest spacing + periphery = hardest)")]
    public float maxIntensity = 2.0f;
    
    [Header("Step Sizes - VERY GRADUAL for Multi-Stimulus")]
    [Tooltip("Initial step size for intensity changes (VERY SMALL for multiple updates per trial)")]
    public float initialStepSize = 0.01f;  // EXTREMELY gradual - barely noticeable
    
    [Tooltip("Final step size for fine adjustments (VERY SMALL)")]
    public float finalStepSize = 0.001f;  // Tiny adjustments
    
    [Tooltip("Reduce step size after this many reversals")]
    public int trialsToReduceStep = 4;
    
    [Header("Stopping Criteria")]
    public int maxTrials = 60;
    public int minReversals = 8;
    public int trialsAfterMinReversals = 10;
    
    // Current state
    [Header("Current State Staircase (Read Only)")]
    [SerializeField, ReadOnly] private float currentIntensity;
    [SerializeField, ReadOnly] private float currentStepSize;
    [SerializeField, ReadOnly] private int trialCount = 0;
    [SerializeField, ReadOnly] private int reversalCount = 0;
    [SerializeField, ReadOnly] private int consecutiveCorrect = 0;
    [SerializeField, ReadOnly] private int consecutiveIncorrect = 0;
    [SerializeField, ReadOnly] private bool isComplete = false;
    
    // History tracking
    private List<float> intensityHistory = new List<float>();
    private List<bool> responseHistory = new List<bool>();
    private List<float> reversalIntensities = new List<float>();
    private List<int> reversalTrials = new List<int>();
    private bool lastDirectionWasUp = false;
    private bool hasHadFirstReversal = false;
    
    // Properties for external access
    public float CurrentIntensity => currentIntensity;
    public bool IsComplete => isComplete;
    public int TrialCount => trialCount;
    public int ReversalCount => reversalCount;
    public float EstimatedThreshold => CalculateThreshold();
    
    void Start()
    {
        InitializeStaircase();
    }
    
    public void InitializeStaircase()
    {
        currentIntensity = initialIntensity;
        currentStepSize = initialStepSize;
        trialCount = 0;
        reversalCount = 0;
        consecutiveCorrect = 0;
        consecutiveIncorrect = 0;
        isComplete = false;
        hasHadFirstReversal = false;
        
        intensityHistory.Clear();
        responseHistory.Clear();
        reversalIntensities.Clear();
        reversalTrials.Clear();
        
        Debug.Log($"GRADUAL Slow Staircase initialized: Type={staircaseType}, Initial={initialIntensity:F3}, StepSize={initialStepSize:F3} (gradual)");
    }
    
    /// <summary>
    /// Main method to call after each trial response
    /// </summary>
    /// <param name="correct">True if response was correct, false if incorrect</param>
    /// <returns>The new intensity value for the next trial</returns>
    public float ProcessResponse(bool correct)
    {
        if (isComplete)
        {
            Debug.LogWarning("Staircase is already complete!");
            return currentIntensity;
        }
        
        // Record the response
        trialCount++;
        responseHistory.Add(correct);
        intensityHistory.Add(currentIntensity);
        
        Debug.Log($"Trial {trialCount}: Response={correct}, Intensity={currentIntensity:F3}");
        
        // Update consecutive counters
        if (correct)
        {
            consecutiveCorrect++;
            consecutiveIncorrect = 0;
        }
        else
        {
            consecutiveIncorrect++;
            consecutiveCorrect = 0;
        }
        
        // Determine if we should change intensity
        bool shouldGoUp = ShouldIncreaseIntensity();
        bool shouldGoDown = ShouldDecreaseIntensity();
        
        // Check for reversal before updating intensity
        bool isReversal = CheckForReversal(shouldGoUp, shouldGoDown);
        
        // Update intensity
        if (shouldGoUp)
        {
            IncreaseIntensity(); // Make HARDER (wider spacing + more peripheral)
            lastDirectionWasUp = true;
            ResetConsecutiveCounters();
        }
        else if (shouldGoDown)
        {
            DecreaseIntensity(); // Make EASIER (tighter spacing + more central)
            lastDirectionWasUp = false;
            ResetConsecutiveCounters();
        }
        
        // Record reversal if it occurred
        if (isReversal)
        {
            reversalCount++;
            reversalIntensities.Add(currentIntensity);
            reversalTrials.Add(trialCount);
            hasHadFirstReversal = true;
            
            Debug.Log($"Reversal #{reversalCount} at trial {trialCount}, intensity {currentIntensity:F3}");
            
            // Reduce step size after certain number of reversals
            if (reversalCount > 0 && reversalCount % trialsToReduceStep == 0)
            {
                ReduceStepSize();
            }
        }
        
        // Check completion criteria
        CheckCompletionCriteria();
        
        // Ensure intensity stays within bounds
        currentIntensity = Mathf.Clamp(currentIntensity, minIntensity, maxIntensity);
        
        Debug.Log($"New intensity: {currentIntensity:F3}, Step size: {currentStepSize:F3}, Reversals: {reversalCount}");
        
        return currentIntensity;
    }
    
    private bool ShouldIncreaseIntensity()
    {
        // When CORRECT: increase intensity (make HARDER - wider spacing + more peripheral)
        switch (staircaseType)
        {
            case StaircaseType.SimpleUpDown:
                return consecutiveCorrect >= 1;
                
            case StaircaseType.TwoUpOneDown:
                return consecutiveCorrect >= 2;
                
            case StaircaseType.ThreeUpOneDown:
                return consecutiveCorrect >= 3;
                
            case StaircaseType.OneUpTwoDown:
                return consecutiveCorrect >= 1;
                
            case StaircaseType.OneUpThreeDown:
                return consecutiveCorrect >= 1;
                
            default:
                return false;
        }
    }
    
    private bool ShouldDecreaseIntensity()
    {
        // When INCORRECT: decrease intensity (make EASIER - tighter spacing + more central)
        switch (staircaseType)
        {
            case StaircaseType.SimpleUpDown:
                return consecutiveIncorrect >= 1;
                
            case StaircaseType.TwoUpOneDown:
                return consecutiveIncorrect >= 1;
                
            case StaircaseType.ThreeUpOneDown:
                return consecutiveIncorrect >= 1;
                
            case StaircaseType.OneUpTwoDown:
                return consecutiveIncorrect >= 2;
                
            case StaircaseType.OneUpThreeDown:
                return consecutiveIncorrect >= 3;
                
            default:
                return false;
        }
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
    
    private void IncreaseIntensity()
    {
        currentIntensity += currentStepSize;
        Debug.Log($"Making HARDER (wider spacing + more peripheral) by {currentStepSize:F3} to {currentIntensity:F3}");
    }
    
    private void DecreaseIntensity()
    {
        currentIntensity -= currentStepSize;
        Debug.Log($"Making EASIER (tighter spacing + more central) by {currentStepSize:F3} to {currentIntensity:F3}");
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
        Debug.Log($"Reduced step size from {oldStepSize:F3} to {currentStepSize:F3}");
    }
    
    private void CheckCompletionCriteria()
    {
        bool maxTrialsReached = trialCount >= maxTrials;
        bool minReversalsReached = reversalCount >= minReversals;
        bool additionalTrialsCompleted = minReversalsReached && 
            (trialCount - reversalTrials[minReversals - 1]) >= trialsAfterMinReversals;
        
        if (maxTrialsReached || additionalTrialsCompleted)
        {
            isComplete = true;
            Debug.Log($"Staircase complete! Trials: {trialCount}, Reversals: {reversalCount}, Threshold: {EstimatedThreshold:F3}");
        }
    }
    
    private float CalculateThreshold()
    {
        if (reversalIntensities.Count < 4)
        {
            return currentIntensity;
        }
        
        // Use last 6 reversals or all if fewer than 6
        int reversalsToUse = Mathf.Min(6, reversalIntensities.Count);
        int startIndex = reversalIntensities.Count - reversalsToUse;
        
        float sum = 0f;
        for (int i = startIndex; i < reversalIntensities.Count; i++)
        {
            sum += reversalIntensities[i];
        }
        
        return sum / reversalsToUse;
    }
    
    public void PrintSummary()
    {
        Debug.Log("=== GRADUAL SLOW STAIRCASE SUMMARY ===");
        Debug.Log($"Type: {staircaseType}");
        Debug.Log($"Trials completed: {trialCount}");
        Debug.Log($"Reversals: {reversalCount}");
        Debug.Log($"Final intensity: {currentIntensity:F3}");
        Debug.Log($"Final step size: {currentStepSize:F3}");
        Debug.Log($"Estimated threshold: {EstimatedThreshold:F3}");
        
        if (responseHistory.Count > 0)
        {
            float accuracy = responseHistory.Count(r => r) / (float)responseHistory.Count * 100f;
            Debug.Log($"Overall accuracy: {accuracy:F1}%");
        }
        
        Debug.Log("Reversal intensities: " + string.Join(", ", reversalIntensities.Select(r => r.ToString("F3"))));
    }
    
    public void Reset()
    {
        InitializeStaircase();
    }
    
    public List<float> GetIntensityHistory()
    {
        return new List<float>(intensityHistory);
    }
    
    public List<bool> GetResponseHistory()
    {
        return new List<bool>(responseHistory);
    }
}