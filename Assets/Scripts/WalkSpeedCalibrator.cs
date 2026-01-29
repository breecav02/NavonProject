using UnityEngine;
using System.Collections.Generic;
using System;

public class WalkSpeedCalibrator : MonoBehaviour
{
    /// <summary>
    /// SIMPLIFIED VERSION FOR STANDING-ONLY EXPERIMENT
    /// This script is bypassed entirely - all trials are standing with fixed 10s duration
    /// </summary>
    
    [Header("Calibration Settings")]
    public int requiredLaps = 6;
    public float minLapTime = 1f;
    public float maxLapTime = 10f;

    [Header("Results")]
    public float walkDuration = 10f;  // Fixed at 10 seconds for standing experiment
    
    public float currentTimer = 0f;
    public bool timerRunning = false;
    public int currentLap = 0;

    [SerializeField] GameObject TextScreen;
    [SerializeField] GameObject TextStartzone;
    [SerializeField] GameObject TextEndzone;
    [SerializeField] GameObject walkingGuide;

    ShowText ShowText;
    CalibrationText CalibrationTextStartzone;
    CalibrationText CalibrationTextEndzone;
    controlWalkingGuide controlWalkingGuide;
    runExperiment runExperiment;
    experimentParameters experimentParameters;

    private enum CalibrationState
    {
        CalibrationComplete
    }
    
    private CalibrationState currentState = CalibrationState.CalibrationComplete;
    
    void Start()
    {
        runExperiment = GetComponent<runExperiment>();
        experimentParameters = GetComponent<experimentParameters>();
        controlWalkingGuide = GetComponent<controlWalkingGuide>();

        // STANDING EXPERIMENT - Skip calibration entirely
        Debug.Log("Standing-only experiment - Walk calibration bypassed");

        // Set walking guide visible and centered
        if (walkingGuide != null)
        {
            walkingGuide.SetActive(true);
        }
        
        // Set default walk duration (not used, but kept for compatibility)
        walkDuration = 10f;

        // Hide calibration text objects if they exist
        if (TextStartzone != null)
        {
            TextStartzone.SetActive(false);
        }
        if (TextEndzone != null)
        {
            TextEndzone.SetActive(false);
        }

        // Set up the walking guide for experiment (centered, stationary)
        if (controlWalkingGuide != null)
        {
            controlWalkingGuide.setGuidetoCentre();
        }

        // Don't show any text - runExperiment will handle showing TrialStart screen

        // Mark calibration as complete
        currentState = CalibrationState.CalibrationComplete;
        
        // Disable this component to save performance
        Debug.Log("WalkSpeedCalibrator: Calibration complete (standing-only mode)");
        this.enabled = false;
    }
    
    public bool isCalibrationComplete()
    {
        // Always return true for standing experiment
        return true;
    }
}
