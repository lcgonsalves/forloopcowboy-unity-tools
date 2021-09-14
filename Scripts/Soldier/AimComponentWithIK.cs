using System;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.Soldier
{
    [RequireComponent(typeof(Animator))]
    public class AimComponentWithIK : AimComponent
    {
        public IKSettings supportHandIKSettings = new IKSettings()
        {
            limb = AvatarIKGoal.LeftHand,
            target = null
        };
        protected Animator _animator;
        
        private void Start() { _animator = GetComponent<Animator>(); }

        private void OnAnimatorIK(int layerIndex)
        {
            if (supportHandIKSettings.target)
            {
                _animator.SetIKPosition(supportHandIKSettings.limb, supportHandIKSettings.target.TransformPoint(supportHandIKSettings.translation.value));
                _animator.SetIKRotation(supportHandIKSettings.limb, Quaternion.Euler(supportHandIKSettings.rotation.value));
                
                _animator.SetIKPositionWeight(supportHandIKSettings.limb, supportHandIKSettings.translation.weight);
                _animator.SetIKRotationWeight(supportHandIKSettings.limb, supportHandIKSettings.rotation.weight);
            }
        }
    }
}