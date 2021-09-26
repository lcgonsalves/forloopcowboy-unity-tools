using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;

namespace forloopcowboy_unity_tools.Scripts.BehaviorDesignerTasks
{
    [TaskCategory("Custom")]
    public class IKLerp : Action
    {
        public AimComponentWithIK ikController;
        public float ikValue;
        public bool writeToSharedValue;
        public SharedFloat currentIKRotationWeight;
        public SharedFloat currentIKPositionWeight;
        public SharedBool isCurrentlyLerpingArm;

        private bool hasCompleted = false;
        private Coroutine previousLerp;
        
        public override void OnStart()
        {
            if (!isCurrentlyLerpingArm.Value)
            {
                if (previousLerp != null) ikController.StopCoroutine(previousLerp);
                isCurrentlyLerpingArm = true;
                previousLerp = ikController.LerpIKFor(ikController.weapon, ikValue, () =>
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