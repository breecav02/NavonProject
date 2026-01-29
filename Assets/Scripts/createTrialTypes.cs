using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class createTrialTypes : MonoBehaviour
{
    /// <summary>
    /// This script contains the high-level experiment structure (nblocks etc), and builds the arrays necessary for trial indexing, and data storage.
    /// - at this stage, we predefine trial conditions (pseudo-randomly).
    /// </summary>


    // within trial data params and storage [ move to scriptable object?]
    
    private int nTrialsperBlock, nBlocks, nStaircaseBlocks, nBlocktypes, nstandingStilltrials;
    private int[] blockTypelist;
    
    [HideInInspector]
    public int[,] blockTypeArray; //nTrials x 3 (block, trialID, type)


    // set up the public struct trialData, for caching trial parameters to be read/recorded as we progress.
    [System.Serializable]
    public struct trialData
    {
        public float trialNumber,blockID,trialID,trialType, targOrien, targLocX, targLocY, walkSpeed, blockType, targContrast, targOnsetTime,
            clickOnsetTime, targResponse, targResponseTime, targCorrect, stairCase, targContrastPosIdx, degPracticalE, intPracticalE;


        public bool isStationary, signalPresent;
    }


    public trialData trialD;

    void Start()
    {

        createTrialTypesmethod();


    }

    /// 
    /// 
    /// METHODS called:
    /// 
    // shuffle array once populated.
    void shuffleArray(int[] a)
    {
        int n = a.Length;

        for (int id = 0; id < n; id++)
        {
            swap(a, id, id + Random.Range(0, n - id));
        }

    }

    void swap(int[] inputArray, int a, int b)
    {
        int temp = inputArray[a];
        inputArray[a] = inputArray[b];
        inputArray[b] = temp;

    }
    void createTrialTypesmethod()
    {
        // gather presets
        nBlocktypes = 2; // walking slow and normal.

        //
        nTrialsperBlock = 20; // 
        nBlocks = 6;
        nStaircaseBlocks = 1; // overrides the first block with some additional controls.
        nstandingStilltrials = 1; // note that trial counts start at 0.
        //

        int nTrials = nTrialsperBlock * nBlocks;

        float[] walkDurs = new float[nBlocktypes];

        walkDurs[0] = 15f; //slowDuration;
        walkDurs[1] = 9f; //natural;


        // also create wrapper to determine block conditions.
        // first few trials (or block) should be stationary, for burn-in.

        blockTypelist = new int[nBlocks];

        // block type determines walking speed,
        // 1 = slow walk,
        // 2 = normal walk, 

        // FILL BLOCKS
        int[] BLOCKtypeArray = new int[nBlocktypes];
        BLOCKtypeArray[0] = 1;
        BLOCKtypeArray[1] = 2;

        int typec = 0;
        // fill amount of blocks we have with the above types
        for (int iblock = 0; iblock < nBlocks; iblock++)
        {
            blockTypelist[iblock] = BLOCKtypeArray[typec];
            typec++;
            if (typec == BLOCKtypeArray.Length)
            {
                typec = 0;
            }

        }


        shuffleArray(blockTypelist);

        blockTypeArray = new int[(int)nTrials, 3]; // 3 columns.
                                                   // ensure first staircased trials are stationary.
        int icounter;
        icounter = 0;
        // for staircaseblocks:
        for (int iblock = 0; iblock < nStaircaseBlocks; iblock++)
        {
            for (int itrial = 0; itrial < nTrialsperBlock; itrial++)
            {
                blockTypeArray[icounter, 0] = iblock;
                blockTypeArray[icounter, 1] = itrial; // trial within block                
                blockTypeArray[icounter, 2] = 2; // normal mvmnt during staircase (except below exceptions)

                //// except for first nstanding trials, in which case, we will practice standing still.
                if (icounter <= nstandingStilltrials)
                {
                    blockTypeArray[icounter, 2] = 0; // stationary for first nStandingStilltrials trials= (~4)
                }
                else if (icounter > nstandingStilltrials && icounter <= (nstandingStilltrials + 2)) // then  2x practice going slow
                {
                    blockTypeArray[icounter, 2] = 1; // slow walk.
                }

                icounter++;
            }

        }

        //now fill remaining blocks 
        //
        for (int iblock = nStaircaseBlocks; iblock < nBlocks; iblock++)
        {
            for (int itrial = 0; itrial < nTrialsperBlock; itrial++)
            {
                blockTypeArray[icounter, 0] = iblock;
                blockTypeArray[icounter, 1] = itrial;
                blockTypeArray[icounter, 2] = blockTypelist[iblock - nStaircaseBlocks]; //mvmnt (randomized).

                icounter++;
            }

        }
    }

}