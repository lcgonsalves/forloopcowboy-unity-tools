using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Soldier;
using forloopcowboy_unity_tools.Scripts.Core;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.BehaviorDesignerTasks
{
    [TaskCategory("Custom")]
    public class IKLerp : Action
    {
        public SharedGameObject objectWithIKController;
        private AimComponentWithIK ikController;
        public float ikTargetValue;
        public bool writeToSharedValue;
        public SharedFloat currentIKRotationWeight;
        public SharedFloat currentIKPositionWeight;
        public SharedBool isCurrentlyLerpingArm;

        private bool hasCompleted = false;
        private Coroutine previousLerp;

        public override void OnAwake()
        {
            ikController = objectWithIKController.Value != null ? objectWithIKController.Value.GetOrElseAddComponent<AimComponentWithIK>() : null;
        }

        public override void OnStart()
        {
            ikController = objectWithIKController.Value != null ? objectWithIKController.Value.GetOrElseAddComponent<AimComponentWithIK>() : null;
            if (!isCurrentlyLerpingArm.Value)
            {
                if (previousLerp != null) ikController.StopCoroutine(previousLerp);
                isCurrentlyLerpingArm = true;
                previousLerp = ikController.LerpIKFor(ikController.weapon, ikTargetValue, () =>
                {
                    isCurrentlyLerpingArm = false;
                    hasCompleted = true;
                });
            }
            else hasCompleted = true; // kill early if a lerp is occurring
        }

        public override TaskStatus OnUpdate()
        {
            if (ikController.currentIKSettings != null)
            {
                currentIKRotationWeight.SetValue(ikController.currentIKSettings.rotation.weight);
                currentIKPositionWeight.SetValue(ikController.currentIKSettings.translation.weight);
            }
            
            var status = hasCompleted ? TaskStatus.Success : TaskStatus.Running;
            if (hasCompleted) hasCompleted = false;

            return status;
        }
    }
}