using UnityEngine;
using DG.Tweening;

public class controlWalkingGuide : MonoBehaviour
{

    runExperiment runExperiment;
    experimentParameters expParams;

    public GameObject walkingGuide;
    public GameObject HMD; // head mounted display camera
    private float reachBelowPcnt;
    private Vector3 centreLocation; // store the central location

    void Start()
    {
        runExperiment = GetComponent<runExperiment>();
        expParams = GetComponent<experimentParameters>();
        
        // Parameters
        reachBelowPcnt = 0.65f; // percentage of height for vertical positioning
        centreLocation = walkingGuide.transform.position; // starts at centre
        
        DOTween.defaultEaseType = Ease.Linear;

        // Set guide to centre at launch
        setGuidetoCentre();
        
        Debug.Log("Walking guide initialized as STATIONARY fixation point (standing experiment)");
    }

    public void SetGuideForNextTrial()
    {
        // In standing experiment, guide always stays centered
        setGuidetoCentre();
    }

    public void setGuidetoCentre()
    {
        Vector3 currentPos = walkingGuide.transform.position;
        currentPos.x = centreLocation.x;
        currentPos.z = centreLocation.z;
        
        // Apply position
        walkingGuide.transform.position = currentPos;
        
        // Face center of environment
        walkingGuide.transform.LookAt(new Vector3(0, walkingGuide.transform.position.y, 0));
    }

    public void setGuidetoHidden()
    {
        // Move off-screen if needed
        Vector3 currentPos = walkingGuide.transform.position;
        currentPos.x = 100f; // Move far off to the side
        walkingGuide.transform.position = currentPos;
    }

    public void updateScreenHeight()
    {
        // If in VR mode, adjust guide height based on HMD position
        if (runExperiment.playinVR)
        {
            Debug.Log("Updating screen height based on HMD");
            Vector3 currentPos = walkingGuide.transform.position;
            currentPos.y = HMD.transform.position.y * reachBelowPcnt;
            walkingGuide.transform.position = currentPos;
        }
    }

    // NOTE: Movement methods removed for standing experiment
    // The guide remains stationary throughout all trials
}