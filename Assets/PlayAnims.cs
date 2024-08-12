using System.Collections;
using System.Collections.Generic;
using UnityEditor.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine;

public class PlayAnims : MonoBehaviour
{
    public Animator mainAnim;

    Coroutine liftsequence; 

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.I))
        {
            if(liftsequence != null)
                StopCoroutine(liftsequence);
            liftsequence = StartCoroutine(AnimationSequence("lift_1")); 
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            if (liftsequence != null)
                StopCoroutine(liftsequence);
            liftsequence = StartCoroutine(AnimationSequence("stack_2"));
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (liftsequence != null)
                StopCoroutine(liftsequence);
            liftsequence = StartCoroutine(AnimationSequence("push_3"));
        }
        
    }


    IEnumerator AnimationSequence(string animname)
    {
        bool animstate = mainAnim.GetCurrentAnimatorStateInfo(0).IsName(animname);
        Debug.Log("1st state: " + animstate);
        yield return new WaitForSeconds(2f);

        // Play animation 
        mainAnim.Play(animname, 0);
        yield return new WaitForSeconds(2f);

        animstate = mainAnim.GetCurrentAnimatorStateInfo(0).IsName(animname);
        Debug.Log("2nd state: " + animstate);
        yield return new WaitForSeconds(2f);

        // Wait for the animation to complete
        while (animstate)
        {
            animstate = mainAnim.GetCurrentAnimatorStateInfo(0).IsName(animname);
            yield return null;
        }

        Debug.Log("Animation Done");

        animstate = mainAnim.GetCurrentAnimatorStateInfo(0).IsName(animname);
        Debug.Log("3rd state: " + animstate);

        yield return null; 

    }

}
