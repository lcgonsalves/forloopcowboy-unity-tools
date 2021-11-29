using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using forloopcowboy_unity_tools.Scripts.Core;
using forloopcowboy_unity_tools.Scripts.GameLogic;
using forloopcowboy_unity_tools.Scripts.Soldier;
using UnityEngine;
using UnityEngine.Assertions;

namespace forloopcowboy_unity_tools.Scripts.BehaviorDesignerTasks
{
    [TaskCategory("Custom")]
    public class MoveAwayFromFriendlyFire : Action
    {
        public SharedGameObject self, gameplayManager;
        public SharedTransform target;

        private AimComponent aim;
        private AdvancedNavigation nav;
        private GameplayManager gm;

        private bool hasFriendlyInCrossfire = false;
        
        private int movementDirection = 1;

        public override void OnStart()
        {
            if (aim == null) aim = self.Value.GetComponent<AimComponent>();
            if (gm == null) gm = gameplayManager.Value.GetComponent<GameplayManager>();
            if (nav == null) nav = self.Value.GetComponent<AdvancedNavigation>();
                        
            Assert.IsNotNull(aim, "Aim Component is required.");
            Assert.IsNotNull(gm, "Gameplay Manager is required.");
            Assert.IsNotNull(nav, "AdvancedNavigation component is required.");
            
            CheckIfHasFriendlyInTheWay();
            if (RandomExtended.Boolean()) movementDirection = -1; else movementDirection = 1;

            if (hasFriendlyInCrossfire)
            {
                var position = nav.transform.TransformPoint(Vector3.left * movementDirection);

                nav.Pause();
                nav.MoveTo(position, .6f);
            }
        }

        private void CheckIfHasFriendlyInTheWay()
        {
            hasFriendlyInCrossfire =
                target.Value != null && aim.HasNPCInCrossfire(target.Value.position, gm.side, gm.UnitManager);
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Success;
        }
    }
}