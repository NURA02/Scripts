using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour {

    private Animator anim;

    void Awake() {
        anim = GetComponent<Animator>();
    }

    public void Walk(bool walk) {
        anim.SetBool(AnimationTags.WALK_PARAMETER, walk);
    } 

    public void Run(bool run) {
        anim.SetBool(AnimationTags.RUN_PARAMETER, run);
    }

    public void Attack(bool attack) {
        anim.SetBool(AnimationTags.ATTACK_PARAMETER, attack);
    }

    // public void Dead

}
